using Microsoft.WindowsAPICodePack.Dialogs;
using PlaylistExporter.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Serialization;

namespace PlaylistExporter.ViewModels
{
    public class MainWindowViewModel: INotifyPropertyChanged
    {

        private BackgroundWorker _worker = new BackgroundWorker();
        /// <summary>
        /// Internal headline of the main window
        /// </summary>
        private int _copyProgress;
        /// <summary>
        /// Headline of the main window
        /// </summary>
        public int CopyProgress
        {
            get { return this._copyProgress; }
            set
            {
                if (value == this._copyProgress) return;
                this._copyProgress = value;
                OnPropertyChanged(nameof(this._copyProgress));
            }
        }

        /// <summary>
        /// Internal headline of the main window
        /// </summary>
        private string _headline;
        /// <summary>
        /// Headline of the main window
        /// </summary>
        public string Headline
        {
            get { return this._headline; }
            set
            {
                if (value == this._headline) return;
                this._headline = value;
                OnPropertyChanged(nameof(this._headline));
            }
        }
        /// <summary>
        /// Current executed action
        /// </summary>
        private string _currentAction;
        /// <summary>
        /// Current executed action
        /// </summary>
        public string CurrentAction
        {
            get { return this._currentAction; }
            set
            {
                if (value == this._currentAction) return;
                this._currentAction = value;
                OnPropertyChanged(nameof(this._currentAction));
            }
        }
        /// <summary>
        /// Current target
        /// </summary>
        private string _currentTarget;
        /// <summary>
        /// Current target
        /// </summary>
        public string CurrentTarget
        {
            get { return this._currentTarget; }
            set
            {
                if (value == this._currentTarget) return;
                this._currentTarget = value;
                OnPropertyChanged(nameof(this._currentTarget));
            }
        }
        /// <summary>
        /// Current target
        /// </summary>
        private string _sourcePath;
        /// <summary>
        /// Current target
        /// </summary>
        public string SourcePath
        {
            get { return this._sourcePath; }
            set
            {
                if (value == this._sourcePath) return;
                this._sourcePath = value;
                OnPropertyChanged(nameof(this._sourcePath));
            }
        }
        /// <summary>
        /// Current target
        /// </summary>
        private string _targetPath;
        /// <summary>
        /// Current target
        /// </summary>
        public string TargetPath
        {
            get { return this._targetPath; }
            set
            {
                if (value == this._targetPath) return;
                this._targetPath = value;
                OnPropertyChanged(nameof(this._targetPath));
            }
        }
        /// <summary>
        /// Current loaded playlist
        /// </summary>
        private ObservableCollection<Track> _playlist;
        /// <summary>
        /// Current loaded playlist
        /// </summary>
        public ObservableCollection<Track> Playlist
        {
            get { return this._playlist; }
            set
            {
                if (Equals(value, this._playlist)) return;
                this._playlist = value;
                OnPropertyChanged(nameof(this.Playlist));
            }
        }

        // Menu commands
        public ICommand ExitApplicationCommand { get; set; }
        public ICommand LoadPlaylistCommand { get; set; }
        public ICommand UpdatePlaylistCommand { get; set; }
        public ICommand SetTargetPathCommand { get; set; }
        public ICommand ExportPlaylistCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowViewModel()
        {
            System.Windows.Application.Current.MainWindow.Closing += MainWindow_Closing;

            this._headline = "Playlist Exporter";
            this._currentAction = "Ready";

            LoadCommands();

            this._worker.WorkerSupportsCancellation = true;
            this._worker.WorkerReportsProgress = true;
            this._worker.ProgressChanged += Worker_ProgressChanged;
            this._worker.DoWork += Worker_DoWork;

        }
        /// <summary>
        /// Load commands for view interaction.
        /// </summary>
        private void LoadCommands()
        {
            // Menu commands
            ExitApplicationCommand = new RelayCommand(ExitApplication, CanExitApplication);
            LoadPlaylistCommand = new RelayCommand(LoadPlaylist, CanLoadPlaylist);
            UpdatePlaylistCommand = new RelayCommand(UpdatePlaylist, CanUpdatePlaylist);
            SetTargetPathCommand = new RelayCommand(SetTargetPath, CanSetTargetPath);
            ExportPlaylistCommand = new RelayCommand(ExportPlaylist, CanExportPlaylist);
        }

        // Menu commands
        private void ExitApplication(object p)
        {
            System.Windows.Application.Current.Shutdown();
        }
        private bool CanExitApplication(object p)
        {
            return true;
        }

        private void LoadPlaylist(object p)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "PLAYLIST files (*.m3u) | *.m3u"
            };
            if (ofd.ShowDialog() == true)
            {
                this.SourcePath = ofd.FileName;
                this.Playlist = new PlaylistHandler().Load(this.SourcePath); // load the playlist
            }
        }
        private bool CanLoadPlaylist(object p)
        {
            return true;
        }

        private void UpdatePlaylist(object p)
        {
            this.Playlist = new PlaylistHandler().Load(this.SourcePath); // load the playlist
        }
        private bool CanUpdatePlaylist(object p)
        {
            return true;
        }

        private void SetTargetPath(object p)
        {
            var cofd = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                Title = "Select root folder."
            };

            while (cofd.ShowDialog() != CommonFileDialogResult.Ok) { }
            this._targetPath = cofd.FileName;
        }
        private bool CanSetTargetPath(object p)
        {
            return true;
        }

        private void ExportPlaylist(object p)
        {            
            this._worker.RunWorkerAsync();
        }
        private bool CanExportPlaylist(object p)
        {
            return true;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.CurrentAction = "Copying";
            for (int i = 0; i < this.Playlist.Count; i++)
            {
                string targetPath = this._targetPath;
                var t = this.Playlist[i];
                targetPath = Path.Combine(targetPath, i.ToString("000") + "_" + t.FileName);
                if (!File.Exists(targetPath))
                {
                    this.CurrentTarget = t.FullFilePath;
                    CopyFile(t.FullFilePath, targetPath);
                }
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.CopyProgress = e.ProgressPercentage;
        }

        private void CopyFile(string src, string des)
        {
            FileStream fsOut = new FileStream(des, FileMode.Create);
            FileStream fsIn = new FileStream(src, FileMode.Open);

            byte[] bt = new byte[1048756];
            int readByte;

            while((readByte = fsIn.Read(bt, 0, bt.Length)) > 0)
            {
                fsOut.Write(bt, 0, readByte);
                this._worker.ReportProgress((int)(fsIn.Position * 100 / fsIn.Length));
            }
            fsIn.Close();
            fsOut.Close();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            
        }

        //[NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Track
    {
        public Track(string title, string interpret, string filePath)
        {
            if (filePath.IndexOf("/") > -1)
                filePath = filePath.Replace("/", "\\");
            this.Title = title;
            this.Interpret = interpret;
            this.FullFilePath = filePath;
            this.FileName = Path.GetFileName(filePath);
            this.FilePath = Path.GetDirectoryName(filePath);
        }
        public string FilePath
        {
            get;
            set;
        }
        public string FileName
        {
            get;
            set;
        }
        public string FullFilePath
        {
            get;
            set;
        }
        public string Title
        {
            set;
            get;
        }
        public string Interpret
        {
            set;
            get;
        }
    }

    public class PlaylistHandler
    {
        public ObservableCollection<Track> Load(string filePath)
        {
            if (File.Exists(filePath))
            {
                // Read playlist line by line.  
                string[] lines = File.ReadAllLines(filePath);
                ObservableCollection<Track> tracks = new ObservableCollection<Track>();

                if (lines.Length <= 0) return null;

                string playlistType = lines[0];
                
                switch (playlistType)
                {
                    case "#EXTM3U":
                        for (int i = 1; i < lines.Length; i+=2)
                        {
                            // invalid lines with no content
                            if (lines[i].Length <= 0 || lines[i + 1].Length <= 0) continue;

                            // line for title and interpret is not valid
                            if (!lines[i].StartsWith("#EXTINF:")) continue;

                            string s = lines[i].Split(',')[1];
                            int id = s.IndexOf(" - ");
                            string title = s.Substring(0, id);
                            string interpret = s.Substring(id).Replace(" - ", "");
                            string path = lines[i + 1];

                            tracks.Add(new Track(title, interpret, path));
                        }
                        break;
                }
                return tracks;
            }
            return null;
        }
    }
}

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AlphaTab;
using AlphaTab.Importer;
using AlphaTab.Model;
using AlphaTab.Synth;
using FontAwesome.Sharp;
using Microsoft.Win32;
using TabSync.src.Data;
using TabSync.src.Util;

namespace TabSync.src.UI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged {

        private Mp3Player player;
        private FileSystem fsys;
        private string currentTabPath, currentSongPath;
        

        private Score _tabScore;
        private Track _selectedTrack;

        // Load the complete Score in the File
        public Score TabScore {
            get => _tabScore;
            set {
                if (Equals(value, _tabScore)) return;
                _tabScore = value;
                SelectedTrack = _tabScore.Tracks[0];
                OnPropertyChanged();
            }
        }

        // Load the selected Track in the score
        public Track SelectedTrack {
            get => _selectedTrack;
            set {
                if (Equals(value, _selectedTrack)) return;
                _selectedTrack = value;

                AlphaTab.Tracks = new[] {
                    _selectedTrack
                };
                AlphaTab.RenderTracks();
                OnPropertyChanged();
            }
        }

        // Get/Set for the playback offset of the mp3 file
        private long _songPlayBackOffset;
        public long SongPlaybackOffset {
            get => _songPlayBackOffset;
            set {
                _songPlayBackOffset = value;
                TB_SongPlayBackOffset.Text = value + "";
            }
        }

        // Unused - Maybe implemented in a later version
        //private bool _isCountInActive;
        //public bool IsCountInActive {
        //    get => _isCountInActive;
        //    set {
        //        if (value == _isCountInActive) return;
        //        _isCountInActive = value;
        //        OnPropertyChanged();
        //        if(AlphaTab.Api != null) {
        //            AlphaTab.Api.CountInVolume = value ? 1 : 0;
        //        }
        //    }
        //}

        //private bool _isLooping;
        //public bool IsLooping {
        //    get => _isLooping;
        //    set {
        //        if (value == _isLooping) return;
        //        _isLooping = value;
        //        OnPropertyChanged();
        //        if(AlphaTab.Api != null) {
        //            AlphaTab.Api.IsLooping = value;
        //        }
        //    }
        //}


        // De-/Activate Metronome playback
        private bool _isMetronomeActive;
        public bool IsMetronomeActive {
            get => _isMetronomeActive;
            set {
                if (value == _isMetronomeActive) return;
                _isMetronomeActive = value;
                OnPropertyChanged();
                if(AlphaTab.Api != null) {
                    AlphaTab.Api.MetronomeVolume = value ? 1 : 0;
                }
            }
        }

        // Setting for the Zoom Level of the Tab renderer
        public double[] ZoomLevels { get; } = { .25, .5, .75, .9, 1.0, 1.1, 1.25, 1.5, 2 };
        private double _currentZoomLevel;
        public double CurrentZoomLevel {
            get => _currentZoomLevel;
            set {
                if (value.Equals(_currentZoomLevel)) return;
                _currentZoomLevel = value;
                OnPropertyChanged();

                AlphaTab.Settings.Display.Scale = value;
                if(AlphaTab.Api != null) {
                    AlphaTab.Api.UpdateSettings();
                    AlphaTab.RenderTracks();
                }
            }
        }

        // Setting for the two Layout Modes of the Tab renderer
        public LayoutMode[] LayoutModes { get; } = new[] { LayoutMode.Page, LayoutMode.Horizontal };
        private LayoutMode _currentLayoutMode;
        public LayoutMode CurrentLayoutMode {
            get => _currentLayoutMode;
            set {
                if (value == _currentLayoutMode) return;
                _currentLayoutMode = value;
                OnPropertyChanged();
                AlphaTab.Settings.Display.LayoutMode = value;
                if(AlphaTab.Api != null) {
                    AlphaTab.Api.UpdateSettings();
                    AlphaTab.RenderTracks();
                }
            }
        }

        // Set Visibility of the File-Loading indicator
        private Visibility _loadingIndicatorVisibility = Visibility.Collapsed;
        public Visibility LoadingIndicatorVisibility {
            get => _loadingIndicatorVisibility;
            set {
                if (value == _loadingIndicatorVisibility) return;
                _loadingIndicatorVisibility = value;
                OnPropertyChanged();
            }
        }

        //--------------------------------
        // Player functions

        public bool IsPlayerReady => AlphaTab.Api?.IsReadyForPlayback ?? false;
        private void OnStopClicked(object sender, RoutedEventArgs e) {
            AlphaTab.Api.Stop();
            player.Stop();
        }
        private void OnPlayPauseClicked(object sender, RoutedEventArgs e) {
            AlphaTab.Api.MasterVolume = CurrentVolume / 100.0 * 3.0;
            AlphaTab.Api.PlayPause();
            
            if (AlphaTab.Api.PlayerState == PlayerState.Paused) { // When unpausing
                player.Seek(CurrentTimePosition.TotalMilliseconds + SongPlaybackOffset);
                player.Play();
            } else {
                player.Pause();
                
            }
        }

        //--------------------------------------------------------


        // Update the Time position (display only) in the Playback
        private TimeSpan _currentTimePosition;
        public TimeSpan CurrentTimePosition {
            get => _currentTimePosition;
            set {
                if (value.Equals(_currentTimePosition)) return;
                _currentTimePosition = value;
                OnPropertyChanged();
            }
        }
        private TimeSpan _totalTimePosition;
        public TimeSpan TotalTimePosition {
            get => _totalTimePosition;
            set {
                if (value.Equals(_totalTimePosition)) return;
                _totalTimePosition = value;
                OnPropertyChanged();
            }
        }

        // Volume Adjustment for the Tab playback
        private double _currentVolume;
        public double CurrentVolume {
            get => _currentVolume;
            set {
                if (value == _currentVolume) return;
                _currentVolume = value;
                OnPropertyChanged();
                if(AlphaTab.Api != null) {
                    AlphaTab.Api.Player.MasterVolume = _currentVolume / 100.0 * 3.0;

                }
            }
        }


        // Volume Adjustment for the MP3 Player Playback
        private double _currentSongVolume;
        public double CurrentSongVolume {
            get => _currentSongVolume;
            set {
                if (value == _currentSongVolume) return;
                _currentSongVolume = value;
                OnPropertyChanged();
                if (player != null) {
                    player.Volume = _currentSongVolume / 100.0;
                }
            }
        }


        public MainWindow() {
            // Catch Unhandled Exceptions:
            //     The AlphaTab player throws exception sometimes 
            //     Sadly I can do nothing against it but disabling most unhandled Exceptions
            //     Some Exceptions by the AlphaTab API may come through anyways
            //     Well rip if it does :)
            AppDomain domain = AppDomain.CurrentDomain;
            domain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledHandler);
            Application.Current.DispatcherUnhandledException += (s, e) => { MessageBox.Show("Error: " + e.Exception.Message); };
            
            InitializeComponent();
            DataContext = this;
            player = new Mp3Player();
            
        }

        // OpenFileDialog Opener for the Tab Files
        private void OnOpenClick(object sender, RoutedEventArgs e) {
            var dialog = new OpenFileDialog {
                Filter = "Supported Files (*.gp3, *.gp4, *.gp5, *.gpx, *.gp)|*.gp3;*.gp4;*.gp5;*.gpx;*.gp"
            };
            if (dialog.ShowDialog().GetValueOrDefault()) {
                OpenFile(dialog.FileName);
            }
        }

        // OpenFileDialog Opener for the Mp3 Files
        private void OnOpenSongClick(object sender, RoutedEventArgs e) {
            var dialog = new OpenFileDialog {
                Filter = "Supported Files (*.mp3)|*.mp3"
            };
            if (dialog.ShowDialog().GetValueOrDefault()) {
                OpenSongFile(dialog.FileName);
            }
        }

        // OpenFile Manager for the Tab Files
        private void OpenFile(string filename) {
            // Read the File and open it via the TabScore Setter
            try {
                TabScore = ScoreLoader.LoadScoreFromBytes(File.ReadAllBytes(filename));
                currentTabPath = filename;
            } catch (Exception e) {
                MessageBox.Show("Failed to open file: " + e.Message);
            }

            // Default Settings for the Player
            CurrentZoomLevel = 1.0;
            CurrentLayoutMode = LayoutMode.Page;
            CurrentVolume = 0;
        }

        // OpenFile Manager for the Mp3 Files
        private void OpenSongFile(string filename) {
            // Reset player and Load new File + set volume
            player.DeletePlayer();
            player.Load(filename);
            currentSongPath = filename;
            CurrentSongVolume = 50;
        } 

        

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Add all needed Event Handlers to the AlphaTab API
        private void OnAlphaTabLoaded(object sender, RoutedEventArgs e) {
            // Dis-/Enable the Loading indicator
            AlphaTab.Api.RenderStarted.On(e => {
                LoadingIndicatorVisibility = Visibility.Visible;
            });
            AlphaTab.Api.RenderFinished.On(e => {
                LoadingIndicatorVisibility = Visibility.Collapsed;
            });

            // Update Play button icon
            AlphaTab.Api.PlayerStateChanged.On(pe => {
                PlayPauseIcon.Icon = pe.State == PlayerState.Playing ? IconChar.Pause : IconChar.Play;
            });

            // Manage Player position changes
            var previousTime = -1;
            AlphaTab.Api.PlayerPositionChanged.On(pe => {
                var currentSeconds = (int)(pe.CurrentTime / 1000);
                if (currentSeconds == previousTime) return;

                //Adjust mp3 playback time
                if (player != null) {
                    if (player.IsPlaying) {
                        // Off by more than 10 ms?
                        double off = Math.Abs(player.CurrentPosition - (pe.CurrentTime + SongPlaybackOffset));
                        if (off > 50) {
                            player.Seek(pe.CurrentTime + SongPlaybackOffset); // -> adjust
                            // Adjusting this OnPlaybackTimeChange results in better
                            // and more synchronized playback of the mp3 with the Tab file
                        }
                    }
                }

                // if new second count -> Adjust TimePosition Labels
                previousTime = currentSeconds;

                CurrentTimePosition = TimeSpan.FromMilliseconds(pe.CurrentTime);
                TotalTimePosition = TimeSpan.FromMilliseconds(pe.EndTime);
            });

            AlphaTab.Api.PlayerFinished.On(() => {
                player?.Stop(); // Stop player on finish to prevent a bug which causes the song to loop
            });

            AlphaTab.Api.PlayerReady.On(() => {
                OnPropertyChanged(nameof(IsPlayerReady));
            });
        }

        // Song Playback Offset Input Handler and Updater 
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) {
            Regex regex = new Regex("[0-9]");
            e.Handled = !regex.IsMatch(e.Text);
        }
        private void SongOffsetTextChanged(object sender, TextChangedEventArgs e) {
            string text = ((TextBox)sender).Text;
            if(String.IsNullOrWhiteSpace(text)) {
                SongPlaybackOffset = 0;
                return;
            }
            long.TryParse(text, out long result);
            SongPlaybackOffset = result;
        }

        // Initialize a new FileSystem
        public void InitFileSystem(FileSystem fsys) {
            this.fsys = fsys;
            // Update all info
            SongPlaybackOffset = fsys.GetJsonObject().SongPlaybackOffset;
            OpenFile(fsys.GetJsonObject().TabPath);
            OpenSongFile(fsys.GetJsonObject().SongPath);
        }

        // save the currently open CsTab File
        public void SaveToFileClick(object sender, RoutedEventArgs e) {
            // Currently Unsaved?
            if (fsys == null) {
                SaveFileDialog sdlg = new SaveFileDialog();
                sdlg.FileName = "Unknown";
                sdlg.DefaultExt = ".cstab";
                sdlg.Filter = "CsTab Files (.cstab)|*.cstab";

                if (sdlg.ShowDialog().GetValueOrDefault()) {
                    fsys = new FileSystem(sdlg.FileName);
                }
            }
            // Save to File
            fsys.GetJsonObject().TabPath = currentTabPath;
            fsys.GetJsonObject().SongPath = currentSongPath;
            fsys.GetJsonObject().SongPlaybackOffset = SongPlaybackOffset;
            fsys.Save();
        }

        // Managing the OpenFileDialog for opening a CsTab File
        public void OpenCstabFile(object sender, RoutedEventArgs e) {
            // are there any unsaved Changes? -> Save yes? no? cancel?
            if(UnsavedChanges()) {
                MessageBoxResult result = MessageBox.Show("Save unsaved changes?", "Save?", MessageBoxButton.YesNoCancel);
                if(result == MessageBoxResult.Yes) {
                    SaveToFileClick(null, null); // Save
                }else if(result == MessageBoxResult.Cancel) {
                    return;
                }
            }
            // Open the Dialog to find the CsTab file wanted
            var dialog = new OpenFileDialog {
                Filter = "CsTab Files (.cstab)|*.cstab"
            };
            // Open it
            if (dialog.ShowDialog().GetValueOrDefault()) {
                FileSystem fsysNew = new FileSystem(dialog.FileName);
                InitFileSystem(fsysNew);
            }
        }

        // Are there any Unsaved changes or is there no SaveFile connected to this session?
        private bool UnsavedChanges() {
            return fsys == null || !(fsys.GetJsonObject().TabPath.Equals(currentTabPath) &&
                fsys.GetJsonObject().SongPath.Equals(currentSongPath) &&
                fsys.GetJsonObject().SongPlaybackOffset == SongPlaybackOffset);
        }

        // Open the Program Info About Box
        public void CreditsInfoClick(object sender, RoutedEventArgs e) {
            AboutBox abt = new AboutBox();
            abt.ShowDialog();
        }


        // Exception handler for Unhandled Exception
        // See MainWindow constructor for more info
        static void UnhandledHandler(object sender, UnhandledExceptionEventArgs e) {
            Exception ex = (Exception)e.ExceptionObject;
            MessageBox.Show("Unhandled Exception: " + ex.Message);
        }
    }
}

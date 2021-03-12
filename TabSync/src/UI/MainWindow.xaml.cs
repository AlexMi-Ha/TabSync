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
using TabSync.src.Util;

namespace TabSync.src.UI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged {

        private Mp3Player player;
        

        private Score _tabScore;
        private Track _selectedTrack;

        public Score TabScore {
            get => _tabScore;
            set {
                if (Equals(value, _tabScore)) return;
                _tabScore = value;
                //OnPropertyChanged();
                SelectedTrack = _tabScore.Tracks[0];
                OnPropertyChanged();
            }
        }
        public Track SelectedTrack {
            get => _selectedTrack;
            set {
                if (Equals(value, _selectedTrack)) return;
                _selectedTrack = value;
                //OnPropertyChanged();

                AlphaTab.Tracks = new[] {
                    _selectedTrack
                };
                AlphaTab.RenderTracks();
                OnPropertyChanged();
            }
        }

        private long _songPlayBackOffset;
        public long SongPlaybackOffset {
            get => _songPlayBackOffset;
            set {
                _songPlayBackOffset = value;
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

        // Unused - Maybe implemented in a later version
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

        public bool IsPlayerReady => AlphaTab.Api?.IsReadyForPlayback ?? false;
        private void OnStopClicked(object sender, RoutedEventArgs e) {
            AlphaTab.Api.Stop();
            player.Stop();
        }
        private void OnPlayPauseClicked(object sender, RoutedEventArgs e) {
            AlphaTab.Api.PlayPause();
            if (AlphaTab.Api.PlayerState == PlayerState.Paused) { // When unpausing
                player.Seek(CurrentTimePosition.TotalMilliseconds + SongPlaybackOffset);
                player.Play();
            } else {
                player.Pause();
                
            }
        }

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

        private Visibility _loadingIndicatorVisibility = Visibility.Collapsed;
        public Visibility LoadingIndicatorVisibility {
            get => _loadingIndicatorVisibility;
            set {
                if (value == _loadingIndicatorVisibility) return;
                _loadingIndicatorVisibility = value;
                OnPropertyChanged();
            }
        }

        public MainWindow() {
            AppDomain domain = AppDomain.CurrentDomain;
            domain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledHandler);
            Application.Current.DispatcherUnhandledException += (s, e) => { MessageBox.Show("Error: " + e.Exception.Message); };
            
            InitializeComponent();
            DataContext = this;
            CurrentZoomLevel = 1.0;
            CurrentLayoutMode = LayoutMode.Page;
            CurrentVolume = 40;
            CurrentSongVolume = 50;
            player = new Mp3Player();
            
        }

        private void OnOpenClick(object sender, RoutedEventArgs e) {
            var dialog = new OpenFileDialog {
                Filter = "Supported Files (*.gp3, *.gp4, *.gp5, *.gpx, *.gp)|*.gp3;*.gp4;*.gp5;*.gpx;*.gp"
            };
            if (dialog.ShowDialog().GetValueOrDefault()) {
                OpenFile(dialog.FileName);
            }
        }

        private void OnOpenSongClick(object sender, RoutedEventArgs e) {
            var dialog = new OpenFileDialog {
                Filter = "Supported Files (*.mp3)|*.mp3"
            };
            if (dialog.ShowDialog().GetValueOrDefault()) {
                OpenSongFile(dialog.FileName);
            }
        }

        private void OpenSongFile(string filename) {
            player.DeletePlayer();
            player.Load(filename);
        } 

        private void OpenFile(string filename) {
            try {
                TabScore = ScoreLoader.LoadScoreFromBytes(File.ReadAllBytes(filename));
            }catch(Exception e) {
                MessageBox.Show("Failed to open file: " + e.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnAlphaTabLoaded(object sender, RoutedEventArgs e) {
            AlphaTab.Api.RenderStarted.On(e => {
                LoadingIndicatorVisibility = Visibility.Visible;
            });
            AlphaTab.Api.RenderFinished.On(e => {
                LoadingIndicatorVisibility = Visibility.Collapsed;
            });
            AlphaTab.Api.PlayerStateChanged.On(pe => {
                PlayPauseIcon.Icon = pe.State == PlayerState.Playing ? IconChar.Pause : IconChar.Play;
            });
            AlphaTab.Api.PlayerReady.On(() => {
                OnPropertyChanged(nameof(IsPlayerReady));
            });

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
                        }
                        //else if(off > 20) {
                        //    AlphaTab.Api.Player.TimePosition = Math.Max(player.CurrentPosition - SongPlaybackOffset, 0);
                        //}
                    }
                }

                previousTime = currentSeconds;

                CurrentTimePosition = TimeSpan.FromMilliseconds(pe.CurrentTime);
                TotalTimePosition = TimeSpan.FromMilliseconds(pe.EndTime);
            });
        }

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

        private void SongVolumeMouseUp(object sender, RoutedPropertyChangedEventArgs<double> e) {
            CurrentSongVolume = ((Slider)sender).Value;
        }

        static void UnhandledHandler(object sender, UnhandledExceptionEventArgs e) {
            Exception ex = (Exception)e.ExceptionObject;
            MessageBox.Show("Unhandled Exception: " + ex.Message);
        }
    }
}

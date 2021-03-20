using System;
using System.Windows.Media;

namespace TabSync.src.Util {
    public class Mp3Player {
        
        public event EventHandler PlaybackEnded;

        private MediaPlayer player;

        public double Duration => player?.NaturalDuration.TimeSpan.TotalMilliseconds ?? 0;

        public double CurrentPosition => player?.Position.TotalMilliseconds ?? 0;

        // 0 to 1
        public double Volume {
            get => player?.Volume ?? 0;
            set => SetVolume(value, Balance);
        }

        //-1 (=hard left) to 1 (=hard right)
        public double Balance {
            get => player?.Balance ?? 0;
            set => SetVolume(Volume, value);
        }

        public bool IsPlaying {
            get {
                if (player == null)
                    return false;
                return _isPlaying;
            }
        }
        private bool _isPlaying;


        public bool CanSeek => player != null;


        public bool Load(string fileName) {
            DeletePlayer();

            player = GetPlayer();

            if (player != null) {
                player.Open(new Uri(fileName));
                player.MediaEnded += OnPlaybackEnded;
            }
            
            return player != null && player.Source != null;
        }

        public void DeletePlayer() {
            Stop();

            if (player != null) {
                player.Close();
                player.MediaEnded -= OnPlaybackEnded;
                player = null;
            }
        }

        private void OnPlaybackEnded(object sender, EventArgs args) {
            Stop();

            PlaybackEnded?.Invoke(sender, args);
        }

   
        public void Play() {
            if (player == null || player.Source == null)
                return;

            if (IsPlaying) {
                Stop();
            }

            _isPlaying = true;
            player.Play();
        }

        public void Pause() {
            _isPlaying = false;
            player?.Pause();
        }

 
        public void Stop() {
            Pause();
            Seek(0);
        }

        //position in ms
        public void Seek(double position) {
            if (player == null) return;
            if (position >= Duration) return;
            player.Position = TimeSpan.FromMilliseconds(position);
        }



        private void SetVolume(double volume, double balance) {
            if (player == null || _isDisposed) return;

            player.Volume = Math.Min(1, Math.Max(0, volume));
            player.Balance = Math.Min(1, Math.Max(-1, balance));
        }

        private static MediaPlayer GetPlayer() {
            return new MediaPlayer();
        }

        private bool _isDisposed;

        protected virtual void Dispose(bool disposing) {
            if (_isDisposed || player == null)
                return;

            if (disposing)
                DeletePlayer();

            _isDisposed = true;
        }

        ~Mp3Player() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);

            GC.SuppressFinalize(this);
        }
    }
}

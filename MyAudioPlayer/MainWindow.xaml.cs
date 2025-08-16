using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Windows.Controls.Primitives; // for Thumb events
using MessageBox = System.Windows.MessageBox;

namespace MyAudioPlayer
{
    public partial class MainWindow : Window
    {
        private List<string> playlist = new List<string>();
        private int currentIndex = 0;
        private bool isPlaying = false;
        private bool shuffleEnabled = false;
        private Random random = new Random();

        // Shuffle queue to prevent repeats
        private List<int> shuffleQueue = new List<int>();

        private DispatcherTimer positionTimer;
        private bool isDraggingSlider = false;

        public MainWindow()
        {
            InitializeComponent();

            // Media events
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

            // Hook Thumb drag events for the slider
            PlaybackSlider.AddHandler(Thumb.DragStartedEvent,
                new DragStartedEventHandler(ProgressSlider_DragStarted), true);
            PlaybackSlider.AddHandler(Thumb.DragCompletedEvent,
                new DragCompletedEventHandler(ProgressSlider_DragCompleted), true);

            // Timer to update the progress bar
            positionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            positionTimer.Tick += PositionTimer_Tick;
        }

        private async void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string folderPath = dialog.SelectedPath;
                    await LoadPlaylistAsync(folderPath);
                }
            }
        }

        private async Task LoadPlaylistAsync(string folderPath)
        {
            playlist.Clear();
            CurrentFolderTextBlock.Text = $"Current folder: {folderPath}";
            NowPlayingTextBlock.Text = "Loading...";

            var validExtensions = new[] { ".mp3", ".wav", ".wma" };

            string[] files = await Task.Run(() =>
                Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                         .Where(file => validExtensions.Contains(
                             Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                         .OrderBy(f => f)
                         .ToArray()
            );

            playlist.AddRange(files);

            InitializeShuffleQueue();

            if (playlist.Count > 0)
            {
                currentIndex = 0;
                PlayCurrentTrack();
            }
            else
            {
                MessageBox.Show("No audio files found in this folder.");
                CurrentFolderTextBlock.Text = "No folder selected";
                NowPlayingTextBlock.Text = "Now Playing: None";
            }
        }

        private void InitializeShuffleQueue()
        {
            shuffleQueue.Clear();
            for (int i = 0; i < playlist.Count; i++)
                shuffleQueue.Add(i);

            // Remove current track from the queue to avoid immediate repeat
            if (playlist.Count > 0)
                shuffleQueue.Remove(currentIndex);
        }

        private void PlayCurrentTrack()
        {
            if (playlist.Count == 0) return;

            mediaPlayer.Source = new Uri(playlist[currentIndex]);
            mediaPlayer.Play();
            PlayPauseButton.Content = "Pause";
            isPlaying = true;

            UpdateNowPlaying();
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (playlist.Count == 0) return;

            if (!isPlaying)
            {
                mediaPlayer.Play();
                PlayPauseButton.Content = "Pause";
                isPlaying = true;
            }
            else
            {
                mediaPlayer.Pause();
                PlayPauseButton.Content = "Play";
                isPlaying = false;
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e) => PlayNextTrack();
        private void Previous_Click(object sender, RoutedEventArgs e) => PlayPreviousTrack();

        private void PlayNextTrack()
        {
            if (playlist.Count == 0) return;

            if (shuffleEnabled)
            {
                if (shuffleQueue.Count == 0)
                    InitializeShuffleQueue(); // Refill the queue when empty

                int randomIndex = random.Next(shuffleQueue.Count);
                currentIndex = shuffleQueue[randomIndex];
                shuffleQueue.RemoveAt(randomIndex); // Remove to avoid repeat
            }
            else
            {
                currentIndex = (currentIndex + 1) % playlist.Count;
            }

            PlayCurrentTrack();
        }

        private void PlayPreviousTrack()
        {
            if (playlist.Count == 0) return;

            if (shuffleEnabled)
            {
                // For simplicity, just pick another random track
                if (shuffleQueue.Count == 0)
                    InitializeShuffleQueue();

                int randomIndex = random.Next(shuffleQueue.Count);
                currentIndex = shuffleQueue[randomIndex];
                shuffleQueue.RemoveAt(randomIndex);
            }
            else
            {
                currentIndex--;
                if (currentIndex < 0)
                    currentIndex = playlist.Count - 1;
            }

            PlayCurrentTrack();
        }

        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                PlaybackSlider.Minimum = 0;
                PlaybackSlider.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                PlaybackSlider.Value = 0;
                positionTimer.Start();
            }
        }

        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            positionTimer.Stop();
            PlayNextTrack();
        }

        private void PositionTimer_Tick(object sender, EventArgs e)
        {
            if (!isDraggingSlider && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                PlaybackSlider.Value = mediaPlayer.Position.TotalSeconds;
            }
        }

        private void ProgressSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
            isDraggingSlider = true;
        }

        private void ProgressSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            isDraggingSlider = false;
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                mediaPlayer.Position = TimeSpan.FromSeconds(PlaybackSlider.Value);
            }
        }

        private void PlaybackSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isDraggingSlider && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                mediaPlayer.Position = TimeSpan.FromSeconds(PlaybackSlider.Value);
            }
        }

        private void UpdateNowPlaying()
        {
            if (playlist.Count == 0 || currentIndex < 0 || currentIndex >= playlist.Count)
            {
                NowPlayingTextBlock.Text = "Now Playing: None";
            }
            else
            {
                string filePath = playlist[currentIndex];
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                NowPlayingTextBlock.Text = $"Now Playing: {fileName}";
            }
        }

        private void Shuffle_Checked(object sender, RoutedEventArgs e)
        {
            shuffleEnabled = true;
            InitializeShuffleQueue();
        }

        private void Shuffle_Unchecked(object sender, RoutedEventArgs e)
        {
            shuffleEnabled = false;
        }
    }
}
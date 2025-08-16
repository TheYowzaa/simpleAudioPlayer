using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
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

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => mediaPlayer.Volume = VolumeSlider.Value;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string folderPath = dialog.SelectedPath;
                    LoadPlaylist(folderPath);
                }
            }
        }

        private void LoadPlaylist(string folderPath)
        {
            playlist.Clear();

            CurrentFolderTextBlock.Text = $"Current folder: {folderPath}";

            string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                                      .Where(file => file.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                                                     file.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                                                     file.EndsWith(".wma", StringComparison.OrdinalIgnoreCase))
                                      .OrderBy(f => f)
                                      .ToArray();

            playlist.AddRange(files);

            if (shuffleEnabled)
                ShufflePlaylist();

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

        private void ShufflePlaylist()
        {
            playlist = playlist.OrderBy(x => random.Next()).ToList();
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

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaPlayer != null)
                mediaPlayer.Volume = VolumeSlider.Value;
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            PlayNextTrack();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            PlayPreviousTrack();
        }

        private void PlayNextTrack()
        {
            if (playlist.Count == 0) return;

            currentIndex++;
            if (currentIndex >= playlist.Count)
                currentIndex = 0;

            PlayCurrentTrack();
        }

        private void PlayPreviousTrack()
        {
            if (playlist.Count == 0) return;

            currentIndex--;
            if (currentIndex < 0)
                currentIndex = playlist.Count - 1;

            PlayCurrentTrack();
        }

        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            PlayNextTrack();
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
            if (playlist.Count > 0)
            {
                ShufflePlaylist();
                currentIndex = 0;
                PlayCurrentTrack();
            }
        }

        private void Shuffle_Unchecked(object sender, RoutedEventArgs e)
        {
            shuffleEnabled = false;
            if (playlist.Count > 0)
            {
                // Reorder alphabetically when shuffle is turned off
                playlist = playlist.OrderBy(f => f).ToList();
                currentIndex = 0;
                PlayCurrentTrack();
            }
        }
    }
}

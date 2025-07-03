using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Google.Apis.YouTube.v3;
using spotifyDragDrop.Model;
using spotifyDragDrop.Services;

namespace spotifyDragDrop
{
    /// <summary>  
    /// Interaction logic for MainWindow.xaml  
    /// </summary>  
    /// 
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly YouTubeApiService _youTubeService;
        private readonly SpotifyApiService _spotifyService;
        private readonly SoundCloudApiService _soundCloudService;
        public ObservableCollection<Song> Songs { get; }
        public ICommand DeleteSongCommand { get; }
        public ICommand DownloadSongsCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _selectedDirectory = "E:\\DJ JOEY";
        public string SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                if (_selectedDirectory != value)
                {
                    _selectedDirectory = value;
                    OnPropertyChanged(nameof(SelectedDirectory)); // Notify the UI about the change  
                    ((RelayCommand<object>)DownloadSongsCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private string _message = string.Empty;
        public string Message
        {
            get => _message;
            set
            {
                if (_message != value)
                {
                    _message = value;
                    OnPropertyChanged(nameof(Message)); // Notify the UI about the change  
                }
            }
        }

        public MainWindow(YouTubeApiService youTubeService, SpotifyApiService spotifyService, SoundCloudApiService soundCloudService)
        {
            _youTubeService = youTubeService;
            _spotifyService = spotifyService;
            _soundCloudService = soundCloudService; 

            InitializeComponent();
            DataContext = this;

            Songs = new ObservableCollection<Song>();
            Songs.CollectionChanged += Songs_CollectionChanged;
            SongListBox.ItemsSource = Songs;

            DeleteSongCommand = new RelayCommand<Song>(DeleteSong);
            DownloadSongsCommand = new RelayCommand<object>(_ => DownloadSongs(), _ => CanDownloadSongs());

        }

        private void Songs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ((RelayCommand<object>)DownloadSongsCommand).RaiseCanExecuteChanged();
        }

        private async void HandleDragAndDrop(string url)
        {

            Message = "Scanning URL...";

            var source = MediaProcessingHelper.DetermineSourceFromUrl(url);

            if (string.IsNullOrWhiteSpace(url))
            {
                Message = "Invalid URL. Please try again.";
                return;
            }

            try
            {
                switch (source)
                {
                    case "Spotify":
                        var spotifySong = await Song.CreateFromSpotifyUrlAsync(url, _youTubeService, _spotifyService);
                        Songs.Add(spotifySong);
                        break;

                    case "SoundCloud":
                        var soundCloudSong = await Song.CreateFromSoundCloudUrlAsync(url,_soundCloudService);
                        Songs.Add(soundCloudSong);
                        break;

                    default:
                        throw new Exception("Unsupported URL format. Please provide a valid Spotify or SoundCloud URL.");
                }
                Message = "Song added successfully!";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex}");
                Message = $"Error: {ex.Message}";
            }
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string dataString = (string)e.Data.GetData(DataFormats.StringFormat);
                HandleDragAndDrop(dataString);
            }

            // Reset the DragDropBorder's color to its original state
            DragDropBorder.BorderBrush = Brushes.Gray; // Revert border color to gray
            DragDropBorder.Background = new SolidColorBrush(Color.FromRgb(239, 239, 239)); // Revert background to original color
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void DragDropBorder_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                DragDropBorder.BorderBrush = Brushes.Green; // Change border color to green  
                DragDropBorder.Background = Brushes.LightGreen; // Change background to light green  
            }
        }

        private void DragDropBorder_DragLeave(object sender, DragEventArgs e)
        {
            DragDropBorder.BorderBrush = Brushes.Gray; // Revert border color to gray  
            DragDropBorder.Background = new SolidColorBrush(Color.FromRgb(239, 239, 239)); // Revert background to original color  
        }

        private void DeleteSong(Song song)
        {
            if (song != null && Songs.Contains(song))
            {
                Songs.Remove(song);
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select a folder",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                // Extract the folder path from the selected file  
                SelectedDirectory = System.IO.Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                Message = $"Selected folder: {SelectedDirectory}";
            }
        }

        private bool CanDownloadSongs()
        {
            return Songs.Count > 0 && !string.IsNullOrWhiteSpace(SelectedDirectory);
        }

        private async void DownloadSongs()
        {
            if (string.IsNullOrWhiteSpace(SelectedDirectory))
            {
                Message = "Please select a directory to save the MP3 files.";
                return;
            }

            if (Songs.Count == 0)
            {
                Message = "No songs to download.";
                return;
            }

            Message = "Starting download...";

            foreach (var song in Songs)
            {
                if (string.IsNullOrWhiteSpace(song.YoutubeUrl))
                {
                    Message = $"Skipping '{song.Title}' (no YouTube URL).";
                    continue;
                }

                try
                {
                    await DownloadMp3Async(song, SelectedDirectory);
                    Message = $"Downloaded: {song.Title}";
                }
                catch (Exception ex)
                {
                    Message = $"Error downloading '{song.Title}': {ex.Message}";
                }
            }

            Message = "Download complete.";

            Songs.Clear();
        }

        private async Task DownloadMp3Async(Song song, string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(song.Title) || string.IsNullOrWhiteSpace(song.Artist))
            {
                throw new Exception($"Missing metadata for song: {song.Title ?? "Unknown Title"}");
            }

            // Paths for the MP3 file and album art
            string mp3Path = Path.Combine(outputDirectory, $"{song.Artist} - {song.Title}.mp3");
            string albumArtPath = Path.Combine(outputDirectory, $"{song.Title}_cover.jpg");

            var mediaHelper = new MediaProcessingHelper();

            if (song.YoutubeUrl == null) return;

            // Step 1: Download the MP3 using yt-dlp
            await mediaHelper.DownloadMp3Async(song.YoutubeUrl, mp3Path);

            // Step 2: Download the album art
            if (!string.IsNullOrWhiteSpace(song.AlbumArt))
            {
                await mediaHelper.DownloadAlbumArtAsync(song.AlbumArt, albumArtPath);
            }

            // Step 3: Set metadata using TagLib#
            mediaHelper.SetMp3Metadata(mp3Path, song, albumArtPath);

            // Step 4: Clean up temporary album art file
            if (File.Exists(albumArtPath))
            {
                File.Delete(albumArtPath);
            }

            Debug.WriteLine($"Successfully created MP3 with metadata: {mp3Path}");
        }

    }
}
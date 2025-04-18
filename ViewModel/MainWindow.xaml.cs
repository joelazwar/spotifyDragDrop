using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using spotifyDragDrop.Model;

namespace spotifyDragDrop
{
    /// <summary>  
    /// Interaction logic for MainWindow.xaml  
    /// </summary>  
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
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

        public MainWindow()
        {
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

            if (string.IsNullOrWhiteSpace(url))
            {
                Message = "Invalid URL. Please try again.";
                return;
            }

            try
            {
                var song = await Song.CreateFromSpotifyUrlAsync(url);
                Songs.Add(song);
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
            string mp3Path = System.IO.Path.Combine(outputDirectory, $"{song.Artist} - {song.Title}.mp3");
            string albumArtPath = System.IO.Path.Combine(outputDirectory, $"{song.Title}_cover.jpg");

            // Step 1: Use yt-dlp to download the MP3
            var ytDlpProcessStartInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp", // Ensure yt-dlp is in your PATH or provide the full path
                Arguments = $"-x --audio-format mp3 -o \"{mp3Path}\" {song.YoutubeUrl}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Debug.WriteLine($"Executing yt-dlp: {ytDlpProcessStartInfo.Arguments}");

            using (var ytDlpProcess = new Process { StartInfo = ytDlpProcessStartInfo })
            {
                ytDlpProcess.Start();
                string ytDlpOutput = await ytDlpProcess.StandardOutput.ReadToEndAsync();
                string ytDlpError = await ytDlpProcess.StandardError.ReadToEndAsync();
                await ytDlpProcess.WaitForExitAsync();

                Debug.WriteLine($"yt-dlp Output: {ytDlpOutput}");
                Debug.WriteLine($"yt-dlp Error: {ytDlpError}");

                if (ytDlpProcess.ExitCode != 0)
                {
                    throw new Exception($"yt-dlp failed: {ytDlpError}");
                }
            }

            // Step 2: Download the Spotify album art
            if (!string.IsNullOrWhiteSpace(song.AlbumArt))
            {
                using (var httpClient = new HttpClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(song.AlbumArt);
                    await System.IO.File.WriteAllBytesAsync(albumArtPath, imageBytes);
                }
            }

            // Step 3: Set metadata using TagLib#
            var file = TagLib.File.Create(mp3Path);
            file.Tag.Title = song.Title;
            file.Tag.Performers = new[] { song.Artist };
            file.Tag.Album = song.Album;

            if (System.IO.File.Exists(albumArtPath))
            {
                // Read the album art file into a byte array
                var albumArtBytes = System.IO.File.ReadAllBytes(albumArtPath);

                // Create a TagLib.Picture object for the front cover
                var frontCover = new TagLib.Picture
                {
                    Data = new TagLib.ByteVector(albumArtBytes),
                    Type = TagLib.PictureType.FrontCover,
                    Description = "Album cover",
                    MimeType = "image/jpeg" // Ensure the MIME type matches the file format
                };

                // Assign both pictures to the MP3 file's tag
                file.Tag.Pictures = new[] { frontCover };

                Debug.WriteLine("Album art successfully added to the MP3 file.");
            }

            //Force ID3v2.3 Tag Version
            if (file.TagTypes.HasFlag(TagLib.TagTypes.Id3v2))
            {
                var id3v2Tag = (TagLib.Id3v2.Tag)file.GetTag(TagLib.TagTypes.Id3v2);
                id3v2Tag.Version = 3; // Force ID3v2.3
            }


            file.Save();

            // Step 4: Clean up temporary album art file
            if (System.IO.File.Exists(albumArtPath))
            {
                System.IO.File.Delete(albumArtPath);
            }

            Debug.WriteLine($"Successfully created MP3 with metadata: {mp3Path}");
        }
    }
}
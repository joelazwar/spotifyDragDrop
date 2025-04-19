using System.Windows;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using SpotifyAPI.Web;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Configuration;
using spotifyDragDrop.Services;

namespace spotifyDragDrop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static SpotifyApiService SpotifyClient { get; private set; } = null!;
        public static YouTubeApiService YouTubeClient { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            IConfiguration configuration;

            if (!File.Exists("appsettings.json"))
            {
                MessageBox.Show("Configuration file 'appsettings.json' is missing. Please set it up before running the application.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(); // Gracefully exit the application
                return;
            }

            try
            {
               configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load configuration: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(); // Gracefully exit the application
                return;
            }

            try
            {
                // Initialize the YouTube client
                YouTubeClient = new YouTubeApiService(configuration);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize YouTube API client: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(); // Gracefully exit the application
                return;
            }

            try
            {
                SpotifyClient = new SpotifyApiService(configuration);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize Spotify API client: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(); // Gracefully exit the application
                return;
            }

            // Show the main window
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }


    }


}

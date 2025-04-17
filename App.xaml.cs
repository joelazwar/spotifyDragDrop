using System.Windows;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using SpotifyAPI.Web;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Configuration;

namespace spotifyDragDrop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static SpotifyClient SpotifyClient { get; private set; } = null!;
        public static YouTubeService YouTubeService { get; private set; } = null!;

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

            var ytApiKey = configuration["ApiKey"];
            if (string.IsNullOrEmpty(ytApiKey))
            {
                MessageBox.Show("YouTube API key is missing in appsettings.json",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(); // Gracefully exit the application
                return;
            }

            var spotifyClientId = configuration["ClientId"];
            var spotifyClientSecret = configuration["ClientSecret"];
            if (string.IsNullOrEmpty(spotifyClientId) || string.IsNullOrEmpty(spotifyClientSecret))
            {
                MessageBox.Show("Spotify credentials are missing in appsettings.json",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(); // Gracefully exit the application
                return;
            }

            var spotifyConfig = SpotifyClientConfig.CreateDefault().WithAuthenticator(new ClientCredentialsAuthenticator(spotifyClientId, spotifyClientSecret));
            SpotifyClient = new SpotifyClient(spotifyConfig);

            // Initialize the YouTube client
            YouTubeService = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = ytApiKey,
                ApplicationName = "SpotifyDragDrop"
            });

            // Show the main window
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }


    }


}

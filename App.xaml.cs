using System.Windows;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using SpotifyAPI.Web;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Configuration;
using spotifyDragDrop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace spotifyDragDrop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                // Load configuration
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                // Initialize services
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection, configuration);

                ServiceProvider = serviceCollection.BuildServiceProvider();

                // Show the main window
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                // Display error message and shut down the application
                MessageBox.Show($"Application failed to start: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(); // Gracefully exit the application
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register configuration
            services.AddSingleton(configuration);

            // Register services
            services.AddSingleton<YouTubeApiService>();
            services.AddSingleton<SpotifyApiService>();
            services.AddSingleton<SoundCloudApiService>();
            services.AddTransient<MainWindow>();
        }


    }


}

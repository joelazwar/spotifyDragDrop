using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web;

namespace spotifyDragDrop.Services
{
    public class SpotifyApiService
    {
        private readonly SpotifyClient _spotifyClient;
        /// Constructor
        public SpotifyApiService(IConfiguration configuration)
        {
            var clientId = configuration["ClientId"];
            var clientSecret = configuration["ClientSecret"];
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new Exception("Spotify Client ID or Secret is missing in appsettings.json");
            }
            var config = SpotifyClientConfig.CreateDefault()
                .WithAuthenticator(new ClientCredentialsAuthenticator(clientId, clientSecret));
            _spotifyClient = new SpotifyClient(config);
        }

        public async Task<FullTrack?> GetTrackAsync(string trackId)
        {
            return await _spotifyClient.Tracks.Get(trackId);
        }


    }
}

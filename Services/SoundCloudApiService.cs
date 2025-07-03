using Google.Apis.Services;
using Google.Apis.YouTube.v3;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using spotifyDragDrop.Model;
using static spotifyDragDrop.Model.Song;

namespace spotifyDragDrop.Services
{

    public class SoundCloudTokenResponse
    {
        public string? access_token { get; set; }
        public int expires_in { get; set; }
        public string? token_type { get; set; }
    }

    public class SoundCloudTrack
    {
        public string? title { get; set; }
        public string? artwork_url { get; set; }
        public SoundCloudUser? user { get; set; }
    }

    public class SoundCloudUser
    {
        public string? username { get; set; }
    }
    public class SoundCloudApiService
    {

        private string? _accessToken;
        private DateTime _tokenExpiresAt;

        private readonly string _clientId;
        private readonly string _clientSecret;

        public SoundCloudApiService(IConfiguration configuration)
        {
            _clientId = configuration["SoundCloudClientId"];
            _clientSecret = configuration["SoundCloudClientSecret"];
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                throw new Exception("Spotify credentials is missing in appsettings.json");
            }
        }

        private bool IsTokenValid() =>
        !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiresAt;

        private async Task EnsureTokenAsync()
        {
            if (IsTokenValid()) return;

            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

            var response = await client.PostAsync("https://api.soundcloud.com/oauth2/token", content);
            response.EnsureSuccessStatusCode();
            var tokenResponse = await response.Content.ReadFromJsonAsync<SoundCloudTokenResponse>();


            _accessToken = tokenResponse?.access_token;
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse?.expires_in ?? 0);
        }

        public async Task<string> GetAccessTokenAsync()
        {
            await EnsureTokenAsync();
            return _accessToken!;
        }

        public async Task<SoundCloudTrack> ResolveUrlAsync(string url)
        {
            await GetAccessTokenAsync();
            using var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

            var resolveUrl = $"https://api.soundcloud.com/resolve?url={url}";
            var request = new HttpRequestMessage(HttpMethod.Get, resolveUrl);

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            var response = await httpClient.SendAsync(request);

            if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400 && response.Headers.Location != null)
            {
                // Follow the redirect manually, re-adding the Authorization header
                var redirectRequest = new HttpRequestMessage(HttpMethod.Get, response.Headers.Location);
                redirectRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                response = await httpClient.SendAsync(redirectRequest);
            }

            var json = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to resolve SoundCloud URL.");

            var track = System.Text.Json.JsonSerializer.Deserialize<SoundCloudTrack>(json);

            if (track == null)
                throw new Exception("Track not found or invalid response.");

            return track;


        }
    }
}

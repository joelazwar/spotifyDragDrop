using System.DirectoryServices;
using System.Windows;
using System.Xml;
using Google.Apis.YouTube.v3.Data;
using SpotifyAPI.Web;
using spotifyDragDrop.Services;
using SearchResult = Google.Apis.YouTube.v3.Data.SearchResult;

namespace spotifyDragDrop.Model
{


    public class Song
    {
        public string? Title { get; private set; }
        public string? Artist { get; private set; }
        public string? Album { get; private set; }
        public string? Thumbnail { get; private set; }
        public string? YoutubeUrl { get; private set; }
        public string? AlbumArt { get; private set; }

        private Song()
        {

        }

        // Factory method
        public static async Task<Song> CreateFromSpotifyUrlAsync(string url, YouTubeApiService youTubeClient, SpotifyApiService spotifyClient)
        {
            try
            {
                // 1. Extract the track ID from the URL (e.g. "1pYPgA8XdHFQS15HPB41MH")
                string trackId = ExtractIdFromSpotifyUrl(url);

                // 2. Use the SpotifyClient to get full track data
                var track = await spotifyClient.GetTrackAsync(trackId);

                if (track == null) throw new Exception("Track not found.");

                var ytVideo = await SearchForYoutubeURL(track, youTubeClient, static async () =>
                {
                    return await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var inputDialog = new InputDialog("No matching video found. Please enter a YouTube URL:");
                        if (inputDialog.ShowDialog() == true)
                        {
                            return inputDialog.ResponseText ?? string.Empty; // Ensure a non-null value is returned
                        }
                        return string.Empty; // Return an empty string if the dialog is canceled
                    });
                });

                // 3. Pull metadata from the result and create a new Song
                return new Song
                {
                    Title = track.Name,
                    Artist = track.Artists[0].Name,
                    Album = track.Album.Name,
                    Thumbnail = ytVideo.Snippet.Thumbnails.Default__.Url,
                    YoutubeUrl = $"https://www.youtube.com/watch?v={ytVideo.Id.VideoId}",
                    AlbumArt = track.Album.Images.FirstOrDefault()?.Url,
                };
            }
            catch (APIException ex)
            {
                throw new Exception($"Failed to retrieve track data from Spotify. Please check the URL. {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while creating the song : {ex.Message}", ex);
            }
        }

        // Utility method to extract ID from URL
        private static string ExtractIdFromSpotifyUrl(string url)
        {
            // Example input: https://open.spotify.com/track/abc123?si=xyz
            var uri = new Uri(url);
            var segments = uri.Segments;
            return segments.Last().Split('?')[0]; // Handles trailing query params
        }

        private static async Task<SearchResult> SearchForYoutubeURL(FullTrack track, YouTubeApiService youTubeClient, Func<Task<string>> promptForYoutubeUrl)
        {
            try
            {
                var query = $"{track.Name} {track.Artists[0].Name} - Topic"; // Search for the track name
                var matchingVideo = await youTubeClient.FindVideoWithMatchingDurationAsync(query, TimeSpan.FromMilliseconds(track.DurationMs));


                if (matchingVideo != null) return matchingVideo;

                // If no matching video is found, prompt the user for a YouTube URL
                string youtubeUrl = await promptForYoutubeUrl();
                if (string.IsNullOrWhiteSpace(youtubeUrl))
                {
                    throw new Exception("No YouTube URL provided.");
                }

                // Validate the provided URL
                var videoId = ExtractVideoIdFromUrl(youtubeUrl);
                if (string.IsNullOrWhiteSpace(videoId))
                {
                    throw new Exception("Invalid YouTube URL provided.");
                }

                // Fetch video details for the provided URL
                var manualVideo = await youTubeClient.SearchVideoByIdAsync(videoId);

                if (manualVideo == null)
                {
                    throw new Exception("The provided YouTube URL is invalid or the video does not exist.");
                }

                return new SearchResult
                {
                    Id = new ResourceId { VideoId = videoId },
                    Snippet = new SearchResultSnippet
                    {
                        Title = manualVideo.Snippet.Title,
                        Description = manualVideo.Snippet.Description,
                        ChannelId = manualVideo.Snippet.ChannelId,
                        ChannelTitle = manualVideo.Snippet.ChannelTitle,
                        Thumbnails = manualVideo.Snippet.Thumbnails
                    }
                };
            }
            catch (Google.GoogleApiException ex)
            {
                throw new Exception($"YouTube API error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred: {ex.Message}", ex);
            }
        }

        // Utility method to extract video ID from a YouTube URL
        private static string? ExtractVideoIdFromUrl(string url)
        {
            var uri = new Uri(url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return query["v"]; // Return type is now nullable to match the potential null value.  
        }




    }
}

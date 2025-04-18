using System.DirectoryServices;
using System.Windows;
using System.Xml;
using Google.Apis.YouTube.v3.Data;
using SpotifyAPI.Web;
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
        public static async Task<Song> CreateFromSpotifyUrlAsync(string url)
        {
            try
            {
                // 1. Extract the track ID from the URL (e.g. "1pYPgA8XdHFQS15HPB41MH")
                string trackId = ExtractIdFromSpotifyUrl(url);



                // 2. Use the SpotifyClient to get full track data
                var track = await App.SpotifyClient.Tracks.Get(trackId);

                var ytVideo = await SearchForYoutubeURL(track, static async () =>
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
                throw new Exception($"An unexpected error occurred while creating the song : {ex.Message}", ex);
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

        private static async Task<SearchResult> SearchForYoutubeURL(FullTrack track, Func<Task<string>> promptForYoutubeUrl)
        {
            try
            {
                // Step 2: Search for the track filtered by the channel ID
                var videoSearchRequest = App.YouTubeService.Search.List("snippet");
                videoSearchRequest.Q = $"{track.Name} {track.Artists[0].Name} - Topic"; // Search for the track name
                videoSearchRequest.Type = "video";
                videoSearchRequest.MaxResults = 5;

                var videoSearchResponse = await videoSearchRequest.ExecuteAsync();

                // Get the first video result with matching duration (+- 2 seconds)
                foreach (var video in videoSearchResponse.Items)
                {
                    var videoDetailsRequest = App.YouTubeService.Videos.List("contentDetails");
                    videoDetailsRequest.Id = video.Id.VideoId;

                    var videoDetailsResponse = await videoDetailsRequest.ExecuteAsync();
                    var videoDetails = videoDetailsResponse.Items.FirstOrDefault();

                    if (videoDetails != null)
                    {
                        var videoDuration = XmlConvert.ToTimeSpan(videoDetails.ContentDetails.Duration);
                        var trackDuration = TimeSpan.FromMilliseconds(track.DurationMs);

                        if (Math.Abs((videoDuration - trackDuration).TotalSeconds) <= 10)
                        {
                            return new SearchResult
                            {
                                Id = video.Id,
                                Snippet = new SearchResultSnippet
                                {
                                    Title = video.Snippet.Title,
                                    Description = video.Snippet.Description,
                                    ChannelId = video.Snippet.ChannelId,
                                    ChannelTitle = video.Snippet.ChannelTitle,
                                    Thumbnails = video.Snippet.Thumbnails
                                }
                            };
                        }
                    }
                }

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
                var manualVideoDetailsRequest = App.YouTubeService.Videos.List("snippet,contentDetails");
                manualVideoDetailsRequest.Id = videoId;

                var manualVideoDetailsResponse = await manualVideoDetailsRequest.ExecuteAsync();
                var manualVideo = manualVideoDetailsResponse.Items.FirstOrDefault();

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

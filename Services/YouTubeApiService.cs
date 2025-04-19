using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web.Http;

namespace spotifyDragDrop.Services
{
    public class YouTubeApiService
    {
        private readonly YouTubeService _youTubeClient;

        /// Constructor
        public YouTubeApiService(IConfiguration configuration)
        {
            var apiKey = configuration["ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("YouTube API key is missing in appsettings.json");
            }

            _youTubeClient = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = apiKey,
                ApplicationName = "SpotifyDragDrop"
            });
        }

        public async Task<SearchListResponse?> SearchVideoByQueryAsync(string query, int maxResults = 5)
        {
            var searchRequest = _youTubeClient.Search.List("snippet");
            searchRequest.Q = query;
            searchRequest.Type = "video";
            searchRequest.MaxResults = maxResults;

            var response = await searchRequest.ExecuteAsync();
            if (response?.Items == null || !response.Items.Any())
            {
                Debug.WriteLine("No videos found for the query.");
                return null;
            }

            return response;
        }

        public async Task<Video?> SearchVideoByIdAsync(string id, string part = "snippet")
        {
            var searchRequest = _youTubeClient.Videos.List(part);
            searchRequest.Id = id;

            var video = await searchRequest.ExecuteAsync();
            if (video?.Items == null || !video.Items.Any())
            {
                Debug.WriteLine("No video found.");
                return null;
            }

            return video.Items.FirstOrDefault();
        }

        public async Task<SearchResult?> FindVideoWithMatchingDurationAsync(string query, TimeSpan trackDuration, int maxResults = 5)
        {
            var searchResponse = await SearchVideoByQueryAsync(query, maxResults);

            if (searchResponse != null)
            {
                foreach (var video in searchResponse.Items)
                {
                    var videoDetails = await SearchVideoByIdAsync(video.Id.VideoId, "contentDetails");
                    if (videoDetails != null)
                    {
                        var videoDuration = XmlConvert.ToTimeSpan(videoDetails.ContentDetails.Duration);
                        if (Math.Abs((videoDuration - trackDuration).TotalSeconds) <= 10)
                        {
                            return video;
                        }
                    }
                }
            }

            return null;
        }
    }
}

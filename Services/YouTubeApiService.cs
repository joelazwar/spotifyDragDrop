using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Configuration;

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

            return await searchRequest.ExecuteAsync();
        }

        public async Task<Video?> SearchVideoByIdAsync(string id)
        {
            var searchRequest = _youTubeClient.Videos.List("snippet");
            searchRequest.Id = id;

            var video = await searchRequest.ExecuteAsync();
            return video.Items.FirstOrDefault();
        }

        public async Task<Video?> GetVideoDetailsAsync(string videoId)
        {
            var videoDetailsRequest = _youTubeClient.Videos.List("contentDetails");
            videoDetailsRequest.Id = videoId;

            var videoDetailsResponse = await videoDetailsRequest.ExecuteAsync();
            return videoDetailsResponse.Items.FirstOrDefault();
        }

        public async Task<SearchResult?> FindVideoWithMatchingDurationAsync(string query, TimeSpan trackDuration, int maxResults = 5)
        {
            var searchResponse = await SearchVideoByQueryAsync(query, maxResults);

            if (searchResponse != null)
            {
                foreach (var video in searchResponse.Items)
                {
                    var videoDetails = await GetVideoDetailsAsync(video.Id.VideoId);
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

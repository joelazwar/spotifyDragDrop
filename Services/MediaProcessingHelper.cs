using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TagLib;
using spotifyDragDrop.Model;
using File = System.IO.File;

namespace spotifyDragDrop.Services
{
    public class MediaProcessingHelper
    {
        /// <summary>
        /// Downloads an MP3 file from YouTube using yt-dlp.
        /// </summary>
        public async Task DownloadMp3Async(string youtubeUrl, string outputPath)
        {
            var ytDlpProcessStartInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp", // Ensure yt-dlp is in your PATH or provide the full path
                Arguments = $"-x --audio-format mp3 -o \"{outputPath}\" {youtubeUrl}",
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
        }

        /// <summary>
        /// Downloads album art from a given URL.
        /// </summary>
        public async Task DownloadAlbumArtAsync(string albumArtUrl, string outputPath)
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(albumArtUrl);
                    await File.WriteAllBytesAsync(outputPath, imageBytes);
                    Debug.WriteLine($"Album art downloaded to: {outputPath}");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to download album art: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Sets metadata for an MP3 file using TagLib#.
        /// </summary>
        public void SetMp3Metadata(string mp3Path, Song song, string albumArtPath)
        {
            var file = TagLib.File.Create(mp3Path);
            file.Tag.Title = song.Title;
            file.Tag.Performers = new[] { song.Artist };
            file.Tag.Album = song.Album;

            if (File.Exists(albumArtPath))
            {
                // Read the album art file into a byte array
                var albumArtBytes = File.ReadAllBytes(albumArtPath);

                // Create a TagLib.Picture object for the front cover
                var frontCover = new TagLib.Picture
                {
                    Data = new TagLib.ByteVector(albumArtBytes),
                    Type = TagLib.PictureType.FrontCover,
                    Description = "Album cover",
                    MimeType = "image/jpeg" // Ensure the MIME type matches the file format
                };

                // Assign the picture to the MP3 file's tag
                file.Tag.Pictures = new[] { frontCover };

                Debug.WriteLine("Album art successfully added to the MP3 file.");
            }

            // Force ID3v2.3 Tag Version
            if (file.TagTypes.HasFlag(TagLib.TagTypes.Id3v2))
            {
                var id3v2Tag = (TagLib.Id3v2.Tag)file.GetTag(TagLib.TagTypes.Id3v2);
                id3v2Tag.Version = 3; // Set to any preferable version
            }

            file.Save();
            Debug.WriteLine($"Metadata successfully set for: {mp3Path}");
        }

        public static string DetermineSourceFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);

                if (uri.Host.Contains("spotify.com"))
                {
                    return "Spotify";
                }
                else if (uri.Host.Contains("soundcloud.com"))
                {
                    return "SoundCloud";
                }
                else
                {
                    return "Unknown";
                }
            }
            catch (UriFormatException)
            {
                return "Invalid";
            }
        }
    }
}

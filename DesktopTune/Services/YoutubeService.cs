using Microsoft.Extensions.Primitives;
using System.Drawing;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;

public class YouTubeService
{
    private readonly YoutubeClient _client = new YoutubeClient();

    public async Task<(bool IsValid, string VideoId, string Title, TimeSpan Duration, string Thumbnail,string AuthorName)?> GetVideoInfo(string url)
    {
        if (!IsYouTubeUrl(url))
            return null;

        try
        {
            var videoId = VideoId.TryParse(url);
            if (videoId == null)
                return null;

            var video = await _client.Videos.GetAsync(videoId.Value);

            if (video.Duration.HasValue && video.Duration.Value > TimeSpan.FromMinutes(6))
                return null;
            var thumbUrl = video.Thumbnails.GetWithHighestResolution()?.Url;
            return (true, video.Id.Value, video.Title, video.Duration ?? TimeSpan.Zero, thumbUrl ?? "",video.Author.ChannelTitle ?? "");
        }
        catch(Exception ex)
        {
            return null;
        }
    }

    private bool IsYouTubeUrl(string url)
    {
        return Regex.IsMatch(url, @"^(https?\:\/\/)?(www\.)?(youtube\.com|youtu\.be)\/.+$");
    }


    private static async Task<BitmapImage> LoadImageFromUrlAsync(string url)
    {
        using var client = new HttpClient();
        await using var stream = await client.GetStreamAsync(url);

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();

        return bitmap;
    }

}

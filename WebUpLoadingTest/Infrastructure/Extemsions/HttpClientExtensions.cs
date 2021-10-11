using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebUpLoadingTest.Infrastructure.Extemsions
{
    public static class HttpClientExtensions
    {
        public static async Task HttpDownloadFileAsync(this HttpClient Client, string url, string FileName, CancellationToken Cancel = default)
        {
            using var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, Cancel);
            await using var source = await response.Content.ReadAsStreamAsync(Cancel);
            await using var destination = System.IO.File.Open(FileName, FileMode.Create);
            await source.CopyToAsync(destination, Cancel);
        }
    }
}

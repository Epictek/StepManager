using System.IO.Compression;
using AngleSharp;
using StepManager.Services;

namespace StepManager.DownloadSources;

public class SMOSource
{
    private readonly SongManager SongManager;
    private readonly HttpClient HttpClient;
    private readonly IBrowsingContext context;
    private readonly SettingsService SettingsService;

    public SMOSource(SongManager songManager, HttpClient httpClient, SettingsService settingsService)
    {
        SongManager = songManager;
        HttpClient = httpClient;
        SettingsService = settingsService;
        var config = Configuration.Default.WithDefaultLoader();
        context = BrowsingContext.New(config);

    }


    
    public async Task<IEnumerable<(string Name, string Id)>> ListPacks()
    {

        var catListUrl = "https://search.stepmaniaonline.net/packs/_";

        var document = await context.OpenAsync(catListUrl);
        return document.QuerySelectorAll("tbody tr")
            .Select(x => (Name: x.FirstElementChild.FirstElementChild.TextContent, Id: x.FirstElementChild.FirstElementChild.GetAttribute("href").Split('/').Last()));

    }

    
    public async Task<IEnumerable<SongDetails>> SearchPacksTitle(string title)
    {
            var catListUrl = $"https://search.stepmaniaonline.net/title/{title}";

            var document = await context.OpenAsync(catListUrl);

            var simTdSelector = "html body div.container table.table tbody tr";

            var SongList = document.QuerySelectorAll(simTdSelector).Select(x =>
                new SongDetails(
                    title: x.Children[0].TextContent,
                    artist: x.Children[1].TextContent,
                    subTitle: x.Children[2].TextContent,
                    credit: x.Children[3].TextContent,
                    banner: x.Children[4].TextContent,
                    lastUpdate: DateTime.Parse(x.Children[5].TextContent)
                )
            );
            
            return SongList;
    }

    public async Task<IEnumerable<SongDetails>> SearchPacksArtist(string artist)
    {
        var catListUrl = $"https://search.stepmaniaonline.net/artist/{artist}";

        var document = await context.OpenAsync(catListUrl);

        var simTdSelector = "html body div.container table.table tbody tr";

        var SongList = document.QuerySelectorAll(simTdSelector).Select(x =>
            new SongDetails(
                title: x.Children[0].TextContent,
                artist: x.Children[1].TextContent,
                subTitle: x.Children[2].TextContent,
                credit: x.Children[3].TextContent,
                banner: x.Children[4].TextContent,
                lastUpdate: DateTime.Parse(x.Children[5].TextContent)
            )
        );
            
        return SongList;
    }

    public async Task<PackDetails> GetPackDetails(string id)
    {
        var url = $"https://search.stepmaniaonline.net/pack/id/{id}";

        var document = await context.OpenAsync(url);

        var titleSelector = "html body div.row div.col-lg-8.col-lg-offset-2 form div.input-group input#search.form-control";
        var title = document.QuerySelector(titleSelector).GetAttribute("value");

        var simTdSelector = "html body div.container table.table tbody tr";

        var SongList = document.QuerySelectorAll(simTdSelector).Select(x =>
            new SongDetails(
                title: x.Children[0].TextContent,
                artist: x.Children[1].TextContent,
                subTitle: x.Children[2].TextContent,
                credit: x.Children[3].TextContent,
                banner: x.Children[4].TextContent,
                lastUpdate: DateTime.Parse(x.Children[5].TextContent)
            )
        );

        return new PackDetails(title, id, null, SongList);
    }
    
    float GetProgressPercentage (float totalBytes, float currentBytes) => (totalBytes / currentBytes) * 100f;
    
    public async Task DownloadPack(PackDetails pack, IProgress<float>? progress, CancellationToken cancellationToken = default)
    {
        // Snackbar.Add($"Downloading pack {Title}", Severity.Info);
        
        var url = $"https://search.stepmaniaonline.net/pack/id/{pack.id}";
        var document = await context.OpenAsync(url);

        var downloadUrl = $"https://search.stepmaniaonline.net{document.QuerySelector("html body div.container h4 a")?.GetAttribute("href")}";

        var response = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, downloadUrl)
        {
            Headers = { Referrer = new Uri(downloadUrl) } 
            
        });
        var zip = Path.Join(SettingsService.Settings.StepManiaSongPath, $"{pack.Id}.zip");

        await using (var fs = new FileStream(zip, FileMode.CreateNew))
        {

            var contentLength = response.Content.Headers.ContentLength;
            using (var download = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                // no progress... no contentLength... very sad
                if (progress is null || !contentLength.HasValue)
                {
                    await download.CopyToAsync(fs);
                    return;
                }

                // Such progress and contentLength much reporting Wow!
                var progressWrapper = new Progress<long>(totalBytes =>
                    progress.Report(GetProgressPercentage(totalBytes, contentLength.Value)));
                await CopyToAsync(download, fs, 81920, progressWrapper, cancellationToken);
            }
        }

        ZipFile.ExtractToDirectory(zip, SettingsService.Settings.StepManiaSongPath, true);
        

        File.Delete(zip);
    }
    
    
    
    static async Task CopyToAsync (Stream source, Stream destination, int bufferSize, IProgress<long> progress = null, CancellationToken cancellationToken = default (CancellationToken))
    {
        if (bufferSize < 0)
            throw new ArgumentOutOfRangeException (nameof (bufferSize));
        if (source is null)
            throw new ArgumentNullException (nameof (source));
        if (!source.CanRead)
            throw new InvalidOperationException ($"'{nameof (source)}' is not readable.");
        if (destination == null)
            throw new ArgumentNullException (nameof (destination));
        if (!destination.CanWrite)
            throw new InvalidOperationException ($"'{nameof (destination)}' is not writable.");

        var buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync (buffer, 0, buffer.Length, cancellationToken).ConfigureAwait (false)) != 0) {
            await destination.WriteAsync (buffer, 0, bytesRead, cancellationToken).ConfigureAwait (false);
            totalBytesRead += bytesRead;
            progress?.Report (totalBytesRead);
        }
    }
}
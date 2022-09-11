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

    
    public async Task<List<(string name, string id, string pack, string packid)>> SearchPacks(string title, string artist)
    {
        try
        {
            var catListUrl = "https://zenius-i-vanisher.com/v5.2/simfiles_search_ajax.php";
            var data = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "songtitle", title },
                    { "songartist", artist }
                }
            );
            var response = await HttpClient.PostAsync(catListUrl, data);
            using var document =
                await context.OpenAsync(async req => req.Content(await response.Content.ReadAsStringAsync()));
            var rows = document.QuerySelectorAll("tr");
            
            if (rows != null && rows.Any()) {
                return rows.Where(x=> x.FirstElementChild?.NodeName == "TD" && x.FirstElementChild.ChildElementCount > 0).Select(x => (
                    name: x.Children[0].FirstElementChild.TextContent,
                    id: x.Children[0].FirstElementChild.GetAttribute("href").Split('=')[1],
                    pack: x.Children[1].FirstElementChild.TextContent,
                    packid: x.Children[1].FirstElementChild.GetAttribute("href").Split('=')[1]
                )).ToList();}
            
        }
        catch (Exception ex)
        {
        }
        return new List<(string name, string id, string pack, string packid)>();

    }

    async Task DownloadBanner(string bannerUrl, string savePath)
    {
        await using var bannerStream = await HttpClient.GetStreamAsync(bannerUrl);

        var ext = Path.GetExtension(bannerUrl);
        if (ext.Contains('?')) ext = ext.Split('?')[0];
        
        var banner = Path.Join(savePath, $"banner{ext}");
        await using var bannerfs = new FileStream(banner, FileMode.CreateNew);
        await bannerStream.CopyToAsync(bannerfs);
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
        var imgUrl = document.QuerySelector("html body div.fixwidth div.content p.centre img")?.GetAttribute("src");
        imgUrl = imgUrl?.Replace("..", "https://zenius-i-vanisher.com/");

        
        return new PackDetails(title, id,imgUrl, SongList);


    }
    public async Task DownloadPack(PackDetails pack)
    {
        // Snackbar.Add($"Downloading pack {Title}", Severity.Info);
        
        var url = $"https://search.stepmaniaonline.net/pack/id/{pack.id}";
        var document = await context.OpenAsync(url);

        var downloadUrl = $"https://search.stepmaniaonline.net{document.QuerySelector("html body div.container h4 a")?.GetAttribute("href")}";

        var response = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, downloadUrl)
            { Headers = { Referrer = new Uri(downloadUrl) } });


        await using var s = await response.Content.ReadAsStreamAsync();

        var zip = Path.Join(SettingsService.Settings.StepManiaSongPath, $"{pack.Id}.zip");
        await using var fs = new FileStream(zip, FileMode.CreateNew);
        await s.CopyToAsync(fs);
        // File.Delete(zip);
    }
}
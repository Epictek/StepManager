using AngleSharp;
using StepDownServer.Services;

namespace StepDownServer.DownloadSources;

public class ZenSource
{
    private readonly SongManager SongManager;
    private readonly HttpClient HttpClient;
    private readonly IBrowsingContext context;

    public ZenSource(SongManager songManager, HttpClient httpClient)
    {
        SongManager = songManager;
        HttpClient = httpClient;
        var config = Configuration.Default.WithDefaultLoader();
        context = BrowsingContext.New(config);

    }


    public record PackDetails(string Name, string id, string? BannerUrl,
        IEnumerable<(string Id, string Name, string? Url, string? Title)> Songs)
    {
        public string Name = Name;
        public string Id = id;
        public string? BannerUrl = BannerUrl;
        public IEnumerable<(string Id, string Name, string? Url, string? Title)> Songs = Songs;
    }
    
    public async Task<IEnumerable<(string Name, string Id)>> ListPacks()
    {

        var catListUrl = "https://zenius-i-vanisher.com/v5.2/simfiles.php?category=simfiles";

        var document = await context.OpenAsync(catListUrl);
        return document.QuerySelectorAll("option").Where(x => x.GetAttribute("value") != "0")
            .Select(x => (Name: x.TextContent, Id: x.GetAttribute("value")!));

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
        var url = $"https://zenius-i-vanisher.com/v5.2/viewsimfilecategory.php?categoryid={id}";

        var document = await context.OpenAsync(url);

        var titleSelector = "html body div.fixwidth div.headertop h1";
        var title = document.QuerySelector(titleSelector).TextContent;

        var simTdSelector = "html body div.fixwidth div.content table tbody tr td strong a";

        var SongList = document.QuerySelectorAll(simTdSelector)
            .Where(x => x.GetAttribute("href").StartsWith("viewsimfile.php")).
            Select(x => (Id: x.GetAttribute("href").Split('=')[1] , Name : x.TextContent, Url: x.GetAttribute("href"), Title: x.GetAttribute("title")));

        var imgUrl = document.QuerySelector("html body div.fixwidth div.content p.centre img")?.GetAttribute("src");
        imgUrl = imgUrl?.Replace("..", "https://zenius-i-vanisher.com/");

        
        return new PackDetails(title, id,imgUrl, SongList);


    }
    public async Task DownloadPack(PackDetails pack)
    {
        // Snackbar.Add($"Downloading pack {Title}", Severity.Info);
        var songPath = SongManager.CreatePackDir(pack.Name);

        if (!string.IsNullOrEmpty(pack.BannerUrl))
        {
            _ = DownloadBanner(pack.BannerUrl, songPath);
        }

        foreach (var song in pack.Songs)
        {
            // Snackbar.Add($"Downloading {sname}", Severity.Info);

            var downloadUrl = $"https://zenius-i-vanisher.com/v5.2/download.php?type=ddrsimfile&simfileid={song.Id}";
            
            await using var s = await HttpClient.GetStreamAsync(downloadUrl);

            var zip = Path.Join(songPath, $"{song.Id}.zip");
            await using var fs = new FileStream(zip, FileMode.CreateNew);
            await s.CopyToAsync(fs);
            // Snackbar.Add($"Finished downloading {sname}", Severity.Success);
        }
    }
    
    
    
}
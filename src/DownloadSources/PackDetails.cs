namespace StepManager.DownloadSources;

public record PackDetails(string Name, string id, string? BannerUrl,
    IEnumerable<SongDetails> Songs)
{
    public string Name = Name;
    public string Id = id;
    public string? BannerUrl = BannerUrl;
    public IEnumerable<SongDetails> Songs = Songs;
}


public record SongDetails()
{
    public string Id;
    public string Title ;
    public string Artist ;
    public string SubTitle ;
    public string Credit ;
    public string Banner ;
    public DateTime LastUpdate;

    public SongDetails(string title, string artist, string subTitle, string credit, string banner, DateTime lastUpdate) : this()
    {
        Title = title;
        Artist = artist;
        SubTitle = subTitle;
        Credit = credit;
        Banner = banner;
        LastUpdate = lastUpdate;
    }
    
}





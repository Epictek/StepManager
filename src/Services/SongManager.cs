namespace StepManager.Services;

public class SongManager
{
    private readonly SettingsService SettingsService;
    
    public SongManager(SettingsService settingsService)
    {
        SettingsService = settingsService;
    }
    public string CreatePackDir(string packTitle)
    {
        var packPath = Path.Join(SettingsService.Settings.StepManiaSongPath, packTitle); 
        Directory.CreateDirectory(packPath);
        return packPath;
    }
}
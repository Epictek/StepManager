namespace StepManager.Services;
using Tomlet;
using Directories.Net;

public class SettingsService
{
    public StepManagerSettings Settings = new StepManagerSettings();
    private string configPath;
    
    public SettingsService()
    {

        string tomlString;
        var usrDirs = new BaseDirectories();
        var configDir = Path.Combine(usrDirs.ConfigDir, System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "StepManager");
        configPath = Path.Combine(configDir, "config.toml");

        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }

        if (!File.Exists(configPath))
        {
            File.Create(configPath).Close();

            if (OperatingSystem.IsWindows())
            {
                Settings.StepManiaRootPath = @"C:\Games\Project OutFox";
            }
            else if (OperatingSystem.IsLinux())
            {
                Settings.StepManiaRootPath = $"{usrDirs.HomeDir}/.OutFox";
            }

            var doc = TomletMain.DocumentFrom(Settings);
            File.WriteAllTextAsync(configPath, doc.SerializedValue);


        }
        else
        {

            var document = TomlParser.ParseFile(configPath);

            Settings = TomletMain.To<StepManagerSettings>(document);
        }
    }

    public void Save()
    {
        var doc = TomletMain.DocumentFrom(Settings);
        File.WriteAllTextAsync(configPath, doc.SerializedValue);
    }
    
    

}

public class StepManagerSettings {
    public string StepManiaRootPath;
    public string StepManiaSongPath => Path.Combine(StepManiaRootPath, "Songs");

    public StepManagerSettings()
    {
        
    }
}
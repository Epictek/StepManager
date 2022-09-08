namespace StepDownServer.Services;

public class SongManager
{
    public string CreatePackDir(string packTitle)
    {
        var songPath = @"C:\Games\Project OutFox\Songs";

        var packPath = Path.Join(songPath, packTitle); 
        Directory.CreateDirectory(packPath);
        return packPath;
    }
}
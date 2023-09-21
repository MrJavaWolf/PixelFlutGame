using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

public class StickFigureWorldImporter
{
    public static StickFigureWorldExport LoadWorldData()
    {
        string json = File.ReadAllText("stickfigure_world.json", Encoding.UTF8);
        return JsonSerializer.Deserialize<StickFigureWorldExport>(json) ?? throw new Exception("Failed to load Stick Figure World data");
    }
}

[Serializable]
public class StickFigureWorldExport
{
    public List<StickFigureSquareExport> Squares = new List<StickFigureSquareExport>();
    public List<StickFigureRespawnPointExport> RespawnPoints = new List<StickFigureRespawnPointExport>();
}

[Serializable]
public class StickFigureSquareExport
{
    public float X;
    public float Y;
    public float Height;
    public float Width;
}

[Serializable]
public class StickFigureRespawnPointExport
{
    public float X;
    public float Y;
}






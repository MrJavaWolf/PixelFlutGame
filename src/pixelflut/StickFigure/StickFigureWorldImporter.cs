using System.Text;
using System.Text.Json;

public class StickFigureWorldImporter
{
    public static StickFigureWorldData LoadWorldData()
    {
        string json = File.ReadAllText("stickfigure_world.json", Encoding.UTF8);
        return JsonSerializer.Deserialize<StickFigureWorldData>(json) ?? throw new Exception("Failed to load Stick Figure World data");
    }
}

[Serializable]
public class StickFigureWorldData
{
    public List<StickFigureSquareData> Squares = new List<StickFigureSquareData>();
    public List<StickFigureRespawnPointData> RespawnPoints = new List<StickFigureRespawnPointData>();
}

[Serializable]
public class StickFigureSquareData
{
    public float X;
    public float Y;
    public float Height;
    public float Width;
}

[Serializable]
public class StickFigureRespawnPointData
{
    public float X;
    public float Y;
}






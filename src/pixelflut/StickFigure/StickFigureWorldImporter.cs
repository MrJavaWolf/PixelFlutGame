using System.Text;
using System.Text.Json;

public class StickFigureWorldImporter
{
    public static StickFigureWorldData LoadWorldData()
    {
        
        string json = File.ReadAllText(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "stickfigure_world.json"), Encoding.UTF8);
        StickFigureWorldData data = JsonSerializer.Deserialize<StickFigureWorldData>(json) ?? throw new Exception("Failed to load Stick Figure World data");
        return data;
    }
}

[Serializable]
public class StickFigureWorldData
{

    public List<StickFigureSquareData> Squares { get; set; } = new List<StickFigureSquareData>();
    public List<StickFigureRespawnPointData> RespawnPoints { get; set; } = new List<StickFigureRespawnPointData>();
}

[Serializable]
public class StickFigureSquareData
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Height { get; set; }
    public float Width { get; set; }
}

[Serializable]
public class StickFigureRespawnPointData
{
    public float X { get; set; }
    public float Y { get; set; }
}






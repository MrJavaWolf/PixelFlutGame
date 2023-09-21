using Humper;
using System.Collections.Generic;
using System.Numerics;
namespace StickFigureGame;

public class StickFigureWorld
{
    public List<StickFigureRespawnPointExport> SpawnPoints = new List<StickFigureRespawnPointExport>();
    public Vector2 WorldMinimum = new Vector2(-20, -20);
    public Vector2 WorldMaximum = new Vector2(1000, 100);
    public World BoxWorld { get; } = new World(100, 100);
    public List<IBox> WorldBoxes { get; } = new List<IBox>();
    public List<StickFigureBox> WorldStickfigureBoxes { get; } = new List<StickFigureBox>();
    public List<StickFigureCharacterController> Players { get; } = new List<StickFigureCharacterController>();
    public List<StickFigureProjectile> Projectiles { get; } = new List<StickFigureProjectile>();

    // Start is called before the first frame update
    void Start()
    {
        WorldStickfigureBoxes.AddRange(FindObjectsByType<StickFigureBox>(sortMode: FindObjectsSortMode.None));
        foreach (var worldBox in WorldStickfigureBoxes)
        {
            IBox box = BoxWorld.Create(
                worldBox.transform.position.x,
                worldBox.transform.position.y,
                worldBox.transform.localScale.x,
                worldBox.transform.localScale.y);
            worldBox.Box = box;
            WorldBoxes.Add(box);
        }
    }


    void Update()
    {
        foreach (var worldBox in WorldBoxes)
        {
            DebugDrawBox(worldBox);
        }
    }

    public void DebugDrawBox(IBox box)
    {
        DebugDrawBox(box.X, box.Y, box.Width, box.Height);
    }

    public void DebugDrawBox(float x, float y, float w, float h)
    {
        Debug.DrawLine(new Vector2(x, y), new Vector2(x + w, y));
        Debug.DrawLine(new Vector2(x, y), new Vector2(x, y + h));
        Debug.DrawLine(new Vector2(x + w, y), new Vector2(x + w, y + h));
        Debug.DrawLine(new Vector2(x, y + h), new Vector2(x + w, y + h));
    }
}

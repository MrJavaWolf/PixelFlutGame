using DynamicData;
using Humper;
using System.Numerics;
namespace StickFigureGame;

public class StickFigureWorld
{
    public List<Vector2> SpawnPoints = new();
    public Vector2 WorldMinimum = new Vector2(-20, -20);
    public Vector2 WorldMaximum = new Vector2(1000, 100);
    public World BoxWorld { get; } = new World(100, 100);
    public List<IBox> WorldBoxes { get; } = new List<IBox>();
    public List<StickFigureCharacterController> Players { get; } = new List<StickFigureCharacterController>();
    public List<StickFigureProjectile> Projectiles { get; } = new List<StickFigureProjectile>();
    public List<StickFigureExplosionEffect> Explosions { get; } = new List<StickFigureExplosionEffect>();
    

    // Start is called before the first frame update
    public StickFigureWorld(StickFigureWorldData stickFigureWorldData)
    {
        foreach (var square in stickFigureWorldData.Squares)
        {
            IBox newBox = BoxWorld.Create(
                square.X,
                square.Y,
                square.Width,
                square.Height);
            WorldBoxes.Add(newBox);
        }

        foreach (var spawnPoint in stickFigureWorldData.RespawnPoints)
        {
            SpawnPoints.Add(new Vector2(spawnPoint.X, spawnPoint.Y));
        }
    }

    //void Update()
    //{
    //    foreach (var worldBox in WorldBoxes)
    //    {
    //        DebugDrawBox(worldBox);
    //    }
    //}

    //public void DebugDrawBox(IBox box)
    //{
    //    DebugDrawBox(box.X, box.Y, box.Width, box.Height);
    //}

    //public void DebugDrawBox(float x, float y, float w, float h)
    //{
    //    Debug.DrawLine(new Vector2(x, y), new Vector2(x + w, y));
    //    Debug.DrawLine(new Vector2(x, y), new Vector2(x, y + h));
    //    Debug.DrawLine(new Vector2(x + w, y), new Vector2(x + w, y + h));
    //    Debug.DrawLine(new Vector2(x, y + h), new Vector2(x + w, y + h));
    //}
}

using Godot;
using Godot.Sharp.Extras;
using Physics.Mains;
using Physics.Mains.v1;
using Physics.Mains.v2;
using Physics.Mains.v3;
using Physics.Mains.v4;
using Physics.Mains.v5;
using Physics.Mains.v7_Game;

namespace Physics;

public partial class Main : Node2D
{
    public const int TEAM_1 = 15_000;
    public const int TEAM_2 = 2_000;

    public static Main Instance { get; private set; } = null!;

    [NodePath] public Camera2D Camera2D { get; set; }
    [NodePath] public ColorRect Background { get; set; } = null!;
    [NodePath] public Node2D QuadtreeLines { get; set; } = null!;

    public IGameLoop gameLoop;

    public override void _Ready()
    {
        this.OnReady();
        Instance = this;

        Camera2D.Position = Background.Size / 2f;

        //gameLoop = new Main1(this, Background.Size);
        //gameLoop = new MainThreadInsert(this, Background.Size);
        //gameLoop = new MainMultimesh(this, Background.Size);
        //gameLoop = new MainMultimeshThreads(this, Background.Size);
        //gameLoop = new MainArch(this, Background.Size);
        //gameLoop = new MainArchSystems(this, Background.Size);
        gameLoop = new MainGame(this, Background.Size);

        //gameLoop = new MainPhysicsServer(this, Background.Size);
        gameLoop.OnReady();
        gameLoop.AddParticles(TEAM_2, team: 2, detectionMask: 1, collisionLayer: 0, new Color(0, 0, 1));
        gameLoop.AddParticles(TEAM_1, team: 1, detectionMask: 0, collisionLayer: 1, new Color(1, 0, 0));
        gameLoop.Start();
    }

    //public override void _Process(double delta)
    //{
    //    gameLoop.Process(delta);
    //}

    public override void _PhysicsProcess(double delta)
    {
        gameLoop.PhysicsProcess(delta);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton)
        {
            InputEventMouseButton emb = (InputEventMouseButton) @event;
            if (emb.IsPressed())
            {
                if (emb.ButtonIndex == MouseButton.Right)
                {
                }
                if (emb.ButtonIndex == MouseButton.WheelUp)
                {
                    Camera2D.Zoom *= 1.1f;
                }
                if (emb.ButtonIndex == MouseButton.WheelDown)
                {
                    Camera2D.Zoom /= 1.1f;
                }
            }
        }
    }


}

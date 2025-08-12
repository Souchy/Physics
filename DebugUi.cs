using Godot;
using Godot.Sharp.Extras;
using System;

public partial class DebugUi : Control
{

    [NodePath] public Label LblFps { get; set; }
    [NodePath] public Label LblMouse { get; set; }
    [NodePath] public Label LblCount { get; set; }
    [NodePath] public Button BtnAdd100 { get; set; }
    [NodePath] public Button BtnRem100 { get; set; }
    private double totalFpsOverTime;
    private int frames;
    private int actualFrames;

    public override void _Ready()
    {
        this.OnReady();
        BtnAdd100.Pressed += () => Main.Instance.AddParticles(100);
        BtnRem100.Pressed += () => Main.Instance.RemoveParticles(100);
    }

    public override void _Process(double delta)
    {
        LblFps.Text = $"FPS: {Engine.GetFramesPerSecond()}";
        LblMouse.Text = $"Mouse: {GetGlobalMousePosition().Round()}";
        LblCount.Text = $"Count: {Main.Instance.particles.Count}";

        // Update FPS over time
        frames++;
        if (frames > 1 * 60 && frames < 30 * 60) //Main.Instance.particles.Count >= 3000)
        {
            totalFpsOverTime += Engine.GetFramesPerSecond();
            actualFrames++;
        }
        double averageFps = totalFpsOverTime / actualFrames;
        LblFps.Text += $"\nAvg FPS: {averageFps:F2}";
    }
}

using Godot;

namespace Physics.Mains;

public interface IGameLoop
{
    public int ParticleCount { get; }
    public void OnReady();
    public void Start();
    public void PhysicsProcess(double delta);
    public void Process(double delta) { }
    public void Draw() { }
    public void AddParticles(int count, int team, int detectionMask, int collisionLayer, Color color);
    public void AddParticles(int count)
    {
        AddParticles(count, team: 1, detectionMask: 0, collisionLayer: 1, new Color(0, 0, 1));
    }
    public void RemoveParticles(int count);

}

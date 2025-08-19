using Godot;
using Physics.Mains.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics.Mains.v3_Multimesh;

public enum MultimeshSpawnerFlags
{
    None = 0,
    Color = 1,
    CustomData = 2,
    All = Color | CustomData,
}

public class MultimeshSpawner
{
    public MultiMeshInstance2D MultiMeshInstance;
    public MultiMesh Multimesh;
    public int VisibleCount
    {
        get; // => Multimesh.VisibleInstanceCount;
        set; // => Multimesh.VisibleInstanceCount = value;
    }

    public MultimeshSpawner(Texture2D texture, Vector2 quadSize, MultimeshSpawnerFlags flags = MultimeshSpawnerFlags.None, int startCount = 0)
    {
        MultiMeshInstance = new MultiMeshInstance2D()
        {
            //Name = texture.ResourceName,
            Texture = texture,
            Multimesh = new MultiMesh()
            {
                TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
                InstanceCount = startCount,
                VisibleInstanceCount = 0,
                UseColors = (flags & MultimeshSpawnerFlags.Color) > 0,
                UseCustomData = (flags & MultimeshSpawnerFlags.CustomData) > 0,
                Mesh = new QuadMesh()
                {
                    Size = quadSize,
                },
            },
        };
        Multimesh = MultiMeshInstance.Multimesh;
    }

    public void AddInstances(int count)
    {
        // Manage visible instances
        //int toVisible = Multimesh.InstanceCount - Multimesh.VisibleInstanceCount;
        int toVisible = Multimesh.InstanceCount - VisibleCount;
        toVisible = Math.Min(toVisible, count);
        //Multimesh.VisibleInstanceCount += toVisible;
        VisibleCount += toVisible;
        count -= toVisible;

        Multimesh.InstanceCount += count;
        Multimesh.VisibleInstanceCount += count;
        VisibleCount += count;
    }

    public void RemoveInstances(int count)
    {
        //Multimesh.VisibleInstanceCount = Math.Max(0, Multimesh.VisibleInstanceCount - count);

        int newVisibleCount  = Math.Max(0, VisibleCount - count);
        count = VisibleCount - newVisibleCount;
        VisibleCount = newVisibleCount;

        Multimesh.VisibleInstanceCount = VisibleCount;
        // make it black
        //for (int i = 0; i < count; i++)
        //{
        //    int index = VisibleCount + i;
        //    Multimesh.SetInstanceColor(index, new Color(0, 1, 0, 1));
        //}
    }

    public void RemoveInstance()
    {
        RemoveInstances(1);
    }

    public void UpdateInstance(int i, Vector2 position, Vector2 velocity)
    {
        Multimesh.SetInstanceTransform2D(i, new Transform2D(velocity.Angle(), position));
    }
    public void UpdateInstance(int i, Vector2 position, Vector2 velocity, Color color)
    {
        UpdateInstance(i, position, velocity);
        Multimesh.SetInstanceColor(i, color);
    }
    public void UpdateInstance(int i, Vector2 position, Vector2 velocity, Color color, Color customData)
    {
        UpdateInstance(i, position, velocity, color);
        Multimesh.SetInstanceCustomData(i, customData);
    }

    public int CurrentIndex { get; set; } = 0;
    public void UpdateInstance(Vector2 position, Vector2 velocity, Color color)
    {
        UpdateInstance(CurrentIndex, position, velocity, color);
        CurrentIndex++;
    }
    public void UpdateInstance(Vector2 position, Vector2 velocity, Color color, Color customData)
    {
        UpdateInstance(CurrentIndex, position, velocity, color, customData);
        CurrentIndex++;
    }

}

using Godot;
using Physics.Mains.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vector2N = System.Numerics.Vector2;
using Vector2G = Godot.Vector2;

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
    public float[] buffer;
    public int stride;
    public bool UseBuffer = true;

    public int CurrentInstance { get; set; } = 0;
    private int WriteIndex = 0;

    public int VisibleCount
    {
        get; // => Multimesh.VisibleInstanceCount;
        set; // => Multimesh.VisibleInstanceCount = value;
    }

    public MultimeshSpawner(Texture2D texture, Vector2G quadSize, MultimeshSpawnerFlags flags = MultimeshSpawnerFlags.None, int startCount = 0)
    {
        bool useColors = (flags & MultimeshSpawnerFlags.Color) > 0;
        bool useCustomData = (flags & MultimeshSpawnerFlags.CustomData) > 0;
        MultiMeshInstance = new MultiMeshInstance2D()
        {
            Name = texture.ResourcePath.Split("/").Last(),
            Texture = texture,
            Multimesh = new MultiMesh()
            {
                TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
                InstanceCount = startCount,
                VisibleInstanceCount = 0,
                UseColors = useColors,
                UseCustomData = useCustomData,
                Mesh = new QuadMesh()
                {
                    Size = quadSize,
                },
            },
        };
        Multimesh = MultiMeshInstance.Multimesh;
        //stride = Multimesh.TransformFormat == MultiMesh.TransformFormatEnum.Transform2D ? 6 : 16;

        stride = 8; //Multimesh.TransformFormat == MultiMesh.TransformFormatEnum.Transform2D ? 8 : 12;
        stride += useColors ? 4 : 0;
        stride += useCustomData ? 4 : 0;
        buffer = new float[startCount * stride];
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
        Array.Resize(ref buffer, Multimesh.InstanceCount * stride);
    }

    public void RemoveInstances(int count)
    {
        //Multimesh.VisibleInstanceCount = Math.Max(0, Multimesh.VisibleInstanceCount - count);

        int newVisibleCount = Math.Max(0, VisibleCount - count);
        count = VisibleCount - newVisibleCount;
        VisibleCount = newVisibleCount;

        Multimesh.VisibleInstanceCount = VisibleCount;
        //Array.Resize(ref buffer, Multimesh.InstanceCount * stride);
    }

    public void RemoveInstance()
    {
        RemoveInstances(1);
    }

    public void UpdateInstance(int i, Vector2G position, Vector2G velocity)
    {
        var t = new Transform2D(velocity.Angle(), position);
        if (!UseBuffer)
        {
            Multimesh.SetInstanceTransform2D(i, t);
        }
        else
        {
            // calculate 2d transform and store in buffer
            buffer[WriteIndex + 0] = t.X.X;
            buffer[WriteIndex + 1] = t.Y.X;
            buffer[WriteIndex + 2] = 0;
            buffer[WriteIndex + 3] = t.Origin.X;
            buffer[WriteIndex + 4] = t.X.Y;
            buffer[WriteIndex + 5] = t.Y.Y;
            buffer[WriteIndex + 6] = 0;
            buffer[WriteIndex + 7] = t.Origin.Y;
            WriteIndex += 8;
        }
    }
    public void UpdateInstance(int i, Vector2N position, Vector2N velocity)
    {
        //var t = new Transform2D(velocity.Angle(), position);
        float angle = MathF.Atan2(velocity.Y, velocity.X);
        Matrix3x2 t = Matrix3x2.CreateRotation(angle) * Matrix3x2.CreateTranslation(position);
        if (!UseBuffer)
        {
            Multimesh.SetInstanceTransform2D(i, new Transform2D(t.M11, t.M12, t.M21, t.M22, t.M31, t.M32));
        }
        else
        {
            // calculate 2d transform and store in buffer
            // 1. Calculate rotation angle (radians)
            buffer[WriteIndex + 0] = t.M11; // t[0][0];
            buffer[WriteIndex + 1] = t.M21; // t[1][0];
            buffer[WriteIndex + 2] = 0; // 0;
            buffer[WriteIndex + 3] = t.M31; // t[2][0];
            buffer[WriteIndex + 4] = t.M12; // t[0][1];
            buffer[WriteIndex + 5] = t.M22; // t[1][1];
            buffer[WriteIndex + 6] = 0; // 0;
            buffer[WriteIndex + 7] = t.M32; // t[2][1];
            WriteIndex += 8;
        }
    }

    public void UpdateInstance(int i, Vector2G position, Vector2G velocity, Color color)
    {
        UpdateInstance(i, position, velocity);
        if (!UseBuffer)
        {
            Multimesh.SetInstanceColor(i, color);
        }
        else
        {
            buffer[WriteIndex + 0] = color.R;
            buffer[WriteIndex + 1] = color.G;
            buffer[WriteIndex + 2] = color.B;
            buffer[WriteIndex + 3] = color.A;
            WriteIndex += 4;
        }
    }
    public void UpdateInstance(int i, Vector2N position, Vector2N velocity, Color color)
    {
        UpdateInstance(i, position, velocity);
        if (!UseBuffer)
        {
            Multimesh.SetInstanceColor(i, color);
        }
        else
        {
            buffer[WriteIndex + 0] = color.R;
            buffer[WriteIndex + 1] = color.G;
            buffer[WriteIndex + 2] = color.B;
            buffer[WriteIndex + 3] = color.A;
            WriteIndex += 4;
        }
    }
    public void UpdateInstance(int i, Vector2G position, Vector2G velocity, Color color, Color customData)
    {
        UpdateInstance(i, position, velocity, color);
        if (!UseBuffer)
        {
            Multimesh.SetInstanceCustomData(i, customData);
        }
        else
        {
            buffer[WriteIndex + 0] = color.R;
            buffer[WriteIndex + 1] = color.G;
            buffer[WriteIndex + 2] = color.B;
            buffer[WriteIndex + 3] = color.A;
            WriteIndex += 4;
        }
    }
    public void UpdateInstance(int i, Vector2N position, Vector2N velocity, Color color, Color customData)
    {
        UpdateInstance(i, position, velocity, color);
        if (!UseBuffer)
        {
            Multimesh.SetInstanceCustomData(i, customData);
        }
        else
        {
            buffer[WriteIndex + 0] = color.R;
            buffer[WriteIndex + 1] = color.G;
            buffer[WriteIndex + 2] = color.B;
            buffer[WriteIndex + 3] = color.A;
            WriteIndex += 4;
        }
    }

    public void UpdateInstance(Vector2N position, Vector2N velocity)
    {
        UpdateInstance(CurrentInstance, position, velocity);
        CurrentInstance++;
    }
    public void UpdateInstance(Vector2N position, Vector2N velocity, Color color)
    {
        UpdateInstance(CurrentInstance, position, velocity, color);
        CurrentInstance++;
    }
    public void UpdateInstance(Vector2N position, Vector2N velocity, Color color, Color customData)
    {
        UpdateInstance(CurrentInstance, position, velocity, color, customData);
        CurrentInstance++;
    }
    public void UpdateInstance(Vector2G position, Vector2G velocity, Color color, Color customData)
    {
        UpdateInstance(CurrentInstance, position, velocity, color, customData);
        CurrentInstance++;
    }


    public void SendToGodot()
    {
        //Multimesh.SetInstanceCount(Multimesh.InstanceCount);
        //Multimesh.SetVisibleInstanceCount(VisibleCount);
        if (UseBuffer) Multimesh.Buffer = buffer;
        CurrentInstance = 0;
        WriteIndex = 0;
    }

}

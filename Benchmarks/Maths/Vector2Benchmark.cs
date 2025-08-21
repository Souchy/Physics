using BenchmarkDotNet.Attributes;
using Godot;
using Microsoft.VSDiagnostics;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Vector2G = Godot.Vector2;
using Vector2N = System.Numerics.Vector2;

namespace Benchmarks.Maths;

//[CPUUsageDiagnoser]
[MemoryDiagnoser]
public class Vector2Benchmark
{
    public const float delta = 1f / 60f; // 60 FPS

    //[Params(500_000)]
    [Params(1_000_000)]
    public int COUNT { get; set; }



    public Vector2N[] nPositions;
    public Vector2N[] nVelocities;
    public float[] nBuffer;
    //[GlobalSetup(Targets = [nameof(NumericsMove), nameof(NumericsTransform)])]
    //[GlobalSetup(Targets = [nameof(NumericsTransform)])]
    [GlobalSetup(Targets = [nameof(NumericsMove), nameof(NumericsSub)])]
    public void NumericsSetup()
    {
        nPositions = new Vector2N[COUNT];
        nVelocities = new Vector2N[COUNT];
        nBuffer = new float[COUNT * 6];
        for (int i = 0; i < COUNT; i++)
        {
            nPositions[i] = new Vector2N(i, i);
            nVelocities[i] = new Vector2N(i * 0.1f, i * 0.1f);
        }
    }

    [Benchmark]
    public void NumericsMove()
    {
        for (int i = 0; i < COUNT; i++)
        {
            nPositions[i] += nVelocities[i] * delta;
        }
    }
    [Benchmark]
    public void NumericsSub()
    {
        for (int i = 0; i < COUNT; i++)
        {
            //nPositions[i] -= nVelocities[i] * delta;
            var delta = nPositions[i] - nVelocities[i];
            nBuffer[i] = delta.X;
            nBuffer[i+1] = delta.Y;
        }
    }
    //[Benchmark]
    //public void NumericsTransform()
    //{
    //    for (int i = 0; i < COUNT; i++)
    //    {
    //        var v = nVelocities[i];
    //        var p = nPositions[i];

    //        // do better than this:
    //        float angle = MathF.Atan2(v.Y, v.X);
    //        var (sin, cos) = MathF.SinCos(angle);

    //        //nMatrices[i] = new Matrix3x2(
    //        //    cos, sin,
    //        //    -sin, cos,
    //        //    nPositions[i].X, nPositions[i].Y);
    //        int idx = i * 6;
    //        nBuffer[idx + 0] = cos;
    //        nBuffer[idx + 1] = sin;
    //        nBuffer[idx + 2] = -sin;
    //        nBuffer[idx + 3] = cos;
    //        nBuffer[idx + 4] = p.X;
    //        nBuffer[idx + 5] = p.Y;
    //    }
    //}

    public Vector2G[] gPositions;
    public Vector2G[] gVelocities;
    public float[] gBuffer;
    //[GlobalSetup(Targets = [nameof(GodotMove), nameof(GodotTransform)])]
    //[GlobalSetup(Targets = [nameof(GodotTransform)])]
    [GlobalSetup(Targets = [nameof(GodotMove), nameof(GodotSub)])]
    public void GodotSetup()
    {
        gPositions = new Vector2G[COUNT];
        gVelocities = new Vector2G[COUNT];
        gBuffer = new float[COUNT * 6];
        for (int i = 0; i < COUNT; i++)
        {
            gPositions[i] = new Vector2G(i, i);
            gVelocities[i] = new Vector2G(i * 0.1f, i * 0.1f);
        }
    }
    [Benchmark]
    public void GodotMove()
    {
        for (int i = 0; i < COUNT; i++)
        {
            gPositions[i] += gVelocities[i] * delta;
        }
    }
    //[Benchmark]
    //public void GodotTransform()
    //{
    //    for (int i = 0; i < COUNT; i++)
    //    {
    //        //gTransforms[i] = new Transform2D(gVelocities[i].Angle(), gPositions[i]);
    //        var t = new Transform2D(gVelocities[i].Angle(), gPositions[i]);
    //        int idx = i * 6;
    //        gBuffer[idx + 0] = t[0][0];
    //        gBuffer[idx + 1] = t[0][1];
    //        gBuffer[idx + 2] = t[1][0];
    //        gBuffer[idx + 3] = t[1][1];
    //        gBuffer[idx + 4] = t[2][0];
    //        gBuffer[idx + 5] = t[2][1];
    //    }
    //}
    [Benchmark]
    public void GodotSub()
    {
        for (int i = 0; i < COUNT; i++)
        {
            //nPositions[i] -= nVelocities[i] * delta;
            var delta = gPositions[i] - gVelocities[i];
            gBuffer[i] = delta.X;
            gBuffer[i + 1] = delta.Y;
        }
    }



}

using Arch.Core;
using Godot;
using System;
using System.Xml;

namespace Physics.Mains.v5_Arch;

public record struct Id(int Value);
public record struct Position(Vector2 Value);
public record struct Velocity(Vector2 Value);
public record struct Modulate(Color Value);

public record struct Size(float Value);
public record struct Sprite(Node2D Value);

public record struct Life(int Value);
public record struct Alive(bool Value);
public record struct CollisionImmunityTime(double Value);
public record struct DetectionMask(int Value);
public record struct CollisionLayer(int Value);

public record struct OnCollision(Action<Entity, Entity, Vector2, float, bool> Value);

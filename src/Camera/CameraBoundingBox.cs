using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// 							Tutorial for For GDPs
// ─────────────────────────────────────────────────────────────────────────────
// Drop one of these nodes into the scene per room (Tutorial),
// or one for the whole level (main level)
// Set WorldCenter to the room's center and HalfExtents to its half-size in
// world units (X = horizontal, Y = vertical (maps Y to Z, treat this as the Z axis).
// ─────────────────────────────────────────────────────────────────────────────
public partial class CameraBoundingBox : Node3D
{
    [Export] public Vector2 HalfExtents = new Vector2(40f, 25f); // world units
    
    // Convenience: the world-space AABB on the XZ plane
    public (float MinX, float MaxX, float MinZ, float MaxZ) GetBounds()
    {
        Vector3 c = GlobalPosition;
        return (
            c.X - HalfExtents.X,
            c.X + HalfExtents.X,
            c.Z - HalfExtents.Y,
            c.Z + HalfExtents.Y
        );
    }
}
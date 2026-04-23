using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CameraController : Camera3D
{
    [ExportGroup("Camera Controls")]
    [Export] private PlayerSpawner playerSpawner;

    [ExportGroup("Follow")]
    [Export] public bool followPlayers = true;
    [Export(PropertyHint.Range, "0.1, 20.0")] public float followSmoothSpeed = 5f;
    [Export(PropertyHint.Range, "-50.0, 50.0")] public float cameraZOffset = 30f;

    [ExportGroup("Zoom")]
    [Export(PropertyHint.Range, "1.0, 50.0")] public float minSize = 32f;
    [Export(PropertyHint.Range, "1.0, 150.0")] public float maxSize = 70f;
    [Export(PropertyHint.Range, "0.1, 20.0")] public float zoomSmoothSpeed = 8f;
    [Export(PropertyHint.Range, "10.0, 40.0")] public float zoomPadding = 20f;
    [Export(PropertyHint.Range, "0.0, 50.0")] public float singlePlayerZoomRadius = 12f;
    [Export] public Marker3D cameraAnchor {get; private set;}
    private CameraBoundingBox cameraBoundingBox;

    private float _targetSize;

    public override void _Ready()
    {
        base._Ready();
        ProcessPhysicsPriority = 1;
        Size = minSize;
        _targetSize = minSize;
        cameraBoundingBox = cameraAnchor as CameraBoundingBox;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (playerSpawner == null) return;

        List<Node3D> players = GetActivePlayers();
        if (players.Count == 0) return;

        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        float aspect = viewportSize.X / viewportSize.Y;
        float requiredSize = minSize;

        // Single player block
        if (players.Count == 1)
        {
            Vector3 playerPos = players[0].GlobalPosition;

            // Use the camera's current XZ position as the screen centre
            Vector3 screenCentre = new Vector3(GlobalPosition.X, playerPos.Y, GlobalPosition.Z - cameraZOffset);
            Vector2 offsetFromCentre = new Vector2(playerPos.X - screenCentre.X, playerPos.Z - screenCentre.Z);

            // How much of the half-view the player is occupying
            float aspectSingle = viewportSize.X / viewportSize.Y;
            float halfW = Size / 2f;
            float halfH = halfW / aspectSingle;

            // Normalised distance: 0 = dead centre, 1 = at the edge
            float normX = Mathf.Abs(offsetFromCentre.X) / halfW;
            float normZ = Mathf.Abs(offsetFromCentre.Y) / halfH;
            float edgeProximity = Mathf.Max(normX, normZ);

            // Start zooming out when player reaches the inner threshold
            float t = Mathf.Clamp((edgeProximity - singlePlayerZoomRadius / 100f) / (1f - singlePlayerZoomRadius / 100f), 0f, 1f);
            requiredSize = Mathf.Lerp(minSize, maxSize, t);
        }
        else
        {
            List<Vector3> trackPoints = players.Select(p => p.GlobalPosition).ToList();

            float minX = trackPoints.Min(p => p.X);
            float maxX = trackPoints.Max(p => p.X);
            float minZ = trackPoints.Min(p => p.Z);
            float maxZ = trackPoints.Max(p => p.Z);

            float worldSpreadX = maxX - minX;
            float worldSpreadZ = maxZ - minZ;

            float angleRad = Mathf.DegToRad(Mathf.Abs(RotationDegrees.X));
            float zStretch = 1f / Mathf.Cos(angleRad);

            float sizeForX = (worldSpreadX + zoomPadding) / aspect;
            float sizeForZ = (worldSpreadZ + zoomPadding) / zStretch;

            requiredSize = Mathf.Max(sizeForX, sizeForZ);
        }
        if (cameraBoundingBox != null)
        {
            var (minX, maxX, minZ, maxZ) = cameraBoundingBox.GetBounds();
            float angleRad = Mathf.DegToRad(Mathf.Abs(RotationDegrees.X));
            float zStretch = 1f / Mathf.Cos(angleRad);
            float boxMaxSizeH = (maxZ - minZ) / zStretch;
            float boxMaxSizeW = (maxX - minX) * aspect;
            requiredSize = Mathf.Min(requiredSize, Mathf.Min(boxMaxSizeH, boxMaxSizeW));
        }
        _targetSize = Mathf.Clamp(requiredSize, minSize, maxSize);
        Size = Mathf.Lerp(Size, _targetSize, zoomSmoothSpeed * (float)delta);

        if (followPlayers)
        {
            List<Vector3> followPoints = players.Select(p => p.GlobalPosition).ToList();

            Vector3 centroid = followPoints
                .Aggregate(Vector3.Zero, (sum, p) => sum + p)
                / followPoints.Count;

            Vector3 targetPos = new Vector3(centroid.X, GlobalPosition.Y, centroid.Z);
            targetPos = ClampToBounds(targetPos, _targetSize); // <-- use _targetSize, not Size
            targetPos.Z += cameraZOffset;
            GlobalPosition = GlobalPosition.Lerp(targetPos, followSmoothSpeed * (float)delta);
        }
    }

    private Vector3 ClampToBounds(Vector3 desired, float currentSize)
    {
        if (cameraBoundingBox == null) return desired;

        var (minX, maxX, minZ, maxZ) = cameraBoundingBox.GetBounds();

        float aspect = GetViewport().GetVisibleRect().Size.X / GetViewport().GetVisibleRect().Size.Y;

        float angleRad = Mathf.DegToRad(Mathf.Abs(RotationDegrees.X));
        float zStretch = 1f / Mathf.Cos(angleRad);

        float halfWorldW = currentSize / 2f;
        float halfWorldH = (halfWorldW / aspect) * zStretch;

        float clampedX = (maxX - minX > halfWorldW * 2f)
            ? Mathf.Clamp(desired.X, minX + halfWorldW, maxX - halfWorldW)
            : (minX + maxX) / 2f;

        float clampedZ = (maxZ - minZ > halfWorldH * 2f)
            ? Mathf.Clamp(desired.Z, minZ + halfWorldH, maxZ - halfWorldH)
            : (minZ + maxZ) / 2f;

        return new Vector3(clampedX, desired.Y, clampedZ);
    }

    private List<Node3D> GetActivePlayers()
    {
        List<Node3D> players = new List<Node3D>();
        for (int i = 0; i < InputManager.MAX_PLAYERS; i++)
        {
            Node playerNode = playerSpawner.GetPlayer(i);
            if (playerNode != null && playerNode is Node3D player3D && playerNode.IsInsideTree())
            {
                //GD.Print($"Player {i}: {playerNode.Name} at {player3D.GlobalPosition}");
                players.Add(player3D);
            }
        }
        return players;
    }
}
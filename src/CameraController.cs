using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CameraController : Camera3D
{
    [ExportGroup("Camera Controls")]
    [Export] private PlayerSpawner playerSpawner;
    [Export(PropertyHint.Range, "0.0, 10.0")]
    public float smoothSpeed = 5;
    [Export(PropertyHint.Range, "0.0, 400")]
    public Vector2 outerBounds = new Vector2(200f, 100f);
    [Export(PropertyHint.Range, "0.0, 400")]
    public Vector2 innerBounds = new Vector2(150f, 75f);
    [Export(PropertyHint.Range, "1.0, 50.0")]
    public float minSize = 15f;
    [Export(PropertyHint.Range, "1.0, 50.0")]
    public float maxSize = 30f;
    
    private Vector2 viewportSize;
    private Vector2 screenCenter;
    private float targetSize;
    private bool isZoomedOut = false;
    private bool isTransitioning = false;
    
    public override void _Ready()
    {
        base._Ready();
        ProcessPhysicsPriority = 1;
        viewportSize = GetViewport().GetVisibleRect().Size;
        screenCenter = viewportSize / 2;
        Size = minSize;
        targetSize = minSize;
        
        if (playerSpawner == null)
        {
            GD.PrintErr("CameraController: PlayerSpawner not assigned!");
        }
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        if (playerSpawner == null)
            return;
        
        // Get all active players
        List<Node3D> activePlayers = GetActivePlayers();
        
        if (activePlayers.Count == 0)
            return;
        
        // Check if ANY player is out of bounds
        bool anyPlayerOutOfBounds = false;
        
        foreach (Node3D player in activePlayers)
        {
            Vector2 screenPos = UnprojectPosition(player.GlobalPosition);
            Vector2 distFromCenter = (screenPos - screenCenter).Abs();
            
            Vector2 checkBounds = isZoomedOut ? innerBounds : outerBounds;
            bool withinBounds = distFromCenter.X <= checkBounds.X && distFromCenter.Y <= checkBounds.Y;
            
            if (!withinBounds)
            {
                anyPlayerOutOfBounds = true;
                break; // No need to check other players
            }
        }
        
        // Handle zoom transitions
        if (!isTransitioning)
        {
            if (anyPlayerOutOfBounds && !isZoomedOut)
            {
                // At least one player is out of bounds - zoom out
                targetSize = maxSize;
                isZoomedOut = true;
                isTransitioning = true;
            }
            else if (!anyPlayerOutOfBounds && isZoomedOut)
            {
                // All players are within bounds - zoom in
                targetSize = minSize;
                isZoomedOut = false;
                isTransitioning = true;
            }
        }
        
        // Smooth zoom
        Size = Mathf.Lerp(Size, targetSize, smoothSpeed * (float)delta);
        
        if (isTransitioning && Mathf.Abs(Size - targetSize) < 0.01f)
        {
            Size = targetSize;
            isTransitioning = false;
        }
    }
    
    private List<Node3D> GetActivePlayers()
    {
        List<Node3D> players = new List<Node3D>();
        
        for (int i = 0; i < InputManager.MAX_PLAYERS; i++)
        {
            Node playerNode = playerSpawner.GetPlayer(i);
            
            if (playerNode != null && playerNode is Node3D player3D && playerNode.IsInsideTree())
            {
                players.Add(player3D);
            }
        }
        
        return players;
    }
    public bool GetCameraZoomOut() => isZoomedOut;
}

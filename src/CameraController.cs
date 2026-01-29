using Godot;
using System;
using System.ComponentModel;

public partial class CameraController : Camera3D
{
    [ExportGroup("Camera Controls")]
    [Export(PropertyHint.NodeType, "CharacterBody3D")]
    private NodePath targetPath;
    [Export(PropertyHint.Range, "0.0, 10.0")]
    private float smoothSpeed = 5;
    [Export(PropertyHint.Range, "0.0, 400")]
    private Vector2 outerBounds = new Vector2(200f, 100f);
    [Export(PropertyHint.Range, "0.0, 400")]
    private Vector2 innerBounds = new Vector2(150f, 75f);
    [Export(PropertyHint.Range, "1.0, 50.0")]
    private float minSize = 15f;
    [Export(PropertyHint.Range, "1.0, 50.0")]
    private float maxSize = 30f;
    
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
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        CharacterBody3D player = GetNode<CharacterBody3D>(targetPath);
        Vector2 screenPos = UnprojectPosition(player.GlobalPosition);
        Vector2 distFromCenter = (screenPos - screenCenter).Abs();
        
        if (!isTransitioning)
        {
            Vector2 checkBounds = isZoomedOut ? innerBounds : outerBounds;
            bool withinBounds = distFromCenter.X <= checkBounds.X && distFromCenter.Y <= checkBounds.Y;
            
            if (!withinBounds && !isZoomedOut)
            {
                targetSize = maxSize;
                isZoomedOut = true;
                isTransitioning = true;
            }
            else if (withinBounds && isZoomedOut)
            {
                targetSize = minSize;
                isZoomedOut = false;
                isTransitioning = true;
            }
        }
        
        Size = Mathf.Lerp(Size, targetSize, smoothSpeed * (float)delta);
        
        if (isTransitioning && Mathf.Abs(Size - targetSize) < 0.01f)
        {
            Size = targetSize;
            isTransitioning = false;
        }
    }
}
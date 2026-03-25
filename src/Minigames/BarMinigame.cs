using Godot;
using System;

public partial class BarMinigame : Node2D
{
    [ExportGroup("Movable Box")]
	[Export] private RigidBody2D rigidBox;

    [ExportGroup("Progress Bar")]
    [Export] private TextureProgressBar textureProgressBar;
    
    [ExportGroup("Item Properties")]
    [Export] private Sprite2D itemToCatch;
    [Export] private int moveDistance = 20;
    [Export] private float moveTime = 1;
    [Export] private float itemSpeed = 100f;

    [Export] private float timeLimit = 10f;

    [Export] public CanvasLayer canvasLayer;

    [Signal] public delegate void MinigameCompletedEventHandler();
    [Signal] public delegate void MinigameFailedEventHandler();
    private float targetY = 0f;
    private float timeElapsed = 0f;
    private bool isActive = false;
    private int activeDevice;

    private bool isOnBar = false;

    private PlayerController activePlayer;

    public void Start(PlayerController player)
    {
        activePlayer = player;
        activeDevice = player.currentDevice;
        isActive = true;
        timeElapsed = 0f;
        textureProgressBar.Value = 50;
        targetY = GD.Randf() * 360f - 130f;
        canvasLayer.Show();
    }

    public override void _Ready()
    {
        GD.Randomize();
        moveTime *= .5f;
        textureProgressBar.Value = 0;
        
    }

    public override void _Process(double delta)
    {
        if(!isActive) return;

        var currentY = itemToCatch.Position.Y;
        var newY = Mathf.MoveToward(currentY, targetY, itemSpeed * (float)delta);
        itemToCatch.Position = new Godot.Vector2(itemToCatch.Position.X, newY);

        timeElapsed += (float)delta;
        if(timeElapsed >= timeLimit)
        {
            isActive = false;
            canvasLayer.Hide();
            EmitSignal(SignalName.MinigameFailed);
        }
        
        
    }

    public void  HandleInput(InputEvent @event)
    {
        if(!isActive) return;

        if(activeDevice == -1)
        {
            if(@event is not InputEventKey) return;

			if(@event.IsActionPressed("ui_left")) rigidBox.ApplyImpulse(new Godot.Vector2(0, -200));
            else if(@event.IsActionPressed("ui_right")) rigidBox.ApplyImpulse(new Godot.Vector2(0, 200));
        }
        else
        {
            if(@event is not InputEventJoypadButton joyEvent) return;
			if(joyEvent.Device != activeDevice) return;

            if(@event.IsActionPressed("sequence_left")) rigidBox.ApplyImpulse(new Godot.Vector2(0, -200));
            else if(@event.IsActionPressed("sequence_right")) rigidBox.ApplyImpulse(new Godot.Vector2(0, 200));
        }
        
    }

    private void OnBodyEntered(Node2D body)
    {
        
        if(body == rigidBox)
            isOnBar = true;
    }
    
    private void OnBodyExited(Node2D body)
    {
        if(body == rigidBox)
            isOnBar = false;
    } 

    private void OnTimerTimeout()
    {
        if(!isActive) return;

        if(isOnBar)
            textureProgressBar.Value += 5;
        else if(textureProgressBar.Value >= 0)
            textureProgressBar.Value -= 5;
        if(textureProgressBar.Value <= 0)
        {
            isActive = false;
            EmitSignal(SignalName.MinigameFailed);
        }
        else if(textureProgressBar.Value >= 100)
        {
            GD.Print("Progress reached 100%");
            isActive = false;
            EmitSignal(SignalName.MinigameCompleted);
        }
            
    }

    private void OnItemTimerTimeout()
    {
        targetY = GD.Randf() * 260f - -120f;
    }

    public bool IsActiveFor(PlayerController player)
	{
		return isActive && activePlayer == player;
	}
}

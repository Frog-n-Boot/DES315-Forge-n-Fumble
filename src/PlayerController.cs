using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerController : CharacterBody3D
{
    [Export] public int PlayerIndex { get; set; } = 0;
    
    private float speed = 5.0f;
    private int maxHealth = 100;
    private int health;
    
    private InputManager inputManager;
    private int currentDevice = -2;
    private HashSet<string> actionsPressed = new HashSet<string>();
    private HashSet<string> actionsPressedLastFrame = new HashSet<string>();
    
    [Signal]
    public delegate void HealthChangedEventHandler(int current, int max);
    [Signal]
    public delegate void DiedEventHandler();
    
    public override void _Ready()
    {
        base._Ready();
        health = maxHealth;
        
        inputManager = GetNode<InputManager>("/root/InputManager");
        
    }
    
    public override void _Input(InputEvent @event)
    {
        if (currentDevice == -2)
            return;

        // Filter events by device
        bool isFromOurDevice = false;
        
        if (@event is InputEventJoypadButton joyButton)
        {
            isFromOurDevice = (joyButton.Device == currentDevice);
        }
        else if (@event is InputEventJoypadMotion joyMotion)
        {
            isFromOurDevice = (joyMotion.Device == currentDevice);
        }
        else if (@event is InputEventKey || @event is InputEventMouse)
        {
            isFromOurDevice = (currentDevice == -1); // Keyboard/mouse device
        }

        if (!isFromOurDevice)
            return;

        // Track which actions are pressed
        foreach (var action in InputMap.GetActions())
        {
            if (@event.IsAction(action))
            {
                if (@event.IsPressed())
                    actionsPressed.Add(action);
                else if (@event.IsReleased())
                    actionsPressed.Remove(action);
            }
        }
    }
    
    public override void _Process(double delta)
    {
        base._Process(delta);
        
        // Update current device assignment
        currentDevice = inputManager.GetDeviceForPlayer(PlayerIndex);
        
        // Hide/disable player if no device assigned
        if (currentDevice == -2)
        {
            Visible = false;
            SetPhysicsProcess(false);
            return;
        }
        
        Visible = true;
        SetPhysicsProcess(true);
        
        // Update previous frame state
        actionsPressedLastFrame.Clear();
        foreach (var action in actionsPressed)
        {
            actionsPressedLastFrame.Add(action);
        }
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        Vector3 velocity = Velocity;
        
        if (!IsOnFloor())
        {
            velocity += GetGravity() * (float)delta;
        }
        
        // Get movement input
        Vector2 inputDir = GetMovementVector();
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * speed;
            velocity.Z = direction.Z * speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, speed);
        }
        
        Velocity = velocity;
        MoveAndSlide();
    }
    
    // Input helper methods
    private Vector2 GetMovementVector()
    {
        if (currentDevice == -2)
            return Vector2.Zero;

        Vector2 input = Vector2.Zero;

        if (currentDevice == -1) // Keyboard
        {
            input.X = Input.GetAxis("move_left", "move_right");
            input.Y = Input.GetAxis("move_up", "move_down");
        }
        else // Gamepad
        {
            input.X = Input.GetJoyAxis(currentDevice, JoyAxis.LeftX);
            input.Y = Input.GetJoyAxis(currentDevice, JoyAxis.LeftY);
            
            // Apply deadzone
            if (input.Length() < 0.2f)
                input = Vector2.Zero;
        }

        return input.Normalized() * (input.Length() > 0 ? 1 : 0);
    }
    
    private bool IsActionPressed(string action)
    {
        return actionsPressed.Contains(action);
    }


    private bool IsActionJustPressed(string action)
    {
        return actionsPressed.Contains(action) && !actionsPressedLastFrame.Contains(action);
    }

    private bool IsActionJustReleased(string action)
    {
        return !actionsPressed.Contains(action) && actionsPressedLastFrame.Contains(action);
    }
    
    // Game methods
    public void TakeDamage(int amount)
    {
        health -= amount;
        EmitSignal(SignalName.HealthChanged, health, maxHealth);
    }
    
    private void Die()
    {
        EmitSignal(SignalName.Died);
        QueueFree();
    }
}

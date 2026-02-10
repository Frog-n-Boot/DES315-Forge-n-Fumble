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
    
    // Get gravity from project settings
    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    
    [Signal]
    public delegate void HealthChangedEventHandler(int current, int max);
    [Signal]
    public delegate void DiedEventHandler();
    
    public override void _Ready()
    {
        base._Ready();
        health = maxHealth;
        
        inputManager = GetNode<InputManager>("/root/InputManager");
        
        if (inputManager == null)
        {
            GD.PrintErr("PlayerController: InputManager not found!");
        }
    }
    
    // Called by PlayerSpawner when spawning
    public void SetPlayerIndex(int index)
    {
        PlayerIndex = index;
        
        // Null safety check
        if (inputManager == null)
        {
            inputManager = GetNode<InputManager>("/root/InputManager");
        }
        
        if (inputManager != null)
        {
            currentDevice = inputManager.GetDeviceForPlayer(PlayerIndex);
            GD.Print($"Player initialized with index {PlayerIndex}, device {currentDevice}");
        }
        else
        {
            GD.PrintErr($"Player {PlayerIndex}: Could not find InputManager!");
        }
        
        // Set player color/appearance based on index
        UpdatePlayerAppearance();
    }
    
    public override void _Input(InputEvent @event)
    {
        if (currentDevice == -2 || inputManager == null)
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
        
        if (inputManager == null)
            return;
        
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
        
        // Add gravity (2.5D - only affects Y axis)
        if (!IsOnFloor())
        {
            velocity.Y -= gravity * (float)delta;
        }
        
        // Get movement input (2D movement on XZ plane)
        Vector2 inputDir = GetMovementVector();
        
        // Convert 2D input to 3D movement (X and Z only)
        if (inputDir != Vector2.Zero)
        {
            velocity.X = inputDir.X * speed;
            velocity.Z = inputDir.Y * speed;  // Note: inputDir.Y controls Z axis movement
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

        if (currentDevice == -1) // Keyboard - ONLY read actual keyboard keys
        {
            // Use raw key inputs instead of Input.GetAxis to avoid reading controller
            float horizontal = 0;
            float vertical = 0;
            
            if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
                horizontal -= 1;
            if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
                horizontal += 1;
            if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
                vertical -= 1;
            if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
                vertical += 1;
            
            input.X = horizontal;
            input.Y = vertical;
        }
        else // Gamepad - read from specific device
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
    
   
    // Player appearance
    private void UpdatePlayerAppearance()
    {
        Color[] playerColors =
        {
            Colors.Blue,
            Colors.Red,
            Colors.Green,
            Colors.Yellow
        };

        Color playerColor = playerColors[PlayerIndex % playerColors.Length];

        var meshInstance = GetNodeOrNull<MeshInstance3D>("CollisionShape3D/MeshInstance3D");

        if (meshInstance == null)
        {
            GD.PrintErr($"Player {PlayerIndex}: MeshInstance3D not found");
            return;
        }

        // Create a new material instance so players don't share materials
        var material = new StandardMaterial3D
        {
            AlbedoColor = playerColor
        };

        meshInstance.MaterialOverride = material;

        GD.Print($"Set player {PlayerIndex} color to {playerColor}");
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
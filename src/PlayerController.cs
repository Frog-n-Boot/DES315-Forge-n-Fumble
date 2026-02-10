using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerController : CharacterBody3D
{
    [ExportCategory("Player Attributes")]
    [Export] public int PlayerIndex { get; set; } = 0;
    
    [Export] private float speed = 5.0f;
    [Export] private int maxHealth = 100;
    [Export] private int health;
    
    private InputManager inputManager;
    private int currentDevice = -2;
    private HashSet<string> actionsPressed = new HashSet<string>();
    private HashSet<string> actionsPressedLastFrame = new HashSet<string>();
    
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
    
    public void SetPlayerIndex(int index)
    {
        PlayerIndex = index;
        
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
        
        UpdatePlayerAppearance();
    }
    
    public override void _Input(InputEvent @event)
    {
        if (currentDevice == -2 || inputManager == null)
            return;

        bool isFromOurDevice = false;
        
        if (@event is InputEventJoypadButton joyButton)
        {
            isFromOurDevice = joyButton.Device == currentDevice;
        }
        else if (@event is InputEventJoypadMotion joyMotion)
        {
            isFromOurDevice = joyMotion.Device == currentDevice;
        }
        else if (@event is InputEventKey || @event is InputEventMouse)
        {
            isFromOurDevice = currentDevice == -1;
        }

        if (!isFromOurDevice)
            return;

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
        
        currentDevice = inputManager.GetDeviceForPlayer(PlayerIndex);
        
        if (currentDevice == -2)
        {
            Visible = false;
            SetPhysicsProcess(false);
            return;
        }
        
        Visible = true;
        SetPhysicsProcess(true);
        
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
            velocity.Y -= gravity * (float)delta;
        }
        
        Vector2 inputDir = GetMovementVector();
        
        if (inputDir != Vector2.Zero)
        {
            velocity.X = inputDir.X * speed;
            velocity.Z = inputDir.Y * speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, speed);
        }
        
        Velocity = velocity;
        MoveAndSlide();
    }
    
    private Vector2 GetMovementVector()
    {
        if (currentDevice == -2)
            return Vector2.Zero;

        if (currentDevice == -1)
        {
            Vector2 input = Vector2.Zero;

            if (IsActionPressed("move_left"))
                input.X -= 1;
            if (IsActionPressed("move_right"))
                input.X += 1;
            if (IsActionPressed("move_up"))
                input.Y -= 1;
            if (IsActionPressed("move_down"))
                input.Y += 1;

            return input.Normalized();
        }
        else
        {
            Vector2 input = new Vector2(
                Input.GetJoyAxis(currentDevice, JoyAxis.LeftX),
                Input.GetJoyAxis(currentDevice, JoyAxis.LeftY)
            );

            if (input.Length() < 0.2f)
                return Vector2.Zero;

            return input;
        }
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

        var material = new StandardMaterial3D
        {
            AlbedoColor = playerColor
        };

        meshInstance.MaterialOverride = material;

        GD.Print($"Set player {PlayerIndex} color to {playerColor}");
    }
    
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

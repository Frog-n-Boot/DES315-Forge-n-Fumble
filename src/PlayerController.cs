using Godot;
using System;
using System.Collections.Generic;

namespace Godot;

public partial class PlayerController : CharacterBody3D
{
    [ExportCategory("Player Attributes")]
    [Export] public int PlayerIndex { get; set; } = 0;
    [Export] public float speed = 5.0f;
    [Export] public float sprintMultiplier = 1.8f;
    [Export] public float jumpVelocity = 4.5f;
    [Export] public int maxHealth = 100;
    [Export] public int health;
    [Export] public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    [Export] public int damage = 10;
    [Export] public PackedScene swordObject { get; private set; }
    [Export] public AnimationPlayer animPlayer;

    [Export] private Node3D hand;
    private StaticBody3D world;
    private Camera3D camera;
    private InputManager inputManager;
    private int currentDevice = -1;
    private HashSet<string> actionsPressed = new();
    private HashSet<string> actionsPressedLastFrame = new();

    [Signal] public delegate void PlayerHealthChangedEventHandler(int current, int max);
    [Signal] public delegate void DiedEventHandler();

    InventorySystem inventory;
    Forge forge;
    HealthPack healthPack;

    private int _healthPack;
    private Vector3 currentLookTarget;
    private Sword swordClass;

    public override void _Ready()
    {
        inputManager = GetNode<InputManager>("/root/InputManager");
        health = maxHealth;

        world = GetTree().Root.GetNodeOrNull<StaticBody3D>("testing_lab");
        if (world != null)
            camera = world.GetNodeOrNull<Camera3D>("Camera3D");

        if (camera == null)
            camera = GetViewport().GetCamera3D();

        forge = GetNodeOrNull<Forge>("/root/Forge");
        healthPack = GetNodeOrNull<HealthPack>("/root/HealthPack");
        //hand = GetNodeOrNull<Node3D>("Hand");
        inventory = GetNodeOrNull<InventorySystem>("/root/InventorySystem");

        var pickupArea = GetNode<Area3D>("Area3D");
        pickupArea.BodyEntered += OnPickupBodyEntered;
        pickupArea.AreaEntered += OnPickupAreaEntered;
        animPlayer.Play("Anim_Attack");
    }

    public override void _Process(double delta)
    {
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
            actionsPressedLastFrame.Add(action);

        PlayerInput(delta);

        if (health <= 0)
            Die();

        Craft();

        if (Input.IsActionJustPressed("attack"))
        {
            animPlayer.Play("Anim_Attack");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        Vector2 inputDir = GetMovementVector();
        Vector3 lookDir = GetLookVector();

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

        if (lookDir != Vector3.Zero)
        {
            float targetAngle = Mathf.Atan2(lookDir.X, lookDir.Z);
            Rotation = new Vector3(Rotation.X, targetAngle, Rotation.Z);
        }

        MoveAndSlide();
    }

    public override void _Input(InputEvent @event)
    {
        if (currentDevice == -2 || inputManager == null)
            return;

        bool isFromOurDevice = false;

        if (@event is InputEventJoypadButton joyButton)
            isFromOurDevice = joyButton.Device == currentDevice;
        else if (@event is InputEventJoypadMotion joyMotion)
            isFromOurDevice = joyMotion.Device == currentDevice;
        else if (@event is InputEventKey || @event is InputEventMouse)
            isFromOurDevice = currentDevice == -1;

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

    private void PlayerInput(double delta)
    {
   
    }

    private void OnPickupBodyEntered(Node body)
    {
        if (body.IsInGroup("pickable"))
            ProcessPickable(body);
    }

    private void OnPickupAreaEntered(Area3D area)
    {
        if (area.IsInGroup("pickable"))
            ProcessPickable(area);
    }

    private void ProcessPickable(Node hitObject)
    {
        if (hitObject == null || inventory == null)
            return;

        if (hitObject.IsInGroup("pickable"))
        {
            Pickable pickable = hitObject as Pickable;
            if (pickable == null)
                return;

            ItemData itemData = pickable.GetItemData();

            if (inventory.AddItem(itemData))
            {
                pickable.PickUp();
                if (itemData.name == "HealthPack")
                    _healthPack++;
            }
        }

        if (hitObject.IsInGroup("Forge") && forge != null)
        {
            var items = inventory.GetItems();
            foreach (var item in items)
            {
                if (item.name == "HealthPack")
                {
                    forge.HealForge(5);
                    inventory.RemoveItem(item);
                    _healthPack--;
                    break;
                }
            }
        }
    }

    private Vector3 GetLookVector()
    {
        if (currentDevice == -2)
            return currentLookTarget;

        var position = Position;

        if (currentDevice == -1)
        {
            var mousePos = GetViewport().GetMousePosition();
            var rayOrigin = camera.ProjectRayOrigin(mousePos);
            var rayDir = camera.ProjectRayNormal(mousePos);
            var plane = new Plane(Vector3.Up, position.Y);
            var intersect = plane.IntersectsRay(rayOrigin, rayDir);

            if (intersect != null)
            {
                var target = new Vector3(intersect.Value.X, position.Y, intersect.Value.Z);
                if (target.DistanceTo(position) > 0.1f)
                    currentLookTarget = target - position;
            }
        }
        else
        {
            Vector2 input = new(
                Input.GetJoyAxis(currentDevice, JoyAxis.RightX),
                Input.GetJoyAxis(currentDevice, JoyAxis.RightY)
            );

            if (input.Length() > 0.2f)
                currentLookTarget = new Vector3(input.X, 0, input.Y).Normalized();
        }

        return currentLookTarget;
    }

    private Vector2 GetMovementVector()
    {
        if (currentDevice == -2)
            return Vector2.Zero;

        if (currentDevice == -1)
        {
            Vector2 input = Vector2.Zero;
            if (IsActionPressed("move_left")) input.X -= 1;
            if (IsActionPressed("move_right")) input.X += 1;
            if (IsActionPressed("move_up")) input.Y -= 1;
            if (IsActionPressed("move_down")) input.Y += 1;
            return input.Normalized();
        }
        else
        {
            Vector2 input = new(
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

    public void TakeDamage(int damage)
    {
        health -= damage;
        EmitSignal(SignalName.PlayerHealthChanged, health, maxHealth);
    }

    private void Die()
    {
        GlobalPosition = new Vector3(0, 1, 0);
    }
    
    private void Craft(){
   
        
        if (inventory == null)
        {
            GD.Print("Inventory object is null");
             return;
        }

        if (hand == null)
           {
            GD.Print("hand object is null");
             return;
        }

        if (swordObject == null)
        {
            GD.Print("Sword object is null");
             return;
        }
        

        if (hand == null || inventory == null || swordObject == null)
            return;

        if (hand.GetChildCount() != 1)
            return;

        var items = inventory.GetItems();
        if (items.Count < 2)
            return;
        
        ItemData stick = null;
        ItemData ingot = null;

        foreach (var item in items)
        { 
            if (item.name == "Stick" && item.count > 0)
                stick = item;
            
            if (item.name == "Ingot" && item.count > 0)
                ingot = item;
         
        }

        if(stick != null && ingot != null && hand.GetChildCount() == 0)
        {
           Node3D sword = swordObject.Instantiate<Node3D>();
            sword.AddToGroup("Sword");
            this.AddChild(sword);
            sword.Reparent(hand);
            sword.GlobalPosition = hand.GlobalPosition;


            inventory.RemoveItem(stick);
            inventory.RemoveItem(ingot);
        }
    }

}

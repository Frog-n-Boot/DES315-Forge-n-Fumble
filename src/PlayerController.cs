using Godot;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Godot;

[Tool]


public partial class PlayerController : CharacterBody3D
{
    #region Variables
    [Export] public float speed;

    [Export] public float sprintMultiplier = 1.8f;

    [Export] public float jumpVelocity = 4.5f;

    [Export] public int maxHealth = 100;

    [Export]  public int health;

    [Export] public float gravity = -9.8f;

    [Export] public int damage = 10;

    [Export] public AnimationPlayer animPlayer;

    private Node3D hand;

    private Node3D world;
    private Camera3D camera;
    // private AnimationPlayer animPlayer;

    [Signal] public delegate void PlayerHealthChangedEventHandler(int current, int max);

    [Export] public PackedScene swordObject { get; private set; }

    InventorySystem inventory;
    Forge forge;
    HealthPack healthPack;

    private bool _collissionHandled = false;
    private int _healthPack;
    private Godot.Vector3 currentLookTarget;

    private Node node;
    private ItemData data;
    private Node3D sword;

    private Sword swordClass;
    #endregion
    
    #region Ready function
    public override void _Ready()
    {
  
        health = maxHealth;
        world = GetTree().Root.GetNode<Node3D>("Level1");
        camera = world.GetNode<Camera3D>("Camera3D");
        forge = GetNode<Forge>("/root/Forge");
        healthPack = GetNode<HealthPack>("/root/HealthPack");

        //inventory = GetNodeOrNull<InventorySystem>("/root/Level1/Player/CharacterBody3D/InventorySystem");
        swordClass = GetNodeOrNull<Sword>("/root/Level1/Player/CharacterBody3D/Hand/Sword");
       	swordObject = GD.Load<PackedScene>("res://Objects/Sword.tscn");
        hand = GetNodeOrNull<Node3D>("/root/Level1/Player/CharacterBody3D/Hand");
        inventory = GetNode<InventorySystem>("/root/InventorySystem");
       // animPlayer = GetNode<AnimationPlayer>("res://Objects/Swprd/StaticBody3D/AnimationPlayer");

    }
    #endregion


    #region Physics Update
    public override void _PhysicsProcess(double delta)
    {
        if (!Engine.IsEditorHint())
        {
            if (!IsInsideTree())
            {
                return;
            }
            PlayerInput(delta);
        }
    }
    #endregion

    #region Update
    public override void _Process(double delta)
    {
 
        if (!Engine.IsEditorHint())
        {
 
            LookAtMouse();
            PlayerInput(delta);
            Craft();
        }
        if(health <= 0)
        {
            Die();
        }
        
    }
   #endregion

    #region PlayerInput

    private void PlayerInput(double delta)
    {  
        Vector2 inputDirection = Input.GetVector("act_left", "act_right", "act_forward", "act_backward");
        Vector3 direction =  new Vector3(inputDirection.X, 0, inputDirection.Y).Normalized();
        if (!IsOnFloor())
        {
            Velocity += GetGravity() * (float)delta;
        }
        if(direction != Vector3.Zero)
        {
            Velocity = new Vector3(direction.X * speed, Velocity.Y, direction.Z * speed);

        }
        else
        {
            Velocity = new Vector3(0, Velocity.Y, 0);
        }


        if(Input.IsActionJustPressed("act_jump") ){
            Velocity = new Vector3(0, jumpVelocity, 0);
        }

        // --- Sprint --- //

        if (Input.IsActionPressed("act_sprint"))
        {
            Velocity = new Vector3(direction.X * speed * sprintMultiplier, Velocity.Y, direction.Z * speed * sprintMultiplier);
        }
        else
        {
            Velocity = new Vector3(direction.X * speed, Velocity.Y, direction.Z * speed);
        }

        // --- Attack --- //
        if (Input.IsActionJustPressed("act_attack"))
        {
            if (animPlayer != null && IsInstanceValid(animPlayer))
            {
                animPlayer.Play("Anim_Attack");
                //animPlayer  = GetNode<AnimationPlayer>("AnimationPlayer");
    
            }
            else
            {
                animPlayer = null;
                return;
            }
        }

        if (Input.IsActionJustPressed("act_pickup"))
        {
            CheckPickable();
        }

        MoveAndSlide();

    }
    
    #endregion

    #region Player Rotation
    //Rotates the player in the direction of the mouse
    //returns a target
    private Vector3 LookAtMouse()
    {
        var position = Position;
        var mousePos = GetViewport().GetMousePosition();
        var rayOrigin = camera.ProjectRayOrigin(mousePos);
        var rayDir = camera.ProjectRayNormal(mousePos);
        var plane = new Plane(Vector3.Up, Position.Y);

        var intersect = plane.IntersectsRay(rayOrigin, rayDir);
        if (intersect != null)
        {
            var target = new Vector3(intersect.Value.X, position.Y, intersect.Value.Z);

            if(target.DistanceTo(Position) > 0.1f)
            {
                
                LookAt(target, Vector3.Up);
                currentLookTarget = target;
            }
     
            return target;
        }
        return currentLookTarget;
    }
    
    #endregion
    private void CheckPickable()
    {

        var spaceState = GetWorld3D().DirectSpaceState;
        var camera = GetViewport().GetCamera3D();
        var mousePos = GetViewport().GetMousePosition();

        var start = camera.ProjectRayOrigin(mousePos);
        var end = camera.ProjectRayNormal(mousePos) * 1000;

        var query = PhysicsRayQueryParameters3D.Create(start, end);
        query.CollideWithAreas = true;
        query.CollideWithBodies = true;

        var result = spaceState.IntersectRay(query);

        if(result.Count > 0)
        {
            var hitObject = result["collider"].As<Node3D>();
            if (hitObject.IsInGroup("pickable"))
            {
                GD.Print("Object is pickable");
                Pickable pickable = hitObject as Pickable;
                data = pickable.GetItemData();

                if (inventory.AddItem(data))
                {
                    pickable.PickUp();
                    GD.Print($"Picked up: {data.name}");
                    //GD.Print($" {data.name}s Picked up: {data.count}");
                    OnBodyEntered(hitObject);
                    GD.Print("I picked up the: ", data.name);
                    if(data.name == "HealthPack")
                    {
                        _healthPack++;
                    }
                }                                  
            }
            
            if(hitObject.IsInGroup("Forge")){
                var items = inventory.GetItems();
                foreach(var item in items){
                    if(items[0].name == "HealthPack" || items[1].name == "HealthPack")
                    {
                        forge.HealForge(5);
                        inventory.RemoveItem(data);         
                        _healthPack--;
                        break;
                    }
                }
           
            }
        }
    }

    public void TakeDamage(int damage)
    {
        this.health -= damage;
        EmitSignal(SignalName.PlayerHealthChanged, health, maxHealth);
    }

    private void Die()
    {
        GlobalPosition = new Vector3(0, 1, 0);
    }

    private void OnBodyEntered(Node3D body)
    {
        if (_collissionHandled) return;
        _collissionHandled = true;
        var hit = LookAtMouse();

        if (body.IsInGroup("Forge") && _healthPack >= 1)
        {
            forge.HealForge(10);
            _healthPack--;
        }
            GetTree().CreateTimer(0.05f).Timeout += () => _collissionHandled = false;
    }

    private void Craft()
    {
        var items = inventory.GetItems();
        if(items.Count < 2)
        {
            return;
        }

        ItemData stick = null;
        ItemData ingot = null;

        foreach(var item in items)
        {
            if(item.name == "Stick" && item.count > 0 )
            {
                stick = item;
            }
            if(item.name =="Ingot" && item.count > 0)
            {
                ingot = item;
            }
        }
        
        if(stick != null && ingot != null && hand.GetChildCount() == 0)
        {
            Node3D sword = swordObject.Instantiate<Node3D>();
            sword.AddToGroup("Sword");
            this.AddChild(sword);
            sword.Reparent(hand);

            animPlayer = sword.GetNode<AnimationPlayer>("StaticBody3D/AnimationPlayer");
            sword.GlobalPosition = hand.GlobalPosition;

            inventory.RemoveItem(stick);
            inventory.RemoveItem(ingot);
        }
   
    }
}

using Godot;
using System;

public partial class Enemy : Node3D
{
    #region Signals
    [Signal] public delegate void DiedEventHandler(Enemy enemy, Vector3 deathPosition);
    [Signal] public delegate void EnemyHealthChangedEventHandler(int currentHealth, int maxHealth);
    [Signal] public delegate void DamagedTargetEventHandler(Node3D target, int damage);
    #endregion

    #region Export
    [Export] public EnemyData enemyData;
    [Export] public Node3D moveTarget;
    [Export] public MeshInstance3D mesh;
    [Export] public Area3D collisionArea;
    #endregion

    #region Components
    private HealthComponent healthComponent;
    private MovementComponent movementComponent;
    private CollissionHandlerComponent collisionComponent;
    #endregion

    #region Properties
    public int health => enemyData?.health ?? 100;
    public int Damage => enemyData?.damage ?? 10;
    public string enemyName => enemyData?.enemyName ?? "Unknown";
    private Forge forgeScript;
    private bool isDying = false;
    #endregion

    #region LifeCycle
    public override void _Ready()
    {
        SetupComponents();
        SetEnemyData();
        ConnectSignals();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (movementComponent != null && moveTarget != null)
        {
            movementComponent.MoveTowards(this, moveTarget.GlobalPosition, delta);
        }
    }
    #endregion

    #region Setup
    private void SetupComponents()
    {
        healthComponent = GetNodeOrNull<HealthComponent>("HealthComponent");
        if (healthComponent == null)
        {
            healthComponent = new HealthComponent();
            AddChild(healthComponent);
            healthComponent.Name = "HealthComponent";
        }

        movementComponent = GetNodeOrNull<MovementComponent>("MovementComponent");
        if (movementComponent == null)
        {
            movementComponent = new MovementComponent();
            AddChild(movementComponent);
            movementComponent.Name = "MovementComponent";
        }

        collisionComponent = GetNodeOrNull<CollissionHandlerComponent>("CollisionHandlerComponent");
        if (collisionComponent == null)
        {
            collisionComponent = new CollissionHandlerComponent();
            AddChild(collisionComponent);
            collisionComponent.Name = "CollisionHandlerComponent";
        }

        if (collisionArea == null)
        {
            collisionArea = GetNodeOrNull<Area3D>("CharacterBody3D/Area3D");
        }

        if (collisionArea != null && collisionComponent != null)
        {
            collisionComponent.InitializeArea(collisionArea);
        }

        // No Forge lookup here — done lazily at collision time instead
    }

    private void SetEnemyData()
    {
        if (enemyData == null)
        {
            GD.PrintErr("Enemy: enemyData is null! Was Initialize() called before _Ready()?");
            return;
        }

        if (healthComponent != null)
        {
            healthComponent.maxHealth = enemyData.health;
            healthComponent.CurrentHealth = enemyData.health;
        }

        if (movementComponent != null)
        {
            movementComponent.speed = enemyData.speed;
        }

        if (mesh != null)
        {
            var material = new StandardMaterial3D();
            material.AlbedoColor = enemyData.color;
            mesh.MaterialOverride = material;
        }

        if (enemyData.scale != Vector3.Zero)
        {
            Scale = enemyData.scale;
        }
    }

    private void ConnectSignals(){
        if (healthComponent != null)
        {
            healthComponent.Died += OnHealthDepleted;
            healthComponent.HealthChanged += (current, max) => EmitSignal(SignalName.EnemyHealthChanged, current, max);
        }

        if (collisionComponent != null)
        {
            collisionComponent.CollisionDetected += OnCollisionDetected;
        }
    }
    #endregion

    #region Initialize
    public void Initialize(EnemyData data, Node3D target)
    {
        enemyData = data;
        moveTarget = target;

        if (IsNodeReady())
        {
            SetEnemyData();
        }
    }

    public static Enemy Create(EnemyData data, Node3D target, Node3D parent)
    {
        var enemyScene = GD.Load<PackedScene>("res://assets/models/Enemy.tscn");
        var enemy = enemyScene.Instantiate<Enemy>();

        enemy.Initialize(data, target); // Set data FIRST before adding to tree
        parent.AddChild(enemy);         // AddChild triggers _Ready(), so data is set by then

        return enemy;
    }
    #endregion

    #region Forge
    // Lazy lookup — only searches at collision time so Forge is guaranteed to be in the tree
    private Forge GetForge()
    {
        if (forgeScript == null)
            forgeScript = GetNode<Forge>("/root/Forge"); 
            //forgeScript = GetTree().GetFirstNodeInGroup("Forge") as Forge; //It's returning null
        return forgeScript;
    }
    #endregion

    private void OnHealthDepleted()
    {
        EmitSignal(SignalName.Died, this, GlobalPosition);
        QueueFree();
    }

    private void OnCollisionDetected(Node3D body, string groupName)
    {
        if (isDying) return; // Ignore further collisions once dying

        if (groupName == "Forge")
        {
           
            isDying = true;
            EmitSignal(SignalName.DamagedTarget, body, Damage);

            Forge forge = GetForge();
            if (forge != null && enemyData != null)
            {
                forge.TakeDamage(enemyData.damage);
                GD.Print(forge.health);
                //GD.Print("Forge took damage");
              
            }

            Die();
        }
        else if(groupName == "Player"){
            isDying = true;
            EmitSignal(SignalName.DamagedTarget, body, Damage);

            if(body is PlayerController playerController&& enemyData !=null){
                //playerController.TakeDamage(enemyData.damage);
            }

            Die();  
        }
        else if (groupName == "Sword")
        {
            Sword sword = FindSwordInHierarchy(body);
            if (sword != null)
            {
                //GD.Print("Enemy took damage");
                sword.DamageWeapon(1);
                TakeDamage(10);
            }
        }
    }

    private Sword FindSwordInHierarchy(Node3D body)
    {
        Node current = body;
        while (current != null)
        {
            if (current is Sword sword)
                return sword;
            current = current.GetParent();
        }
        return null;
    }

    public void TakeDamage(int amount)
    {
        healthComponent?.TakeDamage(amount);
        EmitSignal(SignalName.EnemyHealthChanged, health, 100);
    }

    public void Die()
    {
        EmitSignal(SignalName.Died, this, GlobalPosition);
        QueueFree();
    }

    public void SetTarget(Node3D newTarget)
    {
        moveTarget = newTarget;
    }
}
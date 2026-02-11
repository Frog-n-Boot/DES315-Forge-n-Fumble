using Godot;
using System;

public partial class Enemy: Node3D
{
    #region Signals
	[Signal] public delegate void DiedEventHandler(Enemy enemy, Vector3 deathPosition);
	[Signal] public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);
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
	public int Damage => enemyData?.damage ?? 10;
	public string enemyName => enemyData?.enemyName ?? "Unknown";
	Forge forgeScript;

	#endregion

	#region LifeCycle
	public override void _Ready()
    {
        SetupComponents();
		SetEnemyData();
		ConnectSignals();
    }

    public override void _Process(double delta)
    {
        if(movementComponent != null && moveTarget != null)
        {
            movementComponent.MoveTowards(this, moveTarget.GlobalPosition, delta);
        }
    }
	#endregion

	#region Setup
	private void SetupComponents()
    {
        healthComponent = GetNodeOrNull<HealthComponent>("HealthComponent");
		if(healthComponent == null)
        {
            healthComponent = new HealthComponent();
			AddChild(healthComponent);
			healthComponent.Name = "HealthComponent";
        }

		movementComponent = GetNodeOrNull<MovementComponent>("MovementComponent");
		if(movementComponent == null)
        {
            movementComponent = new MovementComponent();
			AddChild(movementComponent);
			movementComponent.Name = "MovementComponent";
        }

		collisionComponent = GetNodeOrNull<CollissionHandlerComponent>("CollisionHandlerComponent");
		if(collisionComponent == null)
        {
            collisionComponent = new CollissionHandlerComponent();
			AddChild(collisionComponent);
			collisionComponent.Name = "CollisionHandlerComponent";
        }
		if(collisionArea == null)
        {
            collisionArea = GetNodeOrNull<Area3D>("CharacterBody3D/Area3D");
        }

		if(collisionArea != null && collisionComponent != null)
        {
            collisionComponent.InitializeArea(collisionArea);
        }
		forgeScript = GetNode<Forge>("/root/Forge");
    }

	private void SetEnemyData(){
        if(enemyData == null) return;
		if(healthComponent != null)
        {
            healthComponent.maxHealth = enemyData.health;
			healthComponent.CurrentHealth = enemyData.health;
        }

		if(movementComponent != null)
        {
            movementComponent.speed = enemyData.speed;
        }
		if(mesh !=  null)
        {
            var material = new StandardMaterial3D();
			material.AlbedoColor = enemyData.color;
			mesh.MaterialOverride = material;
        }

		if(enemyData.scale != Vector3.Zero)
        {
            Scale = enemyData.scale;
        }
	}

	private void ConnectSignals()
    {
        if(healthComponent != null)
        {
			healthComponent.Died += OnHealthDepleted;
			healthComponent.HealthChanged += (current, max) => EmitSignal(SignalName.HealthChanged, current, max);
        }
		if(collisionComponent != null)
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
        var enemyScene = GD.Load<PackedScene>("res://Objects/Enemy.tscn");
		var enemy = enemyScene.Instantiate<Enemy>();
		parent.AddChild(enemy);
		enemy.Initialize(data, target);
		return enemy;
    }
	#endregion

	private void OnHealthDepleted()
    {
        EmitSignal(SignalName.Died, this, GlobalPosition);
		QueueFree();
    }

	private void OnCollisionDetected(Node3D body, string groupName)
    {
		
        if(groupName =="Player" || groupName == "Forge")
        {
            EmitSignal(SignalName.DamagedTarget, body, Damage);
			forgeScript.TakeDamage(enemyData.damage);
			Die();
        }
		else if( groupName == "Sword")
        {
			Sword sword = FindSwordInHierarchy(body);
			if(sword != null)
            {
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
            if(current is Sword sword)
				return sword;
			current = current.GetParent();
        }
		return null;
    }

	public void TakeDamage(int amount)
    {
        healthComponent?.TakeDamage(amount);
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

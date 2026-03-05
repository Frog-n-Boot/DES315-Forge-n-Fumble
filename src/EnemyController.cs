using Godot;
using System;

public partial class EnemyController : Node3D
{
	public enum EnemyType { Normal, Fast, Strong }

	#region Signals
	[Signal] public delegate void DiedEventHandler(EnemyController enemy, Vector3 deathPosition);
	[Signal] public delegate void EnemyHealthChangedEventHandler(int currentHealth, int maxHealth);
	[Signal] public delegate void DamagedTargetEventHandler(Node3D target, int damage);
	#endregion

	#region Exports
	[ExportGroup("Identity")]
	[Export] public EnemyData enemyData;

	[ExportGroup("Scene References")]
	[Export] public Node3D moveTarget;
	[Export] public MeshInstance3D mesh;
	[Export] public Area3D collisionArea;

	[ExportGroup("Collision")]
	[Export] public float collisionCooldown = 0.01f;
	#endregion

	#region Runtime Stats (populated from EnemyData in _Ready)
	public string EnemyName => enemyData.enemyName;
	public int    MaxHealth => enemyData.maxHealth;
	public int    Damage    => enemyData.damage;

	public int    DamageToPlayer => enemyData.damageToPlayer;
	public float  Speed         => enemyData.speed;
	public float  LootDropChance => enemyData.lootDropChance;
	#endregion

	#region Health
	public int currentHealth { get; set;}
	public int CurrentHealth => currentHealth;

	private void InitHealth()
	{
		currentHealth = MaxHealth;
		EmitSignal(SignalName.EnemyHealthChanged, currentHealth, MaxHealth);
	}

	private void TakeHealthDamage(int amount)
	{
		if (amount <= 0) return;
		int oldHealth = currentHealth;
		currentHealth = Mathf.Clamp(currentHealth - amount, 0, MaxHealth);
		if (oldHealth != currentHealth)
		{
			EmitSignal(SignalName.EnemyHealthChanged, currentHealth, MaxHealth);
			if (currentHealth <= 0)
				OnHealthDepleted();
		}
	}

	private void HealHealth(int amount)
	{
		if (amount <= 0) return;
		int oldHealth = currentHealth;
		currentHealth = Mathf.Clamp(currentHealth + amount, 0, MaxHealth);
		if (oldHealth != currentHealth)
			EmitSignal(SignalName.EnemyHealthChanged, currentHealth, MaxHealth);
	}

	public bool IsAlive() => currentHealth > 0;
	public float GetHealthPercent() => MaxHealth > 0 ? (float)currentHealth / MaxHealth : 0f;
	#endregion

	#region Movement
	private float rotationSpeed = 10.0f;
	private Vector3 velocity    = Vector3.Zero;

	private void MoveTowards(Vector3 targetPosition, double delta)
	{
		Vector3 direction = (targetPosition - GlobalPosition).Normalized();
		velocity = direction * Speed;
		GlobalPosition += velocity * (float)delta;
		if (direction.LengthSquared() > 0.01f)
			RotateTowards(direction, delta);
	}

	private void RotateTowards(Vector3 direction, double delta)
	{
		Vector3 targetRotation = new Vector3(0, Mathf.Atan2(direction.X, direction.Z), 0);
		Rotation = Rotation.Lerp(targetRotation, rotationSpeed * (float)delta);
	}

	public Vector3 GetVelocity() => velocity;
	#endregion

	#region Collision
	private bool canCollide = true;

	private void InitializeCollisionArea(Area3D area)
	{
		area.BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (!canCollide) return;
		canCollide = false;

		string group = GetBodyGroup(body);
		if (!string.IsNullOrEmpty(group))
			OnCollisionDetected(body, group);

		GetTree().CreateTimer(collisionCooldown).Timeout += () => canCollide = true;
	}

	private string GetBodyGroup(Node3D body)
	{
		if (body.IsInGroup("Player")) return "Player";
		if (body.IsInGroup("Forge"))  return "Forge";
		if (body.IsInGroup("Sword"))  return "Sword";
		if (body.IsInGroup("Enemy"))  return "Enemy";
		if (body.IsInGroup("Bullet")) return "Bullet";
		return "";
	}
	#endregion

	#region Private State
	private Forge forgeScript;
	private bool isDying = false;
	#endregion

	#region Lifecycle
	public override void _Ready()
	{
		if (enemyData == null)
		{
			GD.PrintErr($"EnemyController: enemyData is null on {Name}! Assign an EnemyData resource.");
			return;
		}

		AutoFindNodes();
		ApplyVisuals();
		SetupCollision();
		InitHealth();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (moveTarget != null)
			MoveTowards(moveTarget.GlobalPosition, delta);
	}
	#endregion

	#region Setup
	private void ApplyVisuals()
	{
		if (enemyData == null) return;

		if (mesh != null)
		{
			if (enemyData.enemyMesh != null) mesh.Mesh             = enemyData.enemyMesh;
			if (enemyData.enemyMat  != null) mesh.MaterialOverride = enemyData.enemyMat;
		}

		if (enemyData.scale != Vector3.Zero)
			Scale = enemyData.scale;
	}

	private void SetupCollision()
	{
		if (collisionArea == null)
			collisionArea = GetNodeOrNull<Area3D>("Area3D");

		if (collisionArea != null)
			InitializeCollisionArea(collisionArea);
		else
			GD.PrintErr($"EnemyController ({Name}): Could not find Area3D.");
	}

	private void AutoFindNodes()
	{
		if (mesh == null)
			mesh = GetNodeOrNull<MeshInstance3D>("CharacterBody3D/MeshInstance3D");

		if (mesh == null)
			GD.PrintErr($"EnemyController ({Name}): Could not find MeshInstance3D at CharacterBody3D/MeshInstance3D.");
	}
	#endregion

	#region Initialize (Spawner API)
	/// <summary>
	/// Call this BEFORE AddChild so enemyData and target are set before _Ready() fires.
	/// </summary>
	public void Initialize(EnemyData data, Node3D target)
	{
		enemyData  = data;
		moveTarget = target;
	}

	/// <summary>
	/// Factory helper — initializes, adds to tree, returns the ready enemy.
	/// Set GlobalPosition on the returned instance after calling this.
	/// </summary>
	public static EnemyController Create(EnemyData data, Node3D target, Node parent)
	{
		var enemyScene = GD.Load<PackedScene>("res://assets/models/Enemy.tscn");
		var enemy      = enemyScene.Instantiate<EnemyController>();

		enemy.Initialize(data, target); // set data BEFORE _Ready()
		parent.AddChild(enemy);         // triggers _Ready()

		return enemy;
	}
	#endregion

	#region Forge
	private Forge GetForge()
	{
		if (forgeScript == null)
			forgeScript = GetNode<Forge>("/root/Forge");
		return forgeScript;
	}
	#endregion

	#region Collision Handling
	private void OnCollisionDetected(Node3D body, string groupName)
	{
		if (isDying) return;

		switch (groupName)
		{
			case "Forge":
				isDying = true;
				EmitSignal(SignalName.DamagedTarget, body, Damage);
				Forge forge = GetForge();
				if (forge != null)
				{
					forge.TakeDamage(Damage);
					GD.Print(forge.health);
				}
				Die();
				break;

			case "Player":
				isDying = true;
				EmitSignal(SignalName.DamagedTarget, body, Damage);
				if (body is PlayerController playerController)
					playerController.TakeDamage(DamageToPlayer);
				Die();
				break;

			case "Sword":
				Sword sword = FindSwordInHierarchy(body);
				if (sword != null)
				{
					sword.DamageWeapon(1);
					TakeDamage(sword.damage);
				}
				break;

			case "Bullet":
				Bullet bullet = body.GetParent() as Bullet;
				if (bullet != null)
				{
					TakeDamage(bullet.damage);
					bullet.QueueFree();
				}
				else
				{
					GD.Print("Bullet is null");
				}
				break;
		}
	}
	#endregion

	#region Health Events
	private void OnHealthDepleted()
	{
		EmitSignal(SignalName.Died, this, GlobalPosition);
		QueueFree();
	}
	#endregion

	#region Public API
	public void TakeDamage(int amount)
	{
		TakeHealthDamage(amount);
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
	#endregion

	#region Helpers
	private Sword FindSwordInHierarchy(Node3D body)
	{
		Node current = body;
		while (current != null)
		{
			if (current is Sword sword) return sword;
			current = current.GetParent();
		}
		return null;
	}
	#endregion
}

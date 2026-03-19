using Godot;
using System;

public partial class EnemyController : CharacterBody3D
{
	public enum EnemyType { Normal, Fast, Strong }

	#region Signals
	[Signal] public delegate void DiedEventHandler(EnemyController enemy, Vector3 deathPosition);
	[Signal] public delegate void EnemyHealthChangedEventHandler(int currentHealth, int maxHealth);
	[Signal] public delegate void DamagedTargetEventHandler(Node3D target, int damage);
	[Signal] public delegate void StateChangedEventHandler(string newStateName);
	#endregion

	#region Exports
	// EnemyData is the single source of truth for all configuration
	[Export] public EnemyData enemyData;

	// Scene references — auto-found in _Ready if not set
	[Export] public MeshInstance3D mesh;
	[Export] public Area3D collisionArea;
	#endregion

	#region Runtime Stats — read from EnemyData
	public string EnemyName      => enemyData.enemyName;
	public int    MaxHealth       => enemyData.maxHealth;
	public int    Damage          => enemyData.damage;
	public int    DamageToPlayer  => enemyData.damageToPlayer;
	public float  Speed           => enemyData.speed;
	public float  LootDropChance  => enemyData.lootDropChance;
	public bool   IsStationary    => enemyData.isStationary;
	#endregion

	#region Target References
	public CharacterBody3D Player    { get; private set; }
	public Node3D Forge     { get; private set; }
	public Node3D moveTarget { get; set; }
	#endregion

	#region Health
	public int currentHealth { get; set; }
	public int CurrentHealth => currentHealth;

	private StandardMaterial3D material;

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

	#region State Machine
	public IEnemyState CurrentState { get; private set; }

	public void TransitionTo(IEnemyState newState)
	{
		CurrentState?.Exit(this);
		CurrentState = newState;
		CurrentState.Enter(this);
		EmitSignal(SignalName.StateChanged, newState.GetType().Name);
	}
	#endregion

	#region Movement
	private float rotationSpeed = 10.0f;
	private Vector3 velocity    = Vector3.Zero;
	private Vector3 knockback   = Vector3.Zero;

	public void MoveTowards(Vector3 targetPosition, double delta)
	{
		Vector3 direction = (targetPosition - GlobalPosition).Normalized();

		if (knockback.LengthSquared() > 0.01f)
			velocity = knockback;
		else
			velocity = direction * Speed;

		Velocity = velocity;
		MoveAndSlide();

		if (direction.LengthSquared() > 0.01f)
			RotateTowards(direction, delta);

		knockback = knockback.Lerp(Vector3.Zero, 0.15f);
	}

	private void RotateTowards(Vector3 direction, double delta)
	{
		Vector3 targetRotation = new Vector3(0, Mathf.Atan2(direction.X, direction.Z), 0);
		Rotation = Rotation.Lerp(targetRotation, rotationSpeed * (float)delta);
	}

	// GetVelocity() removed — use Velocity property directly (inherited from CharacterBody3D)

	public void ApplyKnockback(Vector3 direction, float force)
	{
		knockback = direction * force;
	}
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

		GetTree().CreateTimer(enemyData.collisionCooldown).Timeout += () => canCollide = true;
	}

	private string GetBodyGroup(Node3D body)
	{
		if (body.IsInGroup("Player")) return "Player";
		if (body.IsInGroup("Forge"))  return "Forge";
		if (body.IsInGroup("Weapon")) return "Weapon";
		if (body.IsInGroup("Enemy"))  return "Enemy";
		if (body.IsInGroup("Bullet")) return "Bullet";
		if (body.IsInGroup("Arrow"))  return "Arrow";
		return "";
	}
	#endregion

	#region Private State
	private Forge forgeScript;
	private bool isDying        = false;
	private Vector3 _spawnPosition = Vector3.Zero;
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

		Player = GetTree().GetFirstNodeInGroup("Player") as CharacterBody3D;
		Forge  = GetTree().GetFirstNodeInGroup("Forge")  as Node3D;

		if (_spawnPosition != Vector3.Zero)
			GlobalPosition = _spawnPosition;

		TransitionTo(new MoveToForgeState());
	}

	public override void _Process(double delta)
	{
		if (IsStationary) return;
		CurrentState?.Update(this, delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsStationary) return;
		CurrentState?.PhysicsUpdate(this, delta);
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

		if (enemyData.enemyMat != null)
		{
			material                          = enemyData.enemyMat.Duplicate() as StandardMaterial3D;
			material.EmissionEnabled          = true;
			material.EmissionEnergyMultiplier = 0f;
			mesh.MaterialOverride             = material;
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
			mesh = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");

		if (mesh == null)
			GD.PrintErr($"EnemyController ({Name}): Could not find MeshInstance3D.");
	}
	#endregion

	#region Initialize (Spawner API)
	private Vector3 _spawnPos = Vector3.Zero;

	public void Initialize(EnemyData data, Node3D target, Vector3 spawnPosition)
	{
		enemyData      = data;
		moveTarget     = target;
		_spawnPosition = spawnPosition;
	}

	public static EnemyController Create(EnemyData data, Node3D target, Node parent, Vector3 spawnPosition)
	{
		var enemyScene = GD.Load<PackedScene>("res://assets/models/Enemy.tscn");
		var enemy      = enemyScene.Instantiate<EnemyController>();

		enemy.Initialize(data, target, spawnPosition);
		parent.CallDeferred(Node.MethodName.AddChild, enemy);

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
				EmitSignal(SignalName.DamagedTarget, body, Damage);
				if (body is PlayerController playerController)
				{
					playerController.TakeDamage(DamageToPlayer);
					Vector3 pushDirection = (playerController.GlobalPosition - GlobalPosition).Normalized();
					pushDirection.Y = 0;
					pushDirection   = pushDirection.Normalized();
					playerController.ApplyKnockback(pushDirection, 20f);
				}
				break;

			case "Weapon":
				BaseWeapon weapon = FindWeaponInHierarchy(body);
				if (weapon != null)
				{
					if (weapon is Sword sword)
						TakeDamage(sword.GetComboDamage());
					else
						TakeDamage(weapon.damage);

					weapon.TakeDurabilityDamage(1);
					Vector3 pushDirection = (GlobalPosition - weapon.GlobalPosition).Normalized();
					pushDirection.Y = 0;
					pushDirection   = pushDirection.Normalized();
					ApplyKnockback(pushDirection, 20f);
				}
				break;

			case "Bullet":
				Bullet bullet = body.GetParent() as Bullet;
				if (bullet != null)
				{
					TakeDamage(bullet.damage);
					bullet.QueueFree();
				}
				break;

			case "Arrow":
				Arrow arrow = body.GetParent() as Arrow;
				if (arrow != null)
				{
					TakeDamage((int)arrow.damage);
					arrow.QueueFree();
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
		Flash();
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
	private BaseWeapon FindWeaponInHierarchy(Node current)
	{
		while (current != null)
		{
			if (current is BaseWeapon weapon) return weapon;
			current = current.GetParent();
		}
		return null;
	}
	#endregion

	#region Flash
	private void Flash()
	{
		if (material == null) return;

		Color white = new Color(1, 1, 1);
		Color red   = new Color(1, 0, 0);

		var tween = CreateTween();
		tween.TweenProperty(material, "emission_energy_multiplier", 2.0f, enemyData.flashDuration / 4);
		tween.Parallel().TweenProperty(material, "emission", white, enemyData.flashDuration / 4);
		tween.TweenProperty(material, "emission", red, enemyData.flashDuration / 4);
		tween.TweenProperty(material, "emission_energy_multiplier", 0.0f, enemyData.flashDuration / 2);
	}
	#endregion

	// ── Base State ────────────────────────────────────────────────────────────

	public class EnemyStateBase : IEnemyState
	{
		public virtual void Enter(EnemyController c) { }
		public virtual void Update(EnemyController c, double delta) { }
		public virtual void PhysicsUpdate(EnemyController c, double delta) { }
		public virtual void Exit(EnemyController c) { }
	}

	// ── States ────────────────────────────────────────────────────────────────

	public class IdleState : EnemyStateBase { }

	public class ChasePlayerState : EnemyStateBase
	{
		public override void Enter(EnemyController c)
			=> c.moveTarget = c.Player;

		public override void PhysicsUpdate(EnemyController c, double delta)
			=> c.MoveTowards(c.Player.GlobalPosition, delta);
	}

	public class MoveToForgeState : EnemyStateBase
	{
		public override void Enter(EnemyController c)
			=> c.moveTarget = c.Forge;

		public override void PhysicsUpdate(EnemyController c, double delta)
			=> c.MoveTowards(c.Forge.GlobalPosition, delta);
	}

	public class AttackPlayerState : EnemyStateBase
	{
		public override void Enter(EnemyController c)
			=> c.moveTarget = c.Player;

		public override void PhysicsUpdate(EnemyController c, double delta)
			=> c.MoveTowards(c.Player.GlobalPosition, delta);
	}

	public class AttackForgeState : EnemyStateBase
	{
		public override void Enter(EnemyController c)
			=> c.moveTarget = c.Forge;

		public override void PhysicsUpdate(EnemyController c, double delta)
			=> c.MoveTowards(c.Forge.GlobalPosition, delta);
	}

	public class FleeState : EnemyStateBase
	{
		public override void PhysicsUpdate(EnemyController c, double delta)
		{
			Vector3 awayFromPlayer = (c.GlobalPosition - c.Player.GlobalPosition).Normalized();
			Vector3 awayFromForge  = (c.GlobalPosition - c.Forge.GlobalPosition).Normalized();
			Vector3 fleeDirection  = (awayFromPlayer + awayFromForge).Normalized();
			c.MoveTowards(c.GlobalPosition + fleeDirection * 20f, delta);
		}
	}
}

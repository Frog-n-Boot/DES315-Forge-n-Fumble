using Godot;
using System;

public partial class DebugMenu : CanvasLayer
{

	[Export] private SpinBox playerMaxHealth;
	[Export] private SpinBox playerSpeed;
	[Export] private SpinBox enemyMaxHealth;
	[Export] private SpinBox enemySpeed;
	[Export] private SpinBox enemyDamage;
	[Export] private SpinBox enemyDamageToPlayer;
	[Export] private SpinBox weaponDurability;
	[Export] private SpinBox weaponDamage;
	[Export] private SpinBox forgeHealth;
	[Export] private SpinBox forgeSmeltTimer;
	[Export] private SpinBox grindstoneSmeltTimer;
	[Export] private CheckBox showCollisions;

	public static float playerMaxHealthOverride = -1;
	public static float playerSpeedOverride = -1;
	public static float SmeltingTimeOverride = -1;
	public static float GrindstoneTimeOverride = -1;
	public static float enemyMaxHealthOverride = -1;
	public static float enemySpeedOverride = -1;
	public static float enemyDamageOverride = -1;
	public static float enemyDamageToPlayerOverride = -1;

	public static float weaponDurabilityOverride = -1;
	public static float weaponDamageOverride = -1;
	


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Hide();
		SetupValues();
		SetupValueRange();

		ProcessMode=ProcessModeEnum.Always;

		playerMaxHealth.ValueChanged += value => SetPlayerMaxHealth((float) value);
		playerSpeed.ValueChanged += value => SetPlayerSpeed((float) value);

		enemyMaxHealth.ValueChanged += value => SetEnemyMaxHealth((float) value);
		enemySpeed.ValueChanged += value => SetEnemySpeed((float) value);
		enemyDamage.ValueChanged += value => SetEnemyDamage((float) value);
		enemyDamageToPlayer.ValueChanged += value => SetEnemyDamageToPlayer((float) value);

		weaponDamage.ValueChanged += value => SetWeaponDamage((float) value);
		weaponDurability.ValueChanged += value => SetWeaponDurability((float) value);

		forgeHealth.ValueChanged += value => SetForgeMaxHealth((float) value);
		forgeSmeltTimer.ValueChanged += value => SetForgeSmeltTimer((float) value);

		grindstoneSmeltTimer.ValueChanged += value => SetGrindstoneSmeltTimer((float) value);
		showCollisions.Toggled += value => ShowCollisionShapes((bool) value);

	}

	private void SetupValueRange()
	{
		playerMaxHealth.MinValue= 0;
		playerMaxHealth.MaxValue= 99999;

		enemyMaxHealth.MinValue= 0;
		enemyMaxHealth.MaxValue= 99999;

		enemyDamage.MinValue= 0;
		enemyDamage.MaxValue= 99999;

		enemyDamageToPlayer.MinValue= 0;
		enemyDamageToPlayer.MaxValue= 99999;

		weaponDurability.MinValue= 0;
		weaponDurability.MaxValue= 99999;

		weaponDamage.MinValue= 0;
		weaponDamage.MaxValue= 99999;

		forgeHealth.MinValue= 0;
		forgeHealth.MaxValue= 99999;

		forgeSmeltTimer.MinValue= 0;
		forgeSmeltTimer.MaxValue= 100;

		grindstoneSmeltTimer.MinValue= 0;
		grindstoneSmeltTimer.MaxValue= 100;

	}

	private void SetupValues()
	{
		foreach(Node p in GetTree().GetNodesInGroup("Player"))
		{
			var player = p as PlayerController;
			if(player == null) continue;
			playerMaxHealth.Value = player.maxHealth;
			playerSpeed.Value= player.speed;
			
			var playerSword = player.currentSword;
			var sword = playerSword as Sword;

			weaponDurability.Value = sword.durability;
			weaponDamage.Value = sword.damage;
		}
	}

    public override void _Input(InputEvent @event)
    {
		if (@event.IsActionPressed("debug"))
		{
			if(Visible){
				Hide();
				GetTree().Paused = false;
			}
			else
			{
				Show();
				SetupValues();
				GetTree().Paused = true;
			}
	
		}
    }

	private void SetPlayerMaxHealth(float value)
	{
		playerMaxHealthOverride = value;
		foreach(Node p in GetTree().GetNodesInGroup("Player"))
		{
			var player = p as PlayerController;
			if(player != null) {
				player.maxHealth = (int)value;
				player.health = player.maxHealth;
			}

		}
	}
	private void SetPlayerSpeed(float value)
	{
		playerSpeedOverride = value;
		foreach(Node p in GetTree().GetNodesInGroup("Player"))
		{
			var player = p as PlayerController;
			if(player != null) player.speed = (int)value;
		}
	}

	private void SetEnemyMaxHealth(float value)
	{
		enemyMaxHealthOverride = value;
		foreach(Node e in GetTree().GetNodesInGroup("Enemy"))
		{
			var enemy = e as EnemyController;
			if(enemy !=null){ 
				enemy.MaxHealth = (int)value;
				enemy.currentHealth = enemy.MaxHealth;
			}
		}
	}

	private void SetEnemySpeed(float value)
	{
		enemySpeedOverride = value;
		foreach(Node e in GetTree().GetNodesInGroup("Enemy"))
		{
			var enemy = e as EnemyController;
			if(enemy !=null) enemy.Speed = (int)value;
		}
	}

	private void SetEnemyDamage(float value)
	{
		enemyDamageOverride = value;
		foreach(Node e in GetTree().GetNodesInGroup("Enemy"))
		{
			var enemy = e as EnemyController;
			if(enemy !=null) enemy.Damage = (int)value;
		}
	}

	private void SetEnemyDamageToPlayer(float value)
	{
		enemyDamageToPlayerOverride = value;
		foreach(Node e in GetTree().GetNodesInGroup("Enemy"))
		{
			var enemy = e as EnemyController;
			if(enemy !=null) enemy.DamageToPlayer = (int)value;
		}
	}

	private void SetWeaponDamage(float value)
	{
		weaponDamageOverride = value;
		foreach(Node s in GetTree().GetNodesInGroup("Sword"))
		{
			var sword = s as Sword;
			if(sword != null) sword.damage =(int)value;
		}
	}
	private void SetWeaponDurability(float value)
	{
		weaponDurabilityOverride = value;
		foreach(Node s in GetTree().GetNodesInGroup("Sword"))
		{
			var sword = s as Sword;
			if(sword != null) sword.durability =(int)value;
		}
	}

	private void SetForgeMaxHealth(float value)
	{
		var forge = GetNode<Forge>("/root/Forge");

		if(forge != null) {
			forge.maxHealth = (int)value;
			forge.health = forge.maxHealth;
			forge.EmitSignal(Forge.SignalName.ForgeTookDamage, forge.health, forge.maxHealth);
		}

		
	}

	private void SetForgeSmeltTimer(float value)
	{
		SmeltingTimeOverride = value;
		foreach(Node f in GetTree().GetNodesInGroup("Station"))
		{
			if(f is SmeltingStation station)
			{
				station.SetCraftDuration(value);
			}
		}
	}

	private void SetGrindstoneSmeltTimer(float value)
	{
		GrindstoneTimeOverride = value;
		foreach(Node f in GetTree().GetNodesInGroup("Station"))
		{
			if(f is GrindstoneStation station)
			{
				station.SetCraftDuration(value);
			}
		}
	}

	private void ShowCollisionShapes(bool value)
    {
        GetTree().DebugCollisionsHint = value;
    }
}

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DebugMenu : CanvasLayer
{
	#region "Export Variables"
	[ExportGroup("Camera Attributes")]
	[Export] private Control cameraControlNode;
	[Export] private SpinBox smoothSpeed;
	[Export] private SpinBox outerBoundsX;
	[Export] private SpinBox outerBoundsY;
	[Export] private SpinBox innerBoundsX;
	[Export] private SpinBox innerBoundsY;
	[Export] private SpinBox minSize;
	[Export] private SpinBox maxSize;

	[ExportGroup("Player Attributes")]

	[Export] private SpinBox playerMaxHealth;
	[Export] private SpinBox playerSpeed;
	[Export] private SpinBox playerSpawnTimer;

	[ExportGroup("Enemy Debug")]
	
	[Export] private EnemyData normalEnemy;
	[Export] private EnemyData fastEnemy;
	[Export] private EnemyData strongEnemy;

	[Export] FoldableContainer normalContainer;
	[Export] FoldableContainer fastContainer;
	[Export] FoldableContainer strongContainer;
	[Export] private SpinBox oreDropChance;
	[Export] private SpinBox healthPackDropChance;

	[ExportGroup("Weapon Attributes")]
	[Export] private SpinBox weaponDurability;
	[Export] private SpinBox weaponDamage;
	[Export] private SpinBox turretRange;
	[Export] private SpinBox turretConeAngle;
	[Export] private SpinBox bulletDamage;
	[Export] private SpinBox bulletSpeed;

	[ExportGroup("Station Attributes")]
	[Export] private SpinBox forgeHealth;
	[Export] private SpinBox forgeSmeltTimer;
	[Export] private SpinBox grindstoneSmeltTimer;
	[Export] private Button addForgeHealth;
	[Export] private Button subtractForgeHealth;

	[ExportGroup("Debug Attributes")]
	[Export] private CheckBox showCollisions;

	[ExportGroup("Game Loop Attributes")]

	[Export] private SpinBox maxWaves;
	[Export] private SpinBox timeBetweenEnemies;
	[Export] private SpinBox startingEnemies;
	[Export] private SpinBox enemyIncreasePerWave;
	[Export] private SpinBox timeBetweenWaves;
	[Export] private SpinBox goblinEnemySpawnChance;
	[Export] private SpinBox skeletonEnemySpawnChance;
	[Export] private SpinBox HobgoblinEnemySpawnChance;

	[ExportGroup("Items")]

	[Export] private Button spawnWeapon;
	[Export] private Button spawnOre;
	[Export] private Button spawnIngot;
	[Export] private Button spawnDullSword;
	[Export] private Button spawnHealthBox;

	[ExportGroup("Scenes")]

	[Export] private PackedScene weaponScene;
	[Export] private PackedScene oreScene;
	[Export] private PackedScene ingotScene;
	[Export] private PackedScene dullSwordScene;
	[Export] private PackedScene healthBoxScene;

	#endregion

	#region "Private Variables"
	private Forge forge;
	private WaveManager waveManager;
	private SmeltingStation forgeScript;
	private GrindstoneStation grindstoneScript;
	private LootTable lootTable;
	private CameraController cameraController;
	private PlayerController playerController;
	private PlayerSpawner playerSpawner;
	private Turret turret;
	private Bullet bullet;
	private bool playerSettingsInitialized = false;
	#endregion

	#region "Ready"
	public override void _Ready()
	{
		Hide();
		GetObjectReferences();
		ConnectButtons();
		SetupValues();
		SetupValueRange();
		SetCameraSettings();
		SetupWeaponSettings();
		SetupStationsSettings();
		PopulatePanel(normalEnemy, normalContainer);
		PopulatePanel(fastEnemy, fastContainer);
		PopulatePanel(strongEnemy, strongContainer);
		SetEnemySpawnChance();
		UpdateWaveManager();
		UpdateLootTable();

		ProcessMode=ProcessModeEnum.Always;
		showCollisions.Toggled += value => ShowCollisionShapes((bool) value);
	}
	#endregion

	#region "Button Connection"
	private void ConnectButtons()
    {
       	addForgeHealth.Pressed += AddForgeHealth;
		subtractForgeHealth.Pressed += SubtractForgeHealth;
		spawnOre.Pressed += SpawnOre;
		spawnIngot.Pressed += SpawnIngot;
		spawnWeapon.Pressed += SpawnWeapon;
		spawnDullSword.Pressed += SpawnDullSword;
		spawnHealthBox.Pressed += SpawnHealthBox; 
    }
	#endregion

	#region "Object References"
	private void GetObjectReferences()
    {
		cameraController = GetTree().GetFirstNodeInGroup("Camera") as CameraController;
        forge = GetTree().Root.GetNode<Forge>("Forge");
		waveManager = GetTree().GetFirstNodeInGroup("WaveManager") as WaveManager;
		
		forgeScript = GetTree().GetFirstNodeInGroup("Forge") as SmeltingStation;
		grindstoneScript = GetTree().GetFirstNodeInGroup("Grindstone") as GrindstoneStation;
		lootTable = GetTree().GetFirstNodeInGroup("LootTable") as LootTable;
		playerSpawner = GetTree().GetFirstNodeInGroup("PlayerSpawner") as PlayerSpawner;
		turret = GetTree().GetFirstNodeInGroup("Turret") as Turret;
		bullet = GetTree().GetFirstNodeInGroup("Bullet") as Bullet;
		
		if(playerSpawner != null)
			playerSpawner.PlayerSpawned += OnPlayerSpawned;			
    }
	#endregion
	
	#region "On Player Spawned"
	private void OnPlayerSpawned(PlayerController player, int playerIndex)
	{
		playerController = player;

		if (!playerSettingsInitialized)
		{
			SetupPlayerSettings();
			playerSettingsInitialized = true;
		}		
	}
	#endregion

	#region "Setup Value Ranges"
	private void SetupValueRange()
	{
		playerMaxHealth.MinValue= 0;
		playerMaxHealth.MaxValue= 99999;

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

		goblinEnemySpawnChance.MinValue = 0;
		goblinEnemySpawnChance.MaxValue = 100;

		skeletonEnemySpawnChance.MinValue = 0;
		skeletonEnemySpawnChance.MaxValue = 100;

		HobgoblinEnemySpawnChance.MinValue = 0;
		HobgoblinEnemySpawnChance.MaxValue = 100;

		outerBoundsX.MinValue = 0;
		outerBoundsX.MaxValue = 1000;
		
		outerBoundsY.MinValue = 0;
		outerBoundsY.MaxValue = 1000;

		innerBoundsX.MinValue = 0;
		innerBoundsX.MaxValue = 1000;

		innerBoundsY.MinValue = 0;
		innerBoundsY.MaxValue = 1000;
	}
	#endregion

	#region "Populate field values with object values"
	private void SetupValues()
	{
		foreach(Node p in GetTree().GetNodesInGroup("Player"))
		{
			var player = p as PlayerController;
			if(player == null) continue;
			playerMaxHealth.Value = player.maxHealth;
			playerSpeed.Value= player.speed;
			playerSpawnTimer.Value = player.playerSpawnTimer;
			
			var playerSword = player.currentWeapon;
			var sword = playerSword as Sword;

			weaponDurability.Value = sword.durability;
			weaponDamage.Value = sword.damage;
		}

		forgeHealth.Value = forge.maxHealth;
		forgeSmeltTimer.Value = forgeScript.GetCraftDuration();
		grindstoneSmeltTimer.Value = grindstoneScript.GetCraftDuration();

		oreDropChance.Value = lootTable.oreDropChance;
		healthPackDropChance.Value = lootTable.healthPackDropChance;

		turretRange.Value = turret.range;
		turretConeAngle.Value = turret.coneAngle;

		if(bullet != null)
		{
			bulletDamage.Value = bullet.damage;
			bulletSpeed.Value = bullet.speed;
		}		
	}
	#endregion

	#region "Input"
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
	#endregion

	#region "Camera Settings"
	private void SetCameraSettings()
    {
        smoothSpeed.Value = cameraController.smoothSpeed;
		smoothSpeed.ValueChanged += v => cameraController.smoothSpeed = (int)v;

		outerBoundsX.Value = cameraController.outerBounds.X;
		outerBoundsX.ValueChanged += v => cameraController.outerBounds.X = (int)v;	

		outerBoundsY.Value = cameraController.outerBounds.Y;
		outerBoundsY.ValueChanged += v => cameraController.outerBounds.Y = (int)v;

		innerBoundsX.Value = cameraController.innerBounds.X;
		innerBoundsX.ValueChanged += v => cameraController.innerBounds.X = (int)v;

		innerBoundsY.Value = cameraController.innerBounds.Y;
		innerBoundsY.ValueChanged += v => cameraController.innerBounds.Y = (int)v;

		minSize.Value = cameraController.minSize;
		minSize.ValueChanged += v => cameraController.minSize = (int)v;

		maxSize.Value = cameraController.maxSize;
		maxSize.ValueChanged += v => cameraController.maxSize = (int)v;
		
    }
	#endregion

	#region "Player Settings"
	private void SetupPlayerSettings()
	{
		playerMaxHealth.Value = playerController.maxHealth;
		playerMaxHealth.ValueChanged += v =>
		{
			foreach(Node p in GetTree().GetNodesInGroup("Player"))
			{
				var player = p as PlayerController;
				if(player != null)
				{
					player.maxHealth = (int)v;
					player.health = player.maxHealth;
				}
			}
		};

		playerSpeed.Value = playerController.speed;
		playerSpeed.ValueChanged += v =>
		{
			foreach(Node p in GetTree().GetNodesInGroup("Player"))
			{
				var player = p as PlayerController;
				if(player != null) player.speed = (float)v;
			}

		};

		playerSpawnTimer.Value = playerController.playerSpawnTimer;
		playerSpawnTimer.ValueChanged +=v =>
		{
			foreach(Node p in GetTree().GetNodesInGroup("Player"))
			{
				var player = p as PlayerController;
				if(player != null) player.playerSpawnTimer = (int)v;
			}
		};

	}
	#endregion

	#region "Weapon Settings"
	private void SetupWeaponSettings()
	{
		weaponDamage.ValueChanged += v =>
		{
			foreach(Node s in GetTree().GetNodesInGroup("Sword"))
			{
				var sword = s as Sword;
				if(sword != null) sword.damage = (int)v;
			}
		};

		weaponDurability.ValueChanged += v =>
		{
			foreach(Node s in GetTree().GetNodesInGroup("Sword"))
			{
				var sword = s as Sword;
				if(sword != null) sword.durability = (int)v;
			}
		};

		turretRange.ValueChanged += v =>
		{
			foreach(Node t in GetTree().GetNodesInGroup("Turret"))
			{
				var turret = t as Turret;
				if(turret != null) turret.range = (int)v;
			}
		};

		turretConeAngle.ValueChanged += v =>
		{
			foreach(Node t in GetTree().GetNodesInGroup("Turret"))
			{
				var turret = t as Turret;
				if(turret != null) turret.coneAngle = (int)v;
			}
		};
		bulletDamage.ValueChanged += v =>Bullet.defaultDamage = (int)v;
		bulletSpeed.ValueChanged += v =>Bullet.defaultSpeed = (float)v;
		
	}
	#endregion

	#region "Station Settings"
	private void SetupStationsSettings()
	{
		forgeHealth.ValueChanged += v =>
		{
			if(forge != null)
			{
				forge.maxHealth = (int)v;
				forge.health = forge.maxHealth;
				forge.EmitSignal(Forge.SignalName.ForgeTookDamage, forge.health, forge.maxHealth);
			}
		};

		forgeSmeltTimer.ValueChanged += v =>
		{
			foreach(Node f in GetTree().GetNodesInGroup("Station"))
			{
				if(f is SmeltingStation station)
				{
					station.SetCraftDuration((float)v);
				}
			}
		};

		grindstoneSmeltTimer.ValueChanged += v =>
		{
			foreach(Node f in GetTree().GetNodesInGroup("Station"))
			{
				if(f is GrindstoneStation station)
				{
					station.SetCraftDuration((float)v);
				}
			}
		};
	}
	#endregion

	#region "Forge Settings"
	public void AddForgeHealth()
    {
        if (forge != null)
        {
            forge.health += 10;
			forge.EmitSignal(Forge.SignalName.ForgeTookDamage, forge.health, forge.maxHealth);
        }
    }

	public void SubtractForgeHealth()
    {
         if (forge != null)
        {
            forge.health -= 10;
			forge.EmitSignal(Forge.SignalName.ForgeTookDamage, forge.health, forge.maxHealth);
        }
    }
	#endregion

	#region "Collision Settings"
	private void ShowCollisionShapes(bool value)
    {
        GetTree().DebugCollisionsHint = value;

		FindCollisionShapes(GetTree().Root);
    }
	

	private void FindCollisionShapes(Node parent)
	{

		foreach(Node child in parent.GetChildren())
		{
			FindCollisionShapes(child);
			
			if(child is CollisionObject3D body)
			{
				var parentNode = child.GetParent();
				var index = child.GetIndex();
				parentNode.RemoveChild(child);
				parentNode.AddChild(child);
				parentNode.MoveChild(child, index);
			}
		}
	}
	#endregion

	#region "Populate enemy values"
	private void PopulatePanel(EnemyData data, FoldableContainer container)
	{

		container.Title = data.enemyName;
		Panel panel = container.GetNode<Panel>("Panel");

		panel.GetNode<Label>("EnemyNameLabel").Text = data.enemyName;

		var healthBox = panel.GetNode<SpinBox>("MaxHealthSpinBox");

		healthBox.MinValue = 0;
		healthBox.MaxValue = 99999;

		healthBox.Value = data.maxHealth;
		healthBox.ValueChanged += v => data.maxHealth = (int)v;

		var speedBox = panel.GetNode<SpinBox>("SpeedSpinBox");

		speedBox.MinValue = 0;
		speedBox.MaxValue = 100;

		speedBox.Value = data.speed;
		speedBox.ValueChanged += v => data.speed = (float)v;

		var damageBox = panel.GetNode<SpinBox>("DamageSpinBox");

		damageBox.MinValue = 0;
		damageBox.MaxValue = 99999;

		damageBox.Value = data.damage;
		damageBox.ValueChanged += v => data.damage = (int)v;

		var playerDamageBox = panel.GetNode<SpinBox>("PlayerDamageSpinBox");

		playerDamageBox.MinValue = 0;
		playerDamageBox.MaxValue = 99999;

		playerDamageBox.Value = data.damageToPlayer;
		playerDamageBox.ValueChanged += v => data.damageToPlayer = (int)v;

		var lootDropChanceBox = panel.GetNode<SpinBox>("LootChanceSpinBox");
		
		lootDropChanceBox.MinValue =0;
		lootDropChanceBox.MaxValue = 100;

		lootDropChanceBox.Value = data.lootDropChance;
		lootDropChanceBox.ValueChanged += v => data.lootDropChance = (float)v;	
	}
	#endregion

	#region "Enemy Spawn Chances"
	private void SetEnemySpawnChance()
    {
		goblinEnemySpawnChance.Value = waveManager.normalEnemyChance;
		goblinEnemySpawnChance.ValueChanged += v => waveManager.normalEnemyChance = (int)v;

		skeletonEnemySpawnChance.Value = waveManager.fastEnemyChance;
		skeletonEnemySpawnChance.ValueChanged += v => waveManager.fastEnemyChance = (int)v;

		HobgoblinEnemySpawnChance.Value = waveManager.strongEnemyChance;	
		HobgoblinEnemySpawnChance.ValueChanged += v => waveManager.strongEnemyChance = (int)v;
    }
	#endregion
	
	#region "Update Wave Manager"
	private void UpdateWaveManager()
    {
        maxWaves.Value = waveManager.maxWaves;
		maxWaves.ValueChanged += v => waveManager.maxWaves = (int)v;

		timeBetweenEnemies.Value = waveManager.timeBetweenEnemySpawns;
		timeBetweenEnemies.ValueChanged += v => waveManager.timeBetweemWaves = (int)v;

		startingEnemies.Value = waveManager.startingEnemiesPerWave;
		startingEnemies.ValueChanged += v => waveManager.startingEnemiesPerWave = (int)v;

		enemyIncreasePerWave.Value = waveManager.enemyIncreasedPerWave;
		enemyIncreasePerWave.ValueChanged += v => waveManager.enemyIncreasedPerWave = (int)v;

		timeBetweenWaves.Value = waveManager.timeBetweemWaves;
		timeBetweenWaves.ValueChanged += v => waveManager.timeBetweemWaves = (int)v;
    }
	#endregion

	#region "Update Loot Table"
	private void UpdateLootTable()
    {
		oreDropChance.Value = lootTable.oreDropChance;
		oreDropChance.ValueChanged += v => lootTable.oreDropChance = (int)v;

		healthPackDropChance.Value = lootTable.healthPackDropChance;
		healthPackDropChance.ValueChanged += v => lootTable.healthPackDropChance = (int)v;
    }
	#endregion

	#region "Spawn Objects"
	protected void SpawnOre()
    {
		var scene = oreScene.Instantiate<Node3D>();
		GetTree().Root.AddChild(scene);
		scene.GlobalPosition = playerController.GlobalPosition + playerController.Transform.Basis.Z * 2f;
    }
	protected void SpawnIngot()
    {
		var scene = ingotScene.Instantiate<Node3D>();
		GetTree().Root.AddChild(scene);
		scene.GlobalPosition = playerController.GlobalPosition + playerController.Transform.Basis.Z * 2f;
    }
	protected void SpawnWeapon()
    {
		var scene = weaponScene.Instantiate<Node3D>();
		GetTree().Root.AddChild(scene);
		scene.GlobalPosition = playerController.GlobalPosition + playerController.Transform.Basis.Z * 2f;
    }
	protected void SpawnDullSword()
    {
		var scene = dullSwordScene.Instantiate<Node3D>();
		GetTree().Root.AddChild(scene);
		scene.GlobalPosition = playerController.GlobalPosition + playerController.Transform.Basis.Z * 2f;
    }
	protected void SpawnHealthBox()
    {
		var scene = healthBoxScene.Instantiate<Node3D>();
		GetTree().Root.AddChild(scene);
		scene.GlobalPosition = playerController.GlobalPosition + playerController.Transform.Basis.Z * 2f;
    }
	#endregion
}

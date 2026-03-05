using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DebugMenu : CanvasLayer
{

	[ExportGroup("Player Attributes")]

	[Export] private Control playerControlNode;
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

	[ExportGroup("Weapon Attributes")]
	[Export] private SpinBox weaponDurability;
	[Export] private SpinBox weaponDamage;

	[ExportGroup("Station Attributes")]
	[Export] private SpinBox forgeHealth;
	[Export] private SpinBox forgeSmeltTimer;
	[Export] private SpinBox grindstoneSmeltTimer;

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

	public static float playerMaxHealthOverride = -1;
	public static float playerSpeedOverride = -1;
	public static float playerSpawnTimerOverride = -1;
	public static float SmeltingTimeOverride = -1;
	public static float GrindstoneTimeOverride = -1;

	public static float weaponDurabilityOverride = -1;
	public static float weaponDamageOverride = -1;
	public static float maxWavesOverirde = -1;
	public static float timeBetweenEnemiesOverride = -1;
	public static float startingEnemiesOverride = -1;
	public static float enemyIncreasePerWaveOverride = -1;
	public static float timeBetweenWavesOverride = -1;
	

	private Forge forge;
	private WaveManager waveManager;

	private SmeltingStation forgeScript;
	private GrindstoneStation grindstoneScript;


	
	public override void _Ready()
	{
		Hide();
		GetObjectReferences();
		SetupValues();
		SetupValueRange();
		PopulatePanel(normalEnemy, normalContainer);
		PopulatePanel(fastEnemy, fastContainer);
		PopulatePanel(strongEnemy, strongContainer);

		ProcessMode=ProcessModeEnum.Always;
		
		playerMaxHealth.ValueChanged += value => SetPlayerMaxHealth((float) value);
		playerSpeed.ValueChanged += value => SetPlayerSpeed((float) value);
		playerSpawnTimer.ValueChanged += value => SetPlayerSpawnTimer((float) value);

		weaponDamage.ValueChanged += value => SetWeaponDamage((float) value);
		weaponDurability.ValueChanged += value => SetWeaponDurability((float) value);

		forgeHealth.ValueChanged += value => SetForgeMaxHealth((float) value);
		forgeSmeltTimer.ValueChanged += value => SetForgeSmeltTimer((float) value);

		grindstoneSmeltTimer.ValueChanged += value => SetGrindstoneSmeltTimer((float) value);
		showCollisions.Toggled += value => ShowCollisionShapes((bool) value);

		goblinEnemySpawnChance.ValueChanged += value => SetEnemySpawnChance((float)value);
		skeletonEnemySpawnChance.ValueChanged += value => SetEnemySpawnChance((float)value);
		HobgoblinEnemySpawnChance.ValueChanged += value => SetEnemySpawnChance((float)value);

		maxWaves.ValueChanged += value => SetMaxWaves((float)value);
		timeBetweenEnemies.ValueChanged += value => SetTimeBetweenEnemies((float)value);
		startingEnemies.ValueChanged += value => SetStartingEnemies((float)value);
		enemyIncreasePerWave.ValueChanged += value => SetEnemyIncreasePerWave((float)value);
		timeBetweenWaves.ValueChanged += value => SetTimeBetweenWaves((float)value);

	}

	private void GetObjectReferences()
    {
        forge = GetNode<Forge>("/root/Forge");
		waveManager = GetTree().Root.GetNode<Node>("TestingLab/EnemySpawner") as WaveManager;

		forgeScript = GetTree().Root.GetNode<Node>("TestingLab/World/Forge") as SmeltingStation;
		grindstoneScript = GetTree().Root.GetNode<Node>("TestingLab/World/Grindstone") as GrindstoneStation;
    }
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

	}

	private void SetupValues()
	{
		foreach(Node p in GetTree().GetNodesInGroup("Player"))
		{
			var player = p as PlayerController;
			if(player == null) continue;
			playerMaxHealth.Value = player.maxHealth;
			playerSpeed.Value= player.speed;
			playerSpawnTimer.Value = player.playerSpawnTimer;
			
			var playerSword = player.currentSword;
			var sword = playerSword as Sword;

			weaponDurability.Value = sword.durability;
			weaponDamage.Value = sword.damage;
		}

		goblinEnemySpawnChance.Value = waveManager.normalEnemyChance;
		skeletonEnemySpawnChance.Value = waveManager.fastEnemyChance;
		HobgoblinEnemySpawnChance.Value = waveManager.strongEnemyChance;


		forgeHealth.Value = forge.maxHealth;
		forgeSmeltTimer.Value = forgeScript.GetCraftDuration();
		grindstoneSmeltTimer.Value = grindstoneScript.GetCraftDuration();

		maxWaves.Value = waveManager.maxWaves;
		timeBetweenEnemies.Value = waveManager.timeBetweenEnemySpawns;
		startingEnemies.Value = waveManager.startingEnemiesPerWave;
		enemyIncreasePerWave.Value = waveManager.enemyIncreasedPerWave;
		timeBetweenWaves.Value = waveManager.timeBetweemWaves;

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
	private void SetPlayerSpawnTimer(float value)
	{
		playerSpawnTimerOverride = value;
		foreach(Node p in GetTree().GetNodesInGroup("Player"))
		{
			var player = p as PlayerController;
			if(player != null) player.playerSpawnTimer = (int)value;
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
	
	private void SetEnemySpawnChance(float value)
    {

		var waveManager = GetTree().Root.GetNode<Node>("TestingLab/EnemySpawner") as WaveManager;

		goblinEnemySpawnChance.ValueChanged += v => waveManager.normalEnemyChance = (int)v;
		skeletonEnemySpawnChance.ValueChanged += v => waveManager.fastEnemyChance = (int)v;
		HobgoblinEnemySpawnChance.ValueChanged += v => waveManager.strongEnemyChance = (int)v;

		goblinEnemySpawnChance.Value = waveManager.normalEnemyChance;
		skeletonEnemySpawnChance.Value = waveManager.fastEnemyChance;
		HobgoblinEnemySpawnChance.Value = waveManager.strongEnemyChance;
    }
	
	private void SetMaxWaves(float value)
    {
		maxWavesOverirde = value;
        maxWaves.Value = value;
    } 
	private void SetTimeBetweenEnemies(float value){
		timeBetweenEnemiesOverride = value;
		timeBetweenEnemies.Value = value;
	}
	private void SetStartingEnemies(float value){
		startingEnemiesOverride = value;
		startingEnemies.Value = value;
	}
	private void SetEnemyIncreasePerWave(float value){
		enemyIncreasePerWaveOverride = value;
		enemyIncreasePerWave.Value = value;
	}
	private void SetTimeBetweenWaves(float value){
		timeBetweenWavesOverride = value;
		timeBetweenEnemies.Value = value;
	}
    

}

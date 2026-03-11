using Godot;
using System;
using System.Collections.Generic;

public partial class WaveManager : Node
{
	#region Exports
	[ExportGroup("References")]
	[Export] public Node spawnParent     { get; private set; }
	[Export] public Node3D targetNode    { get; private set; }
	[Export] public LootTable lootTable;

	[ExportGroup("Enemy Data")]
	[Export] public EnemyData normalEnemyData;
	[Export] public EnemyData fastEnemyData;
	[Export] public EnemyData strongEnemyData;

	[ExportGroup("Enemy Spawn Chances")]
	[Export(PropertyHint.Range, "0,100,1")] public int normalEnemyChance = 60;
	[Export(PropertyHint.Range, "0,100,1")] public int fastEnemyChance   = 20;
	[Export(PropertyHint.Range, "0,100,1")] public int strongEnemyChance = 20;

	[ExportGroup("Wave Settings")]
	[Export] public int   maxWaves               { get; set; } = 3;
	[Export] public float timeBetweenEnemySpawns        { get; private set; } = 2.0f;
	[Export] public int   startingEnemiesPerWave  { get; set; }
	[Export] public int   enemyIncreasedPerWave   { get; set; }
	[Export] public float timeBetweemWaves { get; set;}

	[ExportGroup("Spawn Positions")]
	[Export] public Node3D[] spawnPositions = new Node3D[0];

	[ExportGroup("UI")]
	[Export] private Label enemyUI;
	[Export] private Label3D waveTimerLabel3D;

	#endregion

	#region Signals
	[Signal] public delegate void WaveStartedEventHandler(int waveNumber);
	[Signal] public delegate void WaveCompletedEventHandler(int waveNumber);
	[Signal] public delegate void AllWavesCompletedEventHandler();
	#endregion

	#region Private Variables
	private int currentWave            = 0;
	private int enemiesSpawnedThisWave = 0;
	private int enemiesToSpawnThisWave = 0;
	private int enemiesAliveThisWave   = 0;
	private int enemiesLeft            = 0;
	private int enemiesMultiplier      = 1;

	private Timer spawnTimer;
	private RandomNumberGenerator rnd           = new RandomNumberGenerator();
	private List<EnemyController> activeEnemies = new List<EnemyController>();
	private InputManager inputManager;

	private float waveTimer = 0f;
	private bool isWaiting = false;

	#endregion

	#region Lifecycle
	public override void _Ready()
	{
		waveTimerLabel3D.Visible = false;

		SetupDefaults();
		SetupTimer();

		inputManager = GetNode<InputManager>("/root/InputManager");
		if (inputManager == null)
		{
			GD.PrintErr("WaveManager: InputManager not found!");
			return;
		}

		GD.Print($"WaveManager Ready — {spawnPositions?.Length ?? 0} spawn position(s) found.");
		if (spawnPositions != null)
		{
			for (int i = 0; i < spawnPositions.Length; i++)
			{
				if (spawnPositions[i] != null)
					GD.Print($"  Spawn {i}: {spawnPositions[i].Name} at {spawnPositions[i].GlobalPosition}");
				else
					GD.Print($"  Spawn {i}: NULL");
			}
		}
		waveTimer = timeBetweemWaves;
		isWaiting = true;
		waveTimerLabel3D.Visible = true;
	
	}

	public override void _Process(double delta)
	{
		if (isWaiting)
		{
			waveTimer -= (float)delta;
			waveTimerLabel3D.Text = $"Time until next Wave: {Mathf.Ceil(waveTimer)}";

			if(waveTimer <= 0)
			{
				isWaiting = false;
				waveTimerLabel3D.Visible = false;
				StartNewWave();
			}
		}

		if (enemyUI != null)
			enemyUI.Text = $" Enemies alive: {enemiesLeft} \n Max Waves: {maxWaves}  Current wave: {currentWave} ";
	}
	#endregion

	#region Setup
	private void SetupDefaults()
	{
		rnd.Randomize();

		if (spawnParent == null)
			spawnParent = this;

		if (spawnPositions == null || spawnPositions.Length == 0)
		{
			GD.Print("Spawn positions not set, attempting to auto-find...");
			FindSpawnPositions();
		}

		// Fallback EnemyData if none assigned in Inspector
		if (normalEnemyData == null)
			normalEnemyData = CreateFallbackData("NormalEnemy", 10, 1, 5.0f, Vector3.One);

		if (fastEnemyData == null)
			fastEnemyData = CreateFallbackData("FastEnemy", 6, 1, 9.0f, Vector3.One);

		if (strongEnemyData == null)
		{
			strongEnemyData = CreateFallbackData("StrongEnemy", 25, 3, 3.0f, new Vector3(2, 2, 2));
		}
	}

	private EnemyData CreateFallbackData(string name, int health, int damage, float speed, Vector3 scale)
	{
		return new EnemyData
		{
			enemyName = name,
			maxHealth = health,
			damage    = damage,
			speed     = speed,
			scale     = scale,
		};
	}

	private void FindSpawnPositions()
	{
		Node spawnerParent = GetNodeOrNull("../EnemySpawner")
			?? GetTree().CurrentScene.FindChild("EnemySpawner", true, false);

		if (spawnerParent == null)
		{
			GD.PrintErr("WaveManager: Could not find EnemySpawner node!");
			return;
		}

		var points       = new List<Node3D>();
		var invalidNames = new List<string>();

		foreach (Node child in spawnerParent.GetChildren())
		{
			if (child is Node3D point)
			{
				if (point.GlobalPosition != Vector3.Zero)
					points.Add(point);
				else
					invalidNames.Add(point.Name);
			}
		}

		spawnPositions = points.ToArray();
		GD.Print($"WaveManager: Found {spawnPositions.Length} valid spawn position(s).");

		foreach (var name in invalidNames)
			GD.PrintErr($"  Skipped spawn point '{name}' — positioned at (0,0,0).");
	}

	private void SetupTimer()
	{
		spawnTimer          = new Timer();
		spawnTimer.Name     = "SpawnTimer";
		spawnTimer.OneShot  = false;
		spawnTimer.WaitTime = timeBetweenEnemySpawns;
		spawnTimer.Timeout += OnSpawnTimerTimeout;
		AddChild(spawnTimer);
	}

	private int SetupEnemyMultiplier()
	{
		int playerCount = inputManager.GetAssignedPlayerCount();
		enemiesMultiplier = playerCount switch
		{
			1 => 2,
			2 => 3,
			3 => 4,
			_ => 5
		};
		return enemiesMultiplier;
	}
	#endregion

	#region Wave Management
	private void StartNewWave()
	{
		if (currentWave >= maxWaves)
		{
			OnAllWavesCompleted();
			GetTree().Quit();
			return;
		}

		currentWave++;
		enemiesSpawnedThisWave = 0;
		enemiesToSpawnThisWave = CalculateEnemiesForWave();
		enemiesAliveThisWave   = 0;
		enemiesLeft            = enemiesToSpawnThisWave;

		EmitSignal(SignalName.WaveStarted, currentWave);
		GD.Print($"Wave {currentWave} started — spawning {enemiesToSpawnThisWave} enemies.");
		spawnTimer.Start();
	}

	private int CalculateEnemiesForWave()
	{
		enemiesMultiplier = SetupEnemyMultiplier();
		return startingEnemiesPerWave + ((currentWave - 1) * enemiesMultiplier * enemyIncreasedPerWave);
	}

	private void OnWaveCompleted()
	{
		waveTimerLabel3D.Visible = true;
		waveTimer = timeBetweemWaves;
		isWaiting = true;
		
		EmitSignal(SignalName.WaveCompleted, currentWave);
	}

	private void OnAllWavesCompleted()
	{
		spawnTimer.Stop();
		EmitSignal(SignalName.AllWavesCompleted);
	}
	#endregion

	#region Spawning
	private void OnSpawnTimerTimeout()
	{
		if (enemiesSpawnedThisWave < enemiesToSpawnThisWave)
		{
			int remainingEnemies = enemiesToSpawnThisWave - enemiesSpawnedThisWave;
			int clumpSize= Mathf.Min(GD.RandRange(1, 5), remainingEnemies);

			SpawnEnemy(clumpSize);

			enemiesSpawnedThisWave += clumpSize;
		}

		if (enemiesSpawnedThisWave >= enemiesToSpawnThisWave)
			spawnTimer.Stop();
	}

	private void SpawnEnemy(int clumpSize)
	{
		if (spawnParent == null || targetNode == null) return;

		Vector3 clumpCenter = GetRandomSpawnPosition();

		for(int i = 0; i < clumpSize; i++)
		{
			EnemyData selectedData = GetRandomEnemyData();
			if (selectedData == null) return;

			EnemyController enemy = EnemyController.Create(selectedData, targetNode, spawnParent);
			// float angle = (i / (float)(clumpSize)) * Mathf.Tau;
			// float radius = GD.RandRange((int)1f, (int)3f);
			// Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
			
			Vector3 randomOffSet = new Vector3 ( GD.RandRange(-3, 3), 0, GD.RandRange(-3, 3));
			// GlobalPosition must be set AFTER AddChild (enemy is in the tree at this point)
			enemy.GlobalPosition = clumpCenter + randomOffSet;

			activeEnemies.Add(enemy);
			enemiesAliveThisWave++;

			enemy.Died          += OnEnemyDied;
			enemy.DamagedTarget += OnEnemyDamagedTarget;
		}
		
		
	}

	private EnemyData GetRandomEnemyData()
	{

		int total = normalEnemyChance + fastEnemyChance + strongEnemyChance;
		if (total == 0) total = 100;

		float normalThreshold = (float)normalEnemyChance / total * 100;
		float fastThreshold   = normalThreshold + (float)fastEnemyChance / total * 100;

		int roll = rnd.RandiRange(1, 100);

		if (roll <= normalThreshold) return normalEnemyData;
		if (roll <= fastThreshold)   return fastEnemyData;
		return                              strongEnemyData;
	}

	private Vector3 GetRandomSpawnPosition()
	{
		if (spawnPositions == null || spawnPositions.Length == 0)
		{
			GD.PrintErr("WaveManager: No spawn positions available, using fallback.");
			return new Vector3(10, 1, 10);
		}

		int index = rnd.RandiRange(0, spawnPositions.Length - 1);

		if (spawnPositions[index] == null)
		{
			GD.PrintErr($"WaveManager: Spawn position [{index}] is null, using fallback.");
			return new Vector3(10, 1, 10);
		}

		return spawnPositions[index].GlobalPosition;
	}
	#endregion

	#region Enemy Events
	private void OnEnemyDied(EnemyController enemy, Vector3 deathPosition)
	{
		activeEnemies.Remove(enemy);
		enemiesAliveThisWave--;
		enemiesLeft--;
		float roll = (float)GD.RandRange(0, 99);
		if(roll <= enemy.LootDropChance)
		{
			lootTable?.GetLoot(enemy, deathPosition);
		}


		if (enemiesAliveThisWave <= 0 && enemiesSpawnedThisWave >= enemiesToSpawnThisWave)
			OnWaveCompleted();
	}

	private void OnEnemyDamagedTarget(Node3D target, int damage) { }
	#endregion

	#region Public Methods
	public int GetCurrentWave()          => currentWave;
	public int GetEnemiesAlive()         => enemiesAliveThisWave;
	public int GetTotalEnemiesThisWave() => enemiesToSpawnThisWave;
	#endregion
}

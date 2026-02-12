using Godot;
using System;
using System.Collections.Generic;

public partial class WaveManager : Node
{
	#region Exports
	[Export] public PackedScene enemyScene {get; private set;}
	[Export] public Node spawnParent { get; private set;}
	[Export] public Node3D targetNode { get; private set;}

	// Enemy type resources //
	[Export] public EnemyData normalEnemyData;
	[Export] public EnemyData fastEnemyData;
	[Export] public EnemyData strongEnemyData;
	[Export] public LootTable lootTable;

	[Export(PropertyHint.Range, "0,100,1")] public int normalEnemyChance = 60;
	[Export(PropertyHint.Range, "0,100,1")] public int fastEnemyChance = 20;
	[Export(PropertyHint.Range, "0,100,1")] public int strongEnemyChance = 62;

	// Wave Exports

	[Export] public int maxWaves { get; set;} = 10;
	[Export] public float timeBetweenWaves { get; private set;} = 2.0f;
	[Export] public int startingEnemiesPerWave {get; set;} = 5;
	[Export] public int enemyIncreasedPerWave {get; set;} = 2;

	// Spawn position //

	[Export] public Node3D[] spawnPositions = new Node3D[0];
	#endregion

	#region Signals
	[Signal] public delegate void WaveStartedEventHandler(int waveNumber);
	[Signal] public delegate void WaveCompletedEventHandler(int waveNumber);
	[Signal] public delegate void AllWavesCompletedEventHandler();
	#endregion

	#region Private Variables
	private int currentWave= 0;
	private int enemiesSpawnedThisWave = 0;
	private int enemiesToSpawnThisWave = 0;
	private int enemiesAliveThisWave = 0;

	private Timer spawnTimer;
	private RandomNumberGenerator rnd = new RandomNumberGenerator();

	private List<Enemy> activeEnemies = new List<Enemy>();
	#endregion

	#region LifeCycle
	public override void _Ready()
	{
		SetupDefaults();
		SetupTimer();
		
		// Debug spawn positions
		GD.Print($"WaveManager Ready - Checking spawn positions...");
		GD.Print($"spawnPositions array length: {spawnPositions?.Length ?? 0}");
		
		if (spawnPositions != null)
		{
			for (int i = 0; i < spawnPositions.Length; i++)
			{
				if (spawnPositions[i] != null)
				{
					GD.Print($"Spawn {i}: {spawnPositions[i].Name} at {spawnPositions[i].GlobalPosition}");
				}
				else
				{
					GD.Print($"Spawn {i}: NULL");
				}
			}
		}
		
		StartNewWave();
	}
	#endregion

	#region Setup
	private void SetupDefaults()
	{
		rnd.Randomize();

		if(enemyScene == null)
		{
			enemyScene = GD.Load<PackedScene>("res://assets/models/Enemy.tscn");
		} 
		if(spawnParent == null)
		{
			spawnParent = this;
		}

		// AUTO-FIND SPAWN POSITIONS IF NOT SET
		if(spawnPositions == null || spawnPositions.Length == 0)
		{
			GD.Print("Spawn positions not set, attempting to auto-find...");
			FindSpawnPositions();
		}

		if(normalEnemyData == null)
		{
			normalEnemyData = CreateDefaultEnemyData("Normal", 10, 10, 5.0f, new Color(1, 0, 0));
		}
		if(fastEnemyData == null)
		{
			fastEnemyData = CreateDefaultEnemyData("Fast", 10, 5, 8.0f, new Color(0, 0, 1));
		}
		if(strongEnemyData == null)
		{
			strongEnemyData = CreateDefaultEnemyData("Strong", 20, 15, 3.0f, new Color(1, 0, 1));
			strongEnemyData.scale = new Vector3 (2, 2, 2);
		}
	}
	
	private void FindSpawnPositions()
{
	// Try to find an EnemySpawner parent node
	Node spawnerParent = GetNodeOrNull("../EnemySpawner");
	
	if (spawnerParent == null)
	{
		spawnerParent = GetTree().CurrentScene.FindChild("EnemySpawner", true, false);
	}
	
	if (spawnerParent == null)
	{
		GD.PrintErr("Could not find EnemySpawner node!");
		return;
	}
	
	// Find all valid spawn points (exclude those at 0,0,0)
	var points = new List<Node3D>();
	var invalidPoints = new List<string>();
	
	foreach (Node child in spawnerParent.GetChildren())
	{
		if (child is Node3D point)
		{
			// Check if position is valid (not at origin)
			if (point.GlobalPosition != Vector3.Zero)
			{
				points.Add(point);
				GD.Print($"✓ Valid spawn: {point.Name} at {point.GlobalPosition}");
			}
			else
			{
				invalidPoints.Add(point.Name);
				GD.PrintErr($"✗ SKIPPED: {point.Name} is at (0,0,0) - move it in the editor!");
			}
		}
	}
	
	spawnPositions = points.ToArray();
	
	GD.Print($"Found {spawnPositions.Length} VALID spawn positions");
	
	if (invalidPoints.Count > 0)
	{
		GD.PrintErr($"WARNING: {invalidPoints.Count} spawn points were skipped because they're at (0,0,0):");
		foreach (var name in invalidPoints)
		{
			GD.PrintErr($"  - {name}");
		}
		GD.PrintErr("Go to Godot editor and move these spawn points to proper positions!");
	}
	
	if (spawnPositions.Length == 0)
	{
		GD.PrintErr("CRITICAL: No valid spawn positions! All are at (0,0,0)!");
	}
}
	
	private EnemyData CreateDefaultEnemyData(string name, int health, int damage, float speed, Color color)
	{
		return new EnemyData
		{
			enemyName = name,
			health = health,
			damage = damage,
			speed = speed,
			color = color,
			scale = Vector3.One
		};
	}
	
	private void SetupTimer()
	{
		spawnTimer = new Timer();
		spawnTimer.Name = "SpawnTimer";
		AddChild(spawnTimer);

		spawnTimer.OneShot = false;
		spawnTimer.WaitTime = timeBetweenWaves;
		spawnTimer.Timeout += OnSpawnTimerTimeout;
	}
	#endregion

	#region Wave Management

	private void StartNewWave()
	{
		if(currentWave >= maxWaves)
		{
			OnAllWavesCompleted();
			return;
		}

		currentWave++;
		enemiesSpawnedThisWave = 0;
		enemiesToSpawnThisWave = CalculateEnemiesForWave();
		enemiesAliveThisWave = 0;

		EmitSignal(SignalName.WaveStarted, currentWave);
		GD.Print($"Wave {currentWave} started - Spawning {enemiesToSpawnThisWave} enemies");

		spawnTimer.Start();
	}

	private int CalculateEnemiesForWave()
	{
		return startingEnemiesPerWave + ((currentWave -1) * enemyIncreasedPerWave);
	}

	private void OnWaveCompleted()
	{
		EmitSignal(SignalName.WaveCompleted, currentWave);
		GetTree().CreateTimer(3.0f).Timeout += StartNewWave;
	}

	private void OnAllWavesCompleted()
	{
		spawnTimer.Stop();
		EmitSignal(SignalName.AllWavesCompleted);
	}
	#endregion

	private void OnSpawnTimerTimeout()
	{
		if(enemiesSpawnedThisWave < enemiesToSpawnThisWave)
		{
			SpawnEnemy();
			enemiesSpawnedThisWave++;
		}

		if(enemiesSpawnedThisWave >= enemiesToSpawnThisWave)
		{
			spawnTimer.Stop();
		}
	}
	
	private void SpawnEnemy()
	{
		if(enemyScene == null || spawnParent == null)
		{
			return;
		}
		if(targetNode == null)
		{
			return;
		}
		
		EnemyData selectedData = GetRandomEnemyType();

		Enemy enemy = enemyScene.Instantiate<Enemy>();
		
		// Add to scene tree FIRST
		spawnParent.AddChild(enemy);
		
		// Then set GLOBAL position (after it's in the tree)
		enemy.GlobalPosition = GetRandomSpawnPosition();
		
		enemy.Initialize(selectedData, targetNode);

		activeEnemies.Add(enemy);
		enemiesAliveThisWave++;

		enemy.Died += OnEnemyDied;
		enemy.DamagedTarget += OnEnemyDamagedTarget;
	}

	#region Enemy Type
	private EnemyData GetRandomEnemyType()
	{
		int total = normalEnemyChance + fastEnemyChance + strongEnemyChance;
		if (total == 0) total = 100;

		float normalChance = (float)normalEnemyChance / total * 100;
		float fastChance = (float)fastEnemyChance / total * 100;

		int roll = rnd.RandiRange(1, 100);
		if(roll <= normalChance)
		{
			return normalEnemyData;
		}
		else if (roll <= normalChance + fastChance)
		{
			return fastEnemyData;
		}
		else
		{
			return strongEnemyData;
		}
	}
	#endregion

	private Vector3 GetRandomSpawnPosition()
	{
		if(spawnPositions == null || spawnPositions.Length == 0)
		{
			GD.PrintErr("WaveManager: No spawn positions set! Spawning at default position.");
			// Return a position away from 0,0,0 so you can see the issue
			return new Vector3(10, 1, 10);
		}
		
		int index = rnd.RandiRange(0, spawnPositions.Length - 1);
		
		if (spawnPositions[index] == null)
		{
			GD.PrintErr($"WaveManager: Spawn position at index {index} is null!");
			return new Vector3(10, 1, 10);
		}
		
		Vector3 spawnPos = spawnPositions[index].GlobalPosition;
		GD.Print($"Spawning enemy at spawn point {index}: {spawnPos}");
		
		return spawnPos;
	}

	private void OnEnemyDied(Enemy enemy, Vector3 deathPosition)
	{
		activeEnemies.Remove(enemy);
		enemiesAliveThisWave--;

		if(lootTable != null)
		{
			lootTable.GetLoot(enemy, deathPosition);
		}
		if(enemiesAliveThisWave <= 0 && enemiesSpawnedThisWave >= enemiesToSpawnThisWave)
		{
			OnWaveCompleted();
		}

	}

	private void OnEnemyDamagedTarget(Node3D target, int damage)
	{
		
	}

	#region Public Methods
	public int GetCurrentWave() => currentWave;
	public int GetEnemiesAlive() => enemiesAliveThisWave;
	public int GetTotalEnemiesThisWave() => enemiesToSpawnThisWave;
	#endregion

}

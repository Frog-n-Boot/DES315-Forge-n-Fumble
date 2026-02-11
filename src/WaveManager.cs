using Godot;
using System;
using System.Collections.Generic;

public partial class WaveManager : Node3D
{
    #region Exports
    [Export] public PackedScene enemyScene {get; private set;}
    [Export] public Node3D spawnParent { get; private set;}
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
        StartNewWave();
    }
    #endregion

    #region Setup
    private void SetupDefaults()
    {
        rnd.Randomize();

        if(enemyScene == null)
        {
            enemyScene = GD.Load<PackedScene>("res://Objects/Enemy.tscn");
        }
        if(spawnParent == null)
        {
            spawnParent = this;
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
            strongEnemyData = CreateDefaultEnemyData("SDtrong", 20, 15, 3.0f, new Color(1, 0, 1));
            strongEnemyData.scale = new Vector3 (2, 2, 2);
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
        enemy.Position = GetRandomSpawnPosition();

        spawnParent.AddChild(enemy);
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
            return Vector3.Zero;
        }
        int index = rnd.RandiRange(0, spawnPositions.Length - 1);
        return spawnPositions[index].GlobalPosition;
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
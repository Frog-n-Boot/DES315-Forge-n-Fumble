using Godot;
using System;
using System.Collections.Generic;

public partial class LevelManager : Node
{
	[ExportGroup("Enemies")]
	[Export] EnemyData normalEnemyData;
	[Export] EnemyData fastEnemyData;
	[Export] EnemyData strongEnemyData;
	[Export] int normalEnemyNumber;
	[Export] int fastEnemyNumber;
	[Export] int storngEnemyNumber;

	[ExportCategory("Items")]
	[ExportGroup("Ore")]
	[Export] public PackedScene copperOreScene { get; private set; }
	[Export] int copperOreCount;
	[Export] public PackedScene ironOreScene { get; private set; }
	[Export] int ironOreCount;
	[Export] public PackedScene goldOreScene { get; private set; }
	[Export] int goldOreCount;
	[Export] public PackedScene damasOreScene { get; private set; }
	[Export] int damasOreCount;

	[ExportGroup("Ingots")]
	[Export] public PackedScene copperIngotScene { get; private set; }
	[Export] int copperIngotCount;
	[Export] public PackedScene ironIngotScene { get; private set; }
	[Export] int ironIngotCount;
	[Export] public PackedScene goldIngotScene { get; private set; }
	[Export] int goldIngotCount;
	[Export] public PackedScene damasIngotScene { get; private set; }
	[Export] int damasIngotCount;

	[ExportGroup("Weapons")]
	[Export] public PackedScene copperDullSwordScene { get; private set; }
	[Export] int copperDullSwordCount;
	[Export] public PackedScene copperSwordScene { get; private set; }
	[Export] int copperSwordCount;
	[Export] public PackedScene ironDullSwordScene { get; private set; }
	[Export] int ironDullSwordCount;
	[Export] public PackedScene ironSwordScene { get; private set; }
	[Export] int ironSwordCount;

	[ExportGroup("Other")]
	[Export] public PackedScene healthPackObject { get; private set; }
	[Export] int healthpackCount;
	
	private Node enemyNode = new Node();
	private Node weaponNode = new Node();
	private Node oreNode = new Node();
	private Node ingotNode = new Node();

	private List<PackedScene> oreList = new List<PackedScene>();
	private List<PackedScene> weaponList = new List<PackedScene>();
	private List<PackedScene> ingotList = new List<PackedScene>();
	private List<PackedScene> enemiesList = new List<PackedScene>();
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		InitNodes();
		InitWeapons();
		InitOres();
		InitIngots();
	}

	private void InitNodes()
	{
		enemyNode.Name = "Enemies";	
		weaponNode.Name = "Weapons";	
		oreNode.Name = "Ores";	
		ingotNode.Name = "Ingots";

		AddChild(enemyNode);
		AddChild(weaponNode);
		AddChild(oreNode);
		AddChild(ingotNode);
	}

	private void InitEnemies()
	{
		var enemeis = new[]
		{
			(Scene:normalEnemyData, Count:normalEnemyNumber),
			(Scene:fastEnemyData, Count:fastEnemyNumber),
			(Scene:strongEnemyData, Count:storngEnemyNumber),
		};


		
	}
	private void InitWeapons()
	{
		var weapons = new[]
		{
			(Scene:copperDullSwordScene, Count:copperDullSwordCount),
			(Scene:copperSwordScene, Count:copperSwordCount),
			(Scene:ironDullSwordScene, Count:ironDullSwordCount),
			(Scene:ironSwordScene, Count:ironSwordCount),
		};


		foreach(var weapon in weapons)
		{
			for(int i = 0; i < weapon.Count; i++)
			{
				Node3D weaponInstance = weapon.Scene.Instantiate<Node3D>();
				weaponInstance.Name = $"{weaponInstance.Name}_{i}";
				weaponInstance.Visible = false;
				weaponInstance.ProcessMode = ProcessModeEnum.Disabled;
				weaponNode.AddChild(weaponInstance);
			}
		}
	}
	private void InitOres()
	{
		var ores = new[]
		{
			(Scene:copperOreScene, Count:copperOreCount),
			(Scene:ironOreScene, Count:ironOreCount),
			(Scene:goldOreScene, Count:goldOreCount),
			(Scene:damasOreScene, Count:damasOreCount),
		};

		foreach(var ore in ores)
		{
			for(int i = 0; i < ore.Count; i++)
			{
				Node3D oreInstance = ore.Scene.Instantiate<Node3D>();
				oreInstance.Name = $"{oreInstance.Name}_{i}";
				oreInstance.Visible = false;
				oreInstance.ProcessMode = ProcessModeEnum.Disabled;
				Area3D area = oreInstance.GetNode<Area3D>("Area3D");
				area.SetDeferred("monitoring", false);
				area.SetDeferred("monitorable", false);
				oreNode.AddChild(oreInstance);
				
			}
		}
	}
	private void InitIngots()
	{
		var ingots = new[]
		{
			(Scene:copperIngotScene, Count:copperIngotCount),
			(Scene:ironIngotScene, Count:ironIngotCount),
			(Scene:goldIngotScene, Count:goldIngotCount),
			(Scene:damasIngotScene, Count:damasIngotCount),
		};

		foreach(var ingot in ingots)
		{
			for(int i = 0; i < ingot.Count; i++)
			{
				Node3D ingotInstance = ingot.Scene.Instantiate<Node3D>();
				ingotInstance.Name = $"{ingotInstance.Name}_{i}";
				ingotInstance.Visible = false;
				ingotInstance.ProcessMode = ProcessModeEnum.Disabled;
				Area3D area = ingotInstance.GetNode<Area3D>("Area3D");
				area.SetDeferred("monitoring", false);
				area.SetDeferred("monitorable", false);
				ingotNode.AddChild(ingotInstance);
				
			}
		}
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

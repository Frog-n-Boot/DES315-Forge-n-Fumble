using Godot;
using System;
using System.Collections.Generic;

public partial class LevelManager : Node
{
	[ExportGroup("Enemies")]
	[Export] private PackedScene enemyScene;
	[Export] EnemyData normalEnemyData;
	[Export] EnemyData fastEnemyData;
	[Export] EnemyData strongEnemyData;
	[Export] int normalEnemyNumber;
	[Export] int fastEnemyNumber;
	[Export] int strongEnemyNumber;

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

	private Dictionary<PackedScene, List<Node3D>> pool = new();
	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// InitNodes();
		// InitWeapons();
		// InitOres();
		// InitIngots();
		// //InitEnemies();
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
		var enemies = new[]
		{
			(Data:normalEnemyData, Count:normalEnemyNumber),
			(Data:fastEnemyData, Count:fastEnemyNumber),
			(Data:strongEnemyData, Count:strongEnemyNumber),
		};
		foreach(var enemy in enemies)
		{
			for(int i = 0; i < enemy.Count; i++)
			{
				var instance = enemyScene.Instantiate<EnemyController>();

				instance.enemyData = enemy.Data;
				
				instance.Visible = false;
				instance.ProcessMode = ProcessModeEnum.Disabled;

				enemyNode.AddChild(instance);

				if(!pool.ContainsKey(enemyScene))
					pool[enemyScene] = new List<Node3D>();
				
				pool[enemyScene].Add(instance);
			}
		}
		
		
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

				if(!pool.ContainsKey(weapon.Scene))
					pool[weapon.Scene] = new List<Node3D>();
				
				pool[weapon.Scene].Add(weaponInstance);
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

				if(!pool.ContainsKey(ore.Scene))
					pool[ore.Scene] = new List<Node3D>();
				
				pool[ore.Scene].Add(oreInstance);
				
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
				
				if(!pool.ContainsKey(ingot.Scene))
					pool[ingot.Scene] = new List<Node3D>();
				
				pool[ingot.Scene].Add(ingotInstance);
			}
		}
	}

	public Node3D GetFromPool(PackedScene scene)
	{
		if (!pool.ContainsKey(scene))
		{
			GD.PrintErr($"No pool for scene: {scene.ResourcePath}");
			return null;
		}

		foreach(var obj in pool[scene])
		{
			if (!obj.Visible)
			{

				obj.Visible = true;
				obj.ProcessMode = ProcessModeEnum.Inherit;

				obj.SetProcess(true);
				obj.SetPhysicsProcess(true);

				var area = obj.GetNodeOrNull<Area3D>("Area3D");
				if(area != null)
				{
					area.SetDeferred("monitoring", true);
					area.SetDeferred("monitorable", true);
				}

				return obj;
			}
		}
		GD.PrintErr($"Pool exhausted for: {scene.ResourcePath}");
		return null;
	}

	public void ReturnToPool(Node3D obj)
	{
		if(obj.GetParent() != null)
			obj.GetParent().RemoveChild(obj);
		
		

		obj.Visible = false;
		obj.ProcessMode = ProcessModeEnum.Disabled;

		var area = obj.GetNodeOrNull<Area3D>("Area3D");
		if(area != null)
		{
			area.SetDeferred("monitoring", false);
			area.SetDeferred("monitorable", false);
		}
	}
	public Node3D GetWeapon(PackedScene scene) => GetFromPool(scene);
	public Node3D GetOre(PackedScene scene) => GetFromPool(scene);
	public Node3D GetIngot(PackedScene scene) => GetFromPool(scene);
	public Node3D GetEnemy(PackedScene scene) => GetFromPool(scene);

	public void ReturnWeapon(Node3D obj) => ReturnToPool(obj);
	public void ReturnOre(Node3D obj) => ReturnToPool(obj);
	public void ReturnIngot(Node3D obj) => ReturnToPool(obj);
	public void ReturnEnemy(Node3D obj) => ReturnToPool(obj);
	
}

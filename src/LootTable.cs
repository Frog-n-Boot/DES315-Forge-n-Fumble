using Godot;
using System;

public partial class LootTable : Node3D
{

	#region Variables

	/* ---- Percentage range for the items ----- */
	[Export(PropertyHint.Range, "0,100,1")] public int dropChance = 60;
	[Export(PropertyHint.Range, "0,100,1")] public int copperDropChance = 40;
	[Export(PropertyHint.Range, "0,100,1")] public int ironDropChance = 30;
	[Export(PropertyHint.Range, "0,100,1")] public int goldDropChance =20;
	[Export(PropertyHint.Range, "0,100,1")] public int damasDropChance = 10;
	//[Export(PropertyHint.Range, "0,100,1")] public int ingotDropChance;
	[Export(PropertyHint.Range, "0,100,1")] public int healthPackDropChance = 20;

	/* ---- Packed scenes ---- */
	//private PackedScene item;
	[Export] public PackedScene copperOre { get; private set; }
	[Export] public PackedScene ironOre { get; private set; }
	[Export] public PackedScene goldOre { get; private set; }
	[Export] public PackedScene damasOre { get; private set; }
	//[Export] public PackedScene ingotObject { get; private set; }
	[Export] public PackedScene healthPackObject { get; private set; }

	/* ---- Random number generators ---- */
	RandomNumberGenerator rng = new RandomNumberGenerator();
	//RandomNumberGenerator dropType = new RandomNumberGenerator();

	#endregion

	#region Ready
	/* 
	Randomize num 
	Load the packed scenes if they are null when initializing object
	*/
	public override void _Ready()
	{
		
		rng.Randomize();
		if(copperOre == null)
			copperOre = GD.Load<PackedScene>("res://assets/models/FinalAssets_Low/Ore/Copper_Ore.tscn");
		if(ironOre == null)
			ironOre = GD.Load<PackedScene>("res://assets/models/FinalAssets_Low/Ore/Iron_Ore.tscn");
		if(goldOre == null)
			goldOre = GD.Load<PackedScene>("res://assets/models/FinalAssets_Low/Ore/Gold_Ore.tscn");
		if(damasOre == null)
			damasOre = GD.Load<PackedScene>("res://assets/models/FinalAssets_Low/Ore/Damas_Ore.tscn");

		if(healthPackObject == null)
		{
			healthPackObject = GD.Load<PackedScene>("res://assets/models/Healthpack.tscn");
		}

	}

	#endregion

	#region GetLoot


	public void GetLoot(EnemyController enemy, Vector3 dropPosition)
	{

		PackedScene itemToDrop = GetRandomItem();

		if(itemToDrop != null)
		{
			var droppedItem = itemToDrop.Instantiate<Node3D>();
			GetTree().Root.AddChild(droppedItem);
			droppedItem.GlobalPosition = dropPosition;

			if(droppedItem is Pickable pickable)
				pickable.shouldDespawn = true;
			
		}
		
	}

	private PackedScene GetRandomItem()
	{
		int totalChance = copperDropChance + ironDropChance + goldDropChance + damasDropChance + healthPackDropChance;

		int roll = rng.RandiRange(0, totalChance - 1);
		int currentChance = 0;
		
		currentChance += copperDropChance;
		if(roll < currentChance) return copperOre;

		currentChance += ironDropChance;
		if(roll < currentChance) return ironOre;

		currentChance += goldDropChance;
		if(roll < currentChance) return goldOre;

		currentChance += damasDropChance;
		if(roll < currentChance) return damasOre;

		return healthPackObject;
	}
	#endregion
}

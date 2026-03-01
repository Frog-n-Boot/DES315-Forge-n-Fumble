using Godot;
using System;

public partial class LootTable : Node3D
{



	#region Variables

	/* ---- Percentage range for the items ----- */
	[Export(PropertyHint.Range, "0,100,1")] public int oreDropChance;
	//[Export(PropertyHint.Range, "0,100,1")] public int ingotDropChance;
	[Export(PropertyHint.Range, "0,100,1")] public int healthPackDropChance;

	/* ---- Packed scenes ---- */
	private PackedScene item;
	[Export] public PackedScene oreObject { get; private set; }
	//[Export] public PackedScene ingotObject { get; private set; }
	[Export] public PackedScene healthPackObject { get; private set; }

	/* ---- Random number generators ---- */
	RandomNumberGenerator num = new RandomNumberGenerator();
	RandomNumberGenerator dropType = new RandomNumberGenerator();

	#endregion

	#region Ready
	/* 
	Randomize num 
	Load the packed scenes if they are null when initializing object
	*/
	public override void _Ready()
	{
		
		num.Randomize();
		if(oreObject == null)
		{
			oreObject = GD.Load<PackedScene>("res://assets/models/Ore.tscn");
		}
		// if(ingotObject == null)
		// {
		// 	ingotObject = GD.Load<PackedScene>("res://Objects/Ingot.tscn");
		// }
		if(healthPackObject == null)
		{
			healthPackObject = GD.Load<PackedScene>("res://assets/models/Healthpack.tscn");
		}

	}

	#endregion

	#region GetLoot


	public void GetLoot(EnemyController enemy, Vector3 dropPosition)
	{

		float random = num.RandfRange(0, 100);

		//Compare if random value is less then enemy dropChance
		if(random < 60)
		{
			int ore = Mathf.Clamp(oreDropChance, 0, 100);
			//int ingot = Mathf.Clamp(ingotDropChance, 0, 100);
			int healthPack = Mathf.Clamp(healthPackDropChance, 0, 100);
			int total = ore  + healthPack;

			if(total != 100)
			{
				float scale = 100.0f/ total;
				ore = Mathf.RoundToInt(ore * scale);
				//ingot = Mathf.RoundToInt(ingot * scale);
				healthPack = 100 - ore; //- ingot;
			}
			dropType.Randomize();
			int dropCase = dropType.RandiRange(1, 100);
			
			Node3D droppedItem = null;
			if(dropCase <= ore)
			{
				item = oreObject;
				droppedItem = item.Instantiate<Node3D>();
			}
			// else if(dropCase <= stick + ingot)
			// {
			// 	item = ingotObject;
			// 	droppedItem = item.Instantiate<Node3D>();
			// }
			else
			{
				item = healthPackObject;
				droppedItem = item.Instantiate<Node3D>();
			}

			//Spawns item in the world on the enemies position
			if(droppedItem != null)
			{
				GetTree().Root.AddChild(droppedItem);
				droppedItem.GlobalPosition = dropPosition;
			}
		}
	}
	#endregion
}

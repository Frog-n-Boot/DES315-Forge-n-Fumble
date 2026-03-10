using Godot;
using System;

public partial class AssemblyStation : BaseStationScript
{
	[Export] public PackedScene swordScene {get; private set;}
	//[Export] private AudioStreamPlayer3D assemblySoun;
	protected override void OnReady()
	{
	   	if(swordScene == null)
		{
			swordScene = GD.Load<PackedScene>("res://assets/models/MeltedIngot.tscn");	
		}
	}

	protected override string GetStationName() => "Assembly";

	protected override void OnCraftingRequirementsMet()
	{
		if(GetRequiredItems(out var items, out var recipe))
		{
			foreach(var item in items)
				itemCarrier.RemoveItem(item);
			
			craftingTimer.Start();
			//assemblySoun.Play();
		}
	}
	public float GetCraftDuration()=> craftDuration;
}

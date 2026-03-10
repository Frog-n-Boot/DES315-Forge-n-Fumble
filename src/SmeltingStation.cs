using Godot;
using System;
using System.Collections.Generic;

public partial class SmeltingStation : BaseStationScript
{
	[Export] private PackedScene meltedIngotScene;
	[Export] private AudioStreamPlayer3D audio;

	protected override void OnReady()
	{
	   	if(meltedIngotScene == null)
		{
			meltedIngotScene = GD.Load<PackedScene>("res://assets/models/MeltedIngot.tscn");;
		}
		
	}

	protected override void OnCraftingRequirementsMet()
	{
		if(GetRequiredItems(out var items, out var recipe))
		{
			foreach(var item in items)
				itemCarrier.RemoveItem(item);
			
			craftingTimer.Start();
			audio.Play();
			
		}
	}

	protected override string GetStationName() => "Forge";

	public float GetCraftDuration()=> craftDuration;
}

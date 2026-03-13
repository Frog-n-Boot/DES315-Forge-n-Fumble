using Godot;
using System;
using System.Collections.Generic;

public partial class GrindstoneStation : BaseStationScript
{
	[Export] public PackedScene swordScene {get; private set;}
	[Export] private AudioStreamPlayer3D grindstoneSound;
	protected override void OnReady()
	{
	   	if(swordScene == null)
		{
			swordScene = GD.Load<PackedScene>("res://assets/models/MeltedIngot.tscn");	
		}
	}

	protected override string GetStationName() => "Grindstone";

	protected override void OnCraftingRequirementsMet()
	{
		craftingTimer.Start();
		grindstoneSound.Play();
	}
	public float GetCraftDuration()=> craftDuration;

}

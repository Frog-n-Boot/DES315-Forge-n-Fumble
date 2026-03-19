using Godot;
using System;
using System.Collections.Generic;

public partial class GrindstoneStation : BaseStationScript
{
	[Export] public PackedScene swordScene {get; private set;}
	[Export] private AudioStreamPlayer3D grindstoneSound;
	private Sword depositedSowrd;

	protected override void OnReady()
	{
	   	if(swordScene == null)
		{
			swordScene = GD.Load<PackedScene>("res://assets/models/MeltedIngot.tscn");	
		}
	}

	protected override string GetStationName() => "Grindstone";

	public bool DepositSword(Sword sword)
    {
		GD.Print($"Deposit sword called, sword in group: {sword.IsInGroup("DullSword")}");
		if(!sword.IsInGroup("DullSword")) return false;
        if(depositedSowrd != null) return false;
		if(sword.sharpenedVersion == null) return false;


		depositedSowrd = sword;
		
		if(!isOutputOccupied() && craftingTimer.IsStopped())
			OnCraftingRequirementsMet();

		return true;
    }

	protected override void ProduceOutput()
    {
        if(depositedSowrd == null) return;

		var instance = depositedSowrd.sharpenedVersion.Instantiate<Node3D>();
		outputNode.AddChild(instance);
		instance.GlobalPosition = outputNode.GlobalPosition;
		timeProgressBar.Value = 0;

		depositedSowrd = null;
		pendingRecipe = null;
    }
	protected override void OnCraftingRequirementsMet()
	{
		craftingTimer.Start();
		grindstoneSound.Play();
	}
	public float GetCraftDuration()=> craftDuration;

}

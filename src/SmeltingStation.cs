using Godot;
using System;
using System.Collections.Generic;

public partial class SmeltingStation : BaseStationScript
{
	[Export] private PackedScene meltedIngotScene;
	[Export] private AudioStreamPlayer3D audio;
	[Export] private Area3D healthAura;
	[Export] private float healAmount = 5f;
	[Export] private float healInterval = 1f;

	private float healTimer = 0f;

	protected override void OnReady()
	{

	   	if(meltedIngotScene == null)
		{
			meltedIngotScene = GD.Load<PackedScene>("res://assets/models/MeltedIngot.tscn");;
		}
	}
	
	public override void _Process(double delta)
	{
		if(!craftingTimer.IsStopped()){
			healTimer += (float)delta;
			if(healTimer >= healInterval)
			{
				GetTree().CreateTimer(5f).Timeout += () => healTimer = 0f;
				HealPlayersInAura();
			}
		}
	}

	protected override void OnCraftingRequirementsMet()
	{
		craftingTimer.Start();
		audio.Play();
		
	}

	protected override string GetStationName() => "Forge";

	public float GetCraftDuration()=> craftDuration;

	private void HealPlayersInAura()
	{
		foreach(Node3D body in healthAura.GetOverlappingBodies())
		{
			if(body is PlayerController player)
				player.Heal(healAmount);
		}
	}
}

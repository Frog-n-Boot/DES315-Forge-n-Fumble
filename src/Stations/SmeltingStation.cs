using Godot;
using System;
using System.Collections.Generic;

public partial class SmeltingStation : BaseStationScript
{
	[Export] private AudioStreamPlayer3D audio;
	[Export] private Area3D healthAura;
	[Export] private float healAmount = 5f;
	[Export] private float healInterval = 1f;

	[Signal] public delegate void IngotCraftedEventHandler();

	private float healTimer = 0f;
	private bool isHealing = false;

	protected override void OnReady()
	{
	}
	
	public override void _Process(double delta)
	{
		base._Process(delta);
		
		if(isHealing){
			healTimer += (float)delta;

			if(healTimer >= healInterval )
			{
				healTimer = 0f;
				HealPlayersInAura();
				GetTree().CreateTimer(5f).Timeout += () => isHealing = false;
				
			}
		}
	}

	protected override void OnCraftingRequirementsMet()
	{
		isHealing = true;
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

    protected override void ProduceOutput()
    {
        base.ProduceOutput();
		//EmitSignal(SignalName.IngotCrafted);
    }

}

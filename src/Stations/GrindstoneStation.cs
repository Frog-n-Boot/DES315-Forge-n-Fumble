using Godot;
using System;
using System.Collections.Generic;

public partial class GrindstoneStation : BaseStationScript
{
	[Export] public PackedScene swordScene {get; private set;}
	[Export] private AudioStreamPlayer3D grindstoneSound;
	[Export] private BarMinigame barMinigame;
	private Sword depositedSowrd;
	[Signal] public delegate void SwordCraftedEventHandler();

	private PlayerController activePlayer = null;
	

	protected override void OnReady()
	{
	   	if(swordScene == null)
		{
			swordScene = GD.Load<PackedScene>("res://assets/models/MeltedIngot.tscn");	
		}
		barMinigame.MinigameCompleted += OnMinigameCompleted;
		barMinigame.MinigameFailed += OnMinigameFailed;
		barMinigame.canvasLayer.Hide();

	}

	
	protected override string GetStationName() => "Grindstone";

    protected override bool IsStationBusy() => depositedSowrd != null;

	public bool DepositSword(Sword sword)
	{
		GD.Print($"Deposit sword called, sword in group: {sword.IsInGroup("DullSword")}");
		if(!sword.IsInGroup("DullSword")) return false;
		if(depositedSowrd != null) return false;
		if(sword.sharpenedVersion == null) return false;
		if(IsStationBusy()) return false;
		
		//if(activePlayer != null && activePlayer != player) return false; 
		
		player.DisconnectWeapon(sword);
		depositedSowrd = sword;
		//activePlayer = player;
		
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
		barMinigame.Show();
		barMinigame.Start(player);
		//craftingTimer.Start();
		grindstoneSound.Play();
		
	}

	private void OnMinigameCompleted()
	{
		barMinigame.canvasLayer.Hide();
		//EmitSignal(SignalName.SwordCrafted);
		GD.Print("Grindstone Signal Emiited");
		ProduceOutput();
		CheckForWaitingPlayer();
	}

	private void OnMinigameFailed()
	{
		GD.Print("Minigame Failed");
		barMinigame.canvasLayer.Hide();

		if(depositedSowrd == null) {GD.PrintErr("DepositedSword is null"); return; }
		if(player == null) {GD.PrintErr("Player is null"); return; }

		var sword = depositedSowrd;
		var owningPlayer = player;
		depositedSowrd = null;
		pendingRecipe = null;

		GD.Print($"Sword: {sword.Name}, parent: {sword.GetParent()?.Name}");

			GetTree().CreateTimer(0.1f).Timeout += () =>
			{
				if(!IsInstanceValid(sword)) return;
				sword.Position = Vector3.Zero;
				sword.Rotation = Vector3.Zero;
				owningPlayer.GiveWeapon(sword);
			};
		CheckForWaitingPlayer();
		
		
	}

	private void CheckForWaitingPlayer()
	{
		if(player != null && !playersInZone.Contains(player))
		{
			player = playersInZone.Count > 0 ? playersInZone[0] : null;
			itemCarrier = player as ItemCarrier;
		}
	}

	public float GetCraftDuration()=> craftDuration;
	public override BarMinigame GetBarMinigame() => barMinigame;

}

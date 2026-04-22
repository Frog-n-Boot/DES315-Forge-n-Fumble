using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ForgingStation : BaseStationScript
{
	[Export] public PackedScene dullSwordScene {get; private set;}
	[Export] private SequenceMinigame sequenceMinigame;

	private bool sessionActive = false;
	protected override void OnReady()
	{
		
		sequenceMinigame.SequenceCompleted += OnSequenceCompleted;
		sequenceMinigame.SequenceFailed += OnSequenceFailed;

	   	if(dullSwordScene == null)
		{
			dullSwordScene = GD.Load<PackedScene>("res://assets/models/DullSword.tscn");
		}
	}

    protected override bool IsStationBusy() => sessionActive;

	private int GetSequenceLength()
	{
		//int playerCount = GetTree().GetNodesInGroup("Player").Count;
		return 5;
	}

	private void OnSequenceCompleted()
	{
		sessionActive = false;
		base.ConsumeItems();
		ProduceOutput();
		CheckForWaitingPlayer();
	}

	private void OnSequenceFailed()
	{
		GD.Print("Sequence Failed");
		sessionActive = false;

		if(pendingConsume == null) return;
		GD.Print($"Items to return {pendingConsume.Count}");

		var owningPlayer  = player;

		foreach(var item in pendingConsume)
		{
			GD.Print($"Returning item: {item.name}, scene: {item.itemScene}");
			if(item.itemScene != null)
			{
				var pickable = item.itemScene.Instantiate<Pickable>();
				GetTree().Root.AddChild(pickable);
				pickable.GlobalPosition = owningPlayer.GlobalPosition;
				owningPlayer.GiveItem(pickable);
				GD.Print("Item returned to player");
			}
			else
			{
				GD.Print("Item Scene is null");
			}
			itemsToDeposit.Remove(itemsToDeposit.FirstOrDefault(i => i.name == item.name));

		}

		pendingConsume = null;
		pendingRecipe = null;
		
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
	
	protected override void OnCraftingRequirementsMet()
	{
		sessionActive = true;
		sequenceMinigame.Start(GetSequenceLength(), player);
	}

	protected override string GetStationName() => "Anvil";

	public override SequenceMinigame GetSequenceMinigame() => sequenceMinigame;
	protected override void ConsumeItems() {}
   
}

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ForgingStation : BaseStationScript
{
	[Export] public PackedScene dullSwordScene {get; private set;}
	[Export] private SequenceMinigame sequenceMinigame;
	private List<PlayerController> playersInZone = new List<PlayerController>();

	protected override void OnReady()
	{
		
		sequenceMinigame.SequenceCompleted += OnSequenceCompleted;
		sequenceMinigame.SequenceFailed += OnSequenceFailed;

	   	if(dullSwordScene == null)
		{
			dullSwordScene = GD.Load<PackedScene>("res://assets/models/DullSword.tscn");
		}
	}
	private int GetSequenceLength()
	{
		//int playerCount = GetTree().GetNodesInGroup("Player").Count;
		return 5;
	}

	private void OnSequenceCompleted()
	{
		base.ConsumeItems();
		ProduceOutput();
	}

	private void OnSequenceFailed()
	{
		GD.Print("Sequence Failed");
		if(pendingConsume == null) return;
		GD.Print($"Items to return {pendingConsume.Count}");

		foreach(var item in pendingConsume)
		{
			GD.Print($"Returning item: {item.name}, scene: {item.itemScene}");
			if(item.itemScene != null)
			{
				var pickable = item.itemScene.Instantiate<Pickable>();
				GetTree().Root.AddChild(pickable);
				pickable.GlobalPosition = player.GlobalPosition;
				player.GiveItem(pickable);
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
		
	}

	protected override void OnCraftingRequirementsMet()
	{
		sequenceMinigame.Start(GetSequenceLength(), player);
	}

	protected override string GetStationName() => "Anvil";

	public override SequenceMinigame GetSequenceMinigame() => sequenceMinigame;
	protected override void ConsumeItems() {}
   
}

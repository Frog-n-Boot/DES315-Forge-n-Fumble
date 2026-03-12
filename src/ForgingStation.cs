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
		pendingConsume = null;
		pendingRecipe = null;
	}

	protected override void OnCraftingRequirementsMet()
	{
		sequenceMinigame.Start(GetSequenceLength(), player);
	}
	protected override void OnInputBodyEntered(Node3D body)
	{
		if (body.IsInGroup("Player"))
		{
			var p= body as PlayerController;
			itemCarrier = p as ItemCarrier;
			player = p;
			playersInZone.Add(p);
			p.SetCurrentStation(this);
		}
	}

	protected override void OnInputBodyExited(Node3D body)
	{
		if(body.IsInGroup("Player")){
			var p = body as PlayerController;
			playersInZone.Remove(p);
			p?.SetCurrentStation(null);
		}
	}


	protected override string GetStationName() => "Anvil";

	public override SequenceMinigame GetSequenceMinigame() => sequenceMinigame;




}

using Godot;
using System;
using System.Collections.Generic;

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
		if(itemCarrier == null) return;

		if(GetRequiredItems(out var items, out var recipe))
		{
			foreach(var item in items)
				itemCarrier.RemoveItem(item);

			pendingRecipe = recipe;
			ProduceOutput();

		}
	}

	private void OnSequenceFailed()
	{
		var random = new Random();
		foreach(var hand in new[] { player.GetNode<Node3D>("CollisionShape3D/LeftHand"), player.GetNode<Node3D>("CollisionShape3D/RightHand")}){
			if(hand.GetChildCount() == 0) continue;

			var pickable = hand.GetChild(0) as Node3D;
			if(pickable == null) continue;

			//pickable.Reparent(GetTree().Root);

			//Vector3 randomDir = new Vector3((float) random.NextDouble() * 2 - 1, 1f, (float)random.NextDouble() * 2 - 1).Normalized();
			//pickable.GlobalPosition = GlobalPosition + randomDir;
			//pickable.GetNode<CollisionShape3D>("CollisionShape3D").SetDeferred("disabled", false);
		}
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

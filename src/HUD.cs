using Godot;
using System;

public partial class HUD : CanvasLayer
{
	[Export] private PackedScene portraitScene;
	[Export] private PlayerSpawner spawner;
	[Export] private HBoxContainer portraitContainer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		spawner.PlayerSpawned += OnPlayerSpawned;
	}

	private void OnPlayerSpawned(PlayerController player, int playerIndex)
	{
		var portrait = portraitScene.Instantiate<PlayerPortrait>();
		portraitContainer.AddChild(portrait);
		portrait.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		portrait.Init(player);
	}
}

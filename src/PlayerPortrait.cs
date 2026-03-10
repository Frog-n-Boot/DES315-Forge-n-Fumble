using Godot;
using System;

public partial class PlayerPortrait : PanelContainer
{

	private PlayerController player;

	public void Init(PlayerController player)
	{
		this.player = player;
		GetNode<TextureRect>("HBoxContainer/TextureRect").Texture = player.GetPortraitTexture();
		GetNode<Label>("HBoxContainer/TextureRect/VBoxContainer/Label").Text = $"Player {player.PlayerIndex + 1}";

		GetNode<HealthBar>("HBoxContainer/TextureRect/VBoxContainer/ProgressBar").InitForPlayer(player);
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(player== null || !IsInstanceValid(player)) return;
		ProgressBar progressBar = GetNode<ProgressBar>("HBoxContainer/TextureRect/VBoxContainer/WeaponDurabilityProgressBar");
		progressBar.MinValue = 0;
		progressBar.MaxValue = 30;

		if(player.currentSword != null)
			progressBar.Value = player.currentSword.durability;
		else
			progressBar.Value = 0;

		//GetNode<ProgressBar>("HBoxContainer/TextureRect/VBoxContainer/PowerUpProgressBa")
	}
}

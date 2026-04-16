using Godot;
using System;
using System.Linq;

public partial class PlayerPortrait : PanelContainer
{

	private PlayerController player;
	[Export] private TextureRect itemTextureRect;
	[Export] private TextureProgressBar weaponDurabilityBar;
	[Export] private HealthBar healthBar;
	[Export] public Texture2D[] playerTextures;
	[Export] public Texture2D[] playerLabel;
	public override void _Ready()
	{
		if(weaponDurabilityBar == null) weaponDurabilityBar = GetNode<TextureProgressBar>("Control/TextureRect/WeaponDurabilityProgressBar/TextureProgressBar");
		if(healthBar == null) healthBar = GetNode<HealthBar>("Control/TextureRect/TextureProgressBar");
		weaponDurabilityBar.MinValue = 0;
	}

	public void Init(PlayerController player)
	{
		
		this.player = player;
		GetNode<TextureRect>("Control/PlayerTexture").Texture = playerTextures[player.PlayerIndex];
		GetNode<TextureRect>("Control/PlayerLabel").Texture = playerLabel[player.PlayerIndex];
		GetNode<Label>("Control/PlayerTexture/Label").Text = $"Player {player.PlayerIndex + 1}";

		if(healthBar != null)
		{
			healthBar.InitForPlayer(player);
			
		}
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(player== null || !IsInstanceValid(player)) return;

		if(player.currentWeapon != null)
		{
			weaponDurabilityBar.TextureProgress = player.currentWeapon.weaponTexture;
			weaponDurabilityBar.TintProgress = player.currentWeapon.weaponTint;
			weaponDurabilityBar.MaxValue = player.currentWeapon.maxDurability;	
			weaponDurabilityBar.Value = player.currentWeapon.durability;
		}
			
		else
			weaponDurabilityBar.Value = 0;
		
		ItemData item = player.GetCarriedItems().FirstOrDefault();
		if(item != null)
		{
			itemTextureRect.Show();		
			itemTextureRect.Texture = item.itemTexture;
		}
		else
			itemTextureRect.Hide();
		
		//GetNode<ProgressBar>("HBoxContainer/TextureRect/VBoxContainer/PowerUpProgressBa")
	}
}

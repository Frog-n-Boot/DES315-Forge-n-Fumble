using Godot;
using System;

public partial class PlayerPortrait : PanelContainer
{

	private PlayerController player;
	private ProgressBar weaponDurabilityBar;
	private HealthBar healthBar;

    public override void _Ready()
    {
		weaponDurabilityBar = GetNode<ProgressBar>("HBoxContainer/TextureRect/VBoxContainer/WeaponDurabilityProgressBar");
		healthBar = GetNode<HealthBar>("HBoxContainer/TextureRect/TextureProgressBar");
		weaponDurabilityBar.MinValue = 0;
    }

	public void Init(PlayerController player)
	{
		
		this.player = player;
		GetNode<TextureRect>("HBoxContainer/TextureRect").Texture = player.GetPortraitTexture();
		GetNode<Label>("HBoxContainer/TextureRect/VBoxContainer/Label").Text = $"Player {player.PlayerIndex + 1}";

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
            weaponDurabilityBar.MaxValue = player.currentWeapon.maxDurability;
			weaponDurabilityBar.Value = player.currentWeapon.durability;
        }
			
		else
			weaponDurabilityBar.Value = 0;

		//GetNode<ProgressBar>("HBoxContainer/TextureRect/VBoxContainer/PowerUpProgressBa")
	}
}

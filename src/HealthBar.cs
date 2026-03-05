using Godot;
using System;

public partial class HealthBar : ProgressBar
{
	[Export] private NodePath targetPath;
    [Export] private bool isForge = true;
	[Export] private bool isEnemy = false;

    private Forge forge;
    private PlayerController playerController;
	private EnemyController enemy;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	
		base._Ready();
		if(isForge){
            forge = GetNode<Forge>("/root/Forge");
            forge.ForgeTookDamage += SetHealth;
            SetHealth(forge.health, forge.maxHealth);
		}
		else if(isEnemy){
			enemy = GetNode<EnemyController>($"../../../");
			enemy.EnemyHealthChanged += SetHealth;
			SetHealth(enemy.currentHealth, enemy.MaxHealth);
		}	
	
		MaxValue = 100;
		Value = 100;
	}

	public void InitForPlayer(PlayerController player)
	{
		playerController = player;
		MaxValue = player.maxHealth;
		Value = player.health;
		player.PlayerHealthChanged += SetHealth;
	}

	  private void SetHealth( int current, int max)
    {
        Value = (float)current / (float)max * 100;
    }

    public override void _ExitTree(){
        if(forge != null)
            forge.ForgeTookDamage -= SetHealth;
        
        if(playerController != null)
            playerController.PlayerHealthChanged -= SetHealth;
        
    }
}

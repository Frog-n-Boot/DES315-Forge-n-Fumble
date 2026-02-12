using Godot;
using System;

public partial class HealthBar : ProgressBar
{
	[Export] private NodePath targetPath;
    [Export] private bool isForge = true;
	[Export] private bool isEnemy = false;

    private Forge forge;
    private PlayerController playerController;
	private Enemy enemy;

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
			enemy = GetNode<Enemy>($"../../../");
			enemy.EnemyHealthChanged += SetHealth;
			SetHealth(enemy.health, 100);
		}
		else{
			playerController = GetNode<PlayerController>($"../../../");
			playerController.PlayerHealthChanged += SetHealth;
			SetHealth(playerController.health, playerController.maxHealth);
		}
		
		
	
		MaxValue = 100;
		Value = 100;
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

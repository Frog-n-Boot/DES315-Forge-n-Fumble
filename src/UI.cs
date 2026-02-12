using Godot;
using System;
using System.Transactions;



public partial class UI : ProgressBar
{
    Forge forge;
    public override void _Ready()
    {
        MaxValue = 100;
        forge = GetNode<Forge>("/root/Forge");

        forge.ForgeTookDamage += SetHealth;

        SetHealth(forge.health, forge.maxHealth);

    }
    
    private void SetHealth( int current, int max)
    {
        Value = (float)current / (float)max * 100;
    }


}

public partial class playerUI : ProgressBar
{
    PlayerController playerController;
    public override void _Ready()
    {
        MaxValue = 100;

        playerController = GetNode<PlayerController>("/root/PlayerController");
        playerController.PlayerHealthChanged += playerSetHealth;

        playerSetHealth(playerController.health, playerController.maxHealth);
    }

    private void playerSetHealth(int current, int max)
    {
        Value = (float)current / (float)max * 100;
    }
}

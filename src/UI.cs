using Godot;
using System;
using System.Transactions;



public partial class UI : ProgressBar
{
    [Export] private NodePath targetPath;
    [Export] private bool isForge = true;

    private Forge forge;
    private PlayerController playerController;

    public override void _Ready()
    {
        MaxValue = 100;
        if(isForge){
            forge = GetNode<Forge>("/root/Forge");
            forge.ForgeTookDamage += SetHealth;
            SetHealth(forge.health, forge.maxHealth);
        }
        else{
            playerController = GetNode<PlayerController>("res://src/PlayerController.cs");
            playerController.PlayerHealthChanged += SetHealth;
            SetHealth(playerController.health, playerController.maxHealth);
        }
        

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


using Godot;
using System;

public partial class Forge : Node3D
{
	#region Variables
	public int maxHealth = 100;
	public int health;

	[Signal] public delegate void ForgeTookDamageEventHandler(int current, int max);
    [Signal] public delegate void ForgeHealedEventHandler(int current, int max);

	private int tick = 0;
	#endregion

	#region Ready
	public override void _Ready()
	{
		
		health = maxHealth;
	}
	#endregion

	#region Process
	public override void _Process(double delta)
	{
		tick += 1;
		if(tick % 10 == 0)
        {
            if(health <= 0)
			{
				Destroyed();
				GetTree().Quit();
			}
        }

	}
	#endregion

	#region HealForge
	public void HealForge(int health)
	{
		this.health += health;
		EmitSignal(SignalName.ForgeTookDamage, this.health, maxHealth);
    }
	#endregion

	#region TakeDamage
	public void TakeDamage(int damage)
	{
		this.health -= damage;
		EmitSignal(SignalName.ForgeTookDamage, health, maxHealth);
	}
	#endregion

	#region Destroyed
	private void Destroyed()
	{
		QueueFree();
	}
	#endregion
}

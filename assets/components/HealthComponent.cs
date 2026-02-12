using Godot;
using System;

public partial class HealthComponent : Node
{
	[Signal] public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);
	[Signal] public delegate void DamagedEventHandler(int amount);
	[Signal] public delegate void DiedEventHandler();

	[Export] public int maxHealth {get; set; } = 100;

	private int currentHealth;
	public int CurrentHealth{
        get => currentHealth;
        set
        {
            int oldHealth = currentHealth;
			currentHealth = Mathf.Clamp(value, 0, maxHealth);

			if(oldHealth != currentHealth)
            {
                EmitSignal(SignalName.HealthChanged, currentHealth, maxHealth);

				if(currentHealth <= 0)
                {
                    EmitSignal(SignalName.Died);
                }
            }
        }
    }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
        currentHealth = maxHealth;
    }

	public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
		EmitSignal(SignalName.Damaged, amount);
		CurrentHealth -= amount;
    }

	public void Heal(int amount)
    {
        if(amount <= 0) return;
		CurrentHealth += amount;
    }

	public bool IsAlive() => CurrentHealth > 0;
	public float GetHealthPercent() => (float)CurrentHealth / maxHealth;
}

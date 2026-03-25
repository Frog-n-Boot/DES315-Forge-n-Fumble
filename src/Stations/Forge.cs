using Godot;
using System;

public partial class Forge : Node3D
{
	#region Variables
	public int maxHealth = 100;
	public int health;

	[Signal] public delegate void ForgeTookDamageEventHandler(int current, int max);
	[Signal] public delegate void ForgeHealedEventHandler(int current, int max);
	[Signal] public delegate void ForgeDestroyedEventHandler();

	private int tick = 0;

	[Export] ResultMenu resultMenu;
	private bool isDestroyed = false;

	#endregion

	#region Ready
	public override void _Ready()
	{
		health = maxHealth;
		GetTree().SceneChanged += OnSceneChanged;
		
	}

	private void OnSceneChanged()
    {
		health = maxHealth;

        CallDeferred(nameof(OnHealthChanged));
    }
	#endregion

	#region Process
	public override void _Process(double delta)
	{
		tick += 1;
		if(tick % 10 == 0)
		{
			if(health <= 0 && !isDestroyed)
			{
				isDestroyed = true;
				Destroyed();
				EmitSignal(SignalName.ForgeDestroyed);

				var resultMenu = GetTree().Root.FindChild("ResultsMenu", true, false) as ResultMenu;
				if(resultMenu == null)
					GD.PrintErr("Reult Menu not found");
				else
					resultMenu.ShowFail();
			}
			else if(health >= 0 && isDestroyed)
            {
                isDestroyed = false;
            }
		}

	}
	#endregion

	#region HealForge
	public void HealForge(int health)
	{
		this.health += health;
		EmitSignal(SignalName.ForgeHealed, this.health, maxHealth);
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
		//QueueFree();
	}
	#endregion

	public void SetForgeHealth(int newHealth, int newMaxHealth)
	{
		maxHealth = newMaxHealth;
		health = newHealth;
		EmitSignal(SignalName.ForgeTookDamage,health, maxHealth);
	}
	private void OnHealthChanged()
    {
        EmitSignal(SignalName.ForgeTookDamage, health, maxHealth);
    }

}

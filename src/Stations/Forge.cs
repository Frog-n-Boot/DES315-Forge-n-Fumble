using Godot;
using System;

public partial class Forge : Node3D
{
	#region Variables
	public int maxHealth = 100;
	public int health;

	private int[] shockwaeThresholds = {75, 50, 25};
	private float shockwaveRadius = 10f;
	private float shockwaveForce = 20f;
	private int shockwaveIndex = 0;
	private float healCooldown = 1f;
	private float healAmount;
	private bool canHeal;
	
	private Node3D forgeNode;

	private float pushCheckTimer = 0f;

	[Signal] public delegate void ForgeTookDamageEventHandler(int current, int max);
	[Signal] public delegate void ForgeHealedEventHandler(int current, int max);
	[Signal] public delegate void ForgeDestroyedEventHandler();

	private int tick = 0;

	ResultMenu resultMenu;
	private bool isDestroyed = false;

	#endregion

	#region Ready
	public override void _Ready()
	{
		
		health = maxHealth;
		shockwaveIndex = 0;
		GetTree().SceneChanged += OnSceneChanged;
		
	}

	private void OnSceneChanged()
	{
		if(GetTree().CurrentScene.Name == "Scene")
		{
			health = maxHealth;
			shockwaveIndex = 0;
			canHeal = false;
			healCooldown = 1f;
			healAmount = 0f;
			isDestroyed = false;

			CallDeferred(nameof(OnHealthChanged));
		}
		
	}

	#endregion

	#region Process
	public override void _Process(double delta)
	{
		if(health <= 0 || isDestroyed || health >= maxHealth) return;

		if (!canHeal)
		{
			healCooldown -= (float)delta;
			if(healCooldown <= 0)
				canHeal = true;
		}
		else
		{
			healAmount += 1f / 5f * (float)delta;

			if(healAmount >= 1f)
			{
				int toHeal = (int)healAmount;
				healAmount -= toHeal;
				HealForge(toHeal);
			}
		}

	}
	#endregion

	#region HealForge
	public void HealForge(int health)
	{
		this.health += health;
		this.health = Math.Min(this.health, maxHealth);
		EmitSignal(SignalName.ForgeHealed, this.health, maxHealth);
	}
	#endregion

	#region TakeDamage
	public void TakeDamage(int damage)
	{
		if(isDestroyed) return;
		health -= damage;
		canHeal = false;
		healCooldown = 5;
		healAmount = 0f;
		EmitSignal(SignalName.ForgeTookDamage, health, maxHealth);
		CheckShockwave();

		if(health <= 0)
		{
			isDestroyed = true;
			Destroyed();
			EmitSignal(SignalName.ForgeDestroyed);

			resultMenu = GetTree().Root.FindChild("ResultsMenu", true, false) as ResultMenu;
			if(resultMenu == null)
				GD.PrintErr("Reult Menu not found");
			else
				resultMenu.ShowFail();
		}
	}

	private void CheckShockwave()
	{
		if(shockwaveIndex >= shockwaeThresholds.Length) return;

		int healthPercent = (int)((float)health/maxHealth *100);
		if(healthPercent <= shockwaeThresholds[shockwaveIndex]){
			shockwaveIndex++;
			FireShockwave();
		}
	}

	private void PushNearbyEnemies(double delta)
	{
		pushCheckTimer -= (float)delta;
		if(pushCheckTimer > 0) return;

		pushCheckTimer = 0.1f;
		if(forgeNode == null)
		{
			forgeNode = GetTree().Root.GetNode<SmeltingStation>("Scene/NavigationRegion3D/Forge");
			if(forgeNode == null) return;
		}

		foreach(Node n in GetTree().GetNodesInGroup("Enemy")){
			if(n is EnemyController enemy)
			{
				float distance = enemy.GlobalPosition.DistanceTo(forgeNode.GlobalPosition);

				if(distance <= 2.5f)
				{
					Vector3 pushDir = (enemy.GlobalPosition - forgeNode.GlobalPosition).Normalized();
					pushDir.Y = 0;
					enemy.ApplyKnockback(pushDir, 15f);
				}
			}
		}
	}
	private void FireShockwave()
	{
		forgeNode = GetTree().Root.GetNode<SmeltingStation>("Scene/NavigationRegion3D/Forge");
		if(forgeNode == null) return;

		foreach(Node n in GetTree().GetNodesInGroup("Enemy"))
		{
			if(n is EnemyController enemy)
			{
				GD.Print("Fire Shockwave");
				float distance = enemy.GlobalPosition.DistanceTo(forgeNode.GlobalPosition);
				if(distance <= shockwaveRadius)
				{
					Vector3 pushDir = (enemy.GlobalPosition - forgeNode.GlobalPosition);
					pushDir.Y = 0;
					enemy.ApplyKnockback(pushDir, shockwaveForce);
				}
			}
		}
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
	public void ResetHealth()
	{
		health = maxHealth;
		shockwaveIndex = 0;
		canHeal = false;
		healCooldown = 1f;
		healAmount = 0f;
		isDestroyed = false;

		EmitSignal(SignalName.ForgeHealed, health, maxHealth);
	}

}

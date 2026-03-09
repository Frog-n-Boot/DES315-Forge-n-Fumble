using Godot;
using System;

public abstract partial class BaseWeapon : Node3D
{
	[Export] public int damage;
	[Export] public int maxDurability;
	public int durability;
	public bool isBeingPickedUp = false;

	[Signal] public delegate void BrokeEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		durability = maxDurability;
		OnReady();
	}

	protected virtual void OnReady() {}

	public virtual void TakeDurabilityDamage(int amount)
	{
		durability -= amount;
		//EmitSignal(SignalName.DurabilityChanged, durability, maxDurability);

		if(durability <= 0)
		{
			durability = 0;
			OnBroke();
		}
	}

	protected virtual void OnBroke()
	{
		EmitSignal(SignalName.Broke);
	}

	public abstract void Use();
}

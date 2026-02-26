using Godot;
using System;

[Tool]
public partial class Sword : Node3D
{
	[Export] public int damage;
	[Export] public int durability;

	[Signal] public delegate void CheckDurabilityEventHandler();
	[Signal] public delegate void BrokeEventHandler();

	public bool isBeingPickedUp = false;

	private CollisionShape3D hitbox;

	public override void _Ready()
	{
		hitbox = GetNodeOrNull<CollisionShape3D>("StaticBody3D/CollisionShape3D");

		if (hitbox == null)
			GD.PrintErr("Sword: CollisionShape3D not found at StaticBody3D/CollisionShape3D");
			
		SetHitboxEnabled(false);
	}

	public void SetHitboxEnabled(bool enabled)
	{
		if (hitbox != null)
			hitbox.Disabled = !enabled;
	}

	private void DestroyWeapon()
	{
		if (durability <= 0)
		{
			EmitSignal(SignalName.Broke);
			QueueFree();
		}
	}

	public void DamageWeapon(int durability)
	{
		this.durability = this.durability - durability;
		EmitSignal(SignalName.CheckDurability);
		DestroyWeapon();
	}
}

using Godot;
using System;

[Tool]
public partial class Sword : Node3D
{
	[Export] public int damage;
	[Export] public int durability;


	[Signal] public delegate void CheckDurabilityEventHandler();

	private void DestroyWeapon()
	{
		if(durability <= 0)
		{
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

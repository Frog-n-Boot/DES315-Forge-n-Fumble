using Godot;
using System;

public abstract partial class MeleeWeapon : BaseWeapon
{
	//[Export] public float attackRange = 2.0f;
	[Export] public float attackCooldown = 0.5f;
	[Export] private AudioStreamPlayer3D audio;
	

	protected bool canAttack = true;
	protected CollisionShape3D hitbox;

	protected override void OnReady()
	{
		base.OnReady();
		hitbox = GetNodeOrNull<CollisionShape3D>("StaticBody3D/CollisionShape3D");
		if(hitbox == null)
			GD.PrintErr("MeleeWeapon: CollisionShape3D not found at StaticBody3D/CollisionShape");
		SetHitboxEnabled(false);
		
	}

	public override void Use()
	{
	}

	protected virtual void Attack()
	{
		
		SetHitboxEnabled(true);
		canAttack = false;
		
		GetTree().CreateTimer(attackCooldown).Timeout += () =>
		{
			SetHitboxEnabled(false);
			canAttack = true;
		};
	}

	public void SetHitboxEnabled(bool enable)
	{
		if(hitbox !=null)
			hitbox.Disabled = !enable;
	}

	protected override void OnBroke()
	{
		EmitSignal(SignalName.Broke);
		audio.Play();
		GetTree().CreateTimer(0.5f).Timeout += () =>
		{
			QueueFree();
		};
		
	}
}

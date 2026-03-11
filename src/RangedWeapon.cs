using Godot;
using System;

public abstract partial class RangedWeapon : BaseWeapon
{
	[Export] public float fireRate = 1.0f;
	[Export] protected PackedScene projectileScene;

	protected Node3D firePoint;
	protected bool canFire = true;

    protected override void OnReady()
    {
        firePoint = GetNodeOrNull<Node3D>("FirePoint");
		if(firePoint == null)
			GD.PrintErr("RangedWeapon: FirePoint not found");
    }

    public override void Use()
    {
        if(!canFire || projectileScene == null) return;
		Fire();
    }

	protected virtual void Fire()
	{
		if(firePoint == null) return;

		var projectile = projectileScene.Instantiate<Node3D>();
		GetTree().Root.AddChild(projectile);
		projectile.GlobalPosition = firePoint.GlobalPosition;

		canFire = false;
		GetTree().CreateTimer(fireRate).Timeout += () => canFire = true;
	}
	protected override void OnBroke()
    {
		EmitSignal(SignalName.Broke);
		QueueFree();
    }
}

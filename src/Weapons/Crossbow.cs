using Godot;
using System;

public partial class Crossbow : RangedWeapon
{
	[Export] public MeshInstance3D shootingDirectionMesh;
	protected override void OnReady()
	{
		base.OnReady();
	}

	public void Shoot()
	{
		if(firePoint == null || projectileScene == null || canFire == false) return;

		var arrow = projectileScene.Instantiate<Arrow>();
		GetTree().Root.AddChild(arrow);
		arrow.GlobalPosition = firePoint.GlobalPosition;

		Vector3 direction = firePoint.GlobalBasis.Z;

		arrow.Initialize(direction, 2);

		canFire = false;
		TakeDurabilityDamage(1);

		GetTree().CreateTimer(fireRate).Timeout += () => canFire = true;
	
	}

	// public void StartDraw()
	// {

	// 	if(!canFire) return;
	// 	isDrawing = true;
	// 	drawProgress = 0f;
	// }

	// public void Release()
	// {
	// 	if(!isDrawing) return;
	// 	isDrawing = false;

	// 	float power = Mathf.Clamp(drawProgress / drawTime, 0.0f, 1f);
	// 	PowerShot(power);
	// }

    // public override void _Process(double delta)
    // {
		
        
    // }

	// private void PowerShot(float power)
	// {
	// 	if(firePoint == null || projectileScene == null) return;

	// 	var arrow = projectileScene.Instantiate<Arrow>();
	// 	GetTree().Root.AddChild(arrow);
	// 	arrow.GlobalPosition = firePoint.GlobalPosition;

	// 	Vector3 direction = firePoint.GlobalBasis.Z;

	// 	arrow.Initialize(direction, power);

	// 	canFire = false;
	// 	GetTree().CreateTimer(fireRate).Timeout += () => canFire = true;

	// 	TakeDurabilityDamage(1);
	// }

}

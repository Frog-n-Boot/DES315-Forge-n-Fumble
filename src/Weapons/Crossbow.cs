using Godot;
using System;

public partial class Crossbow : RangedWeapon
{
	[Export] public MeshInstance3D shootingDirectionMesh;
	[Export] public int maxArrow;
	public Ballista sourceBallista = null;

	public TripleShot tripleShot;
	public int currentAmmo = 0;
	protected override void OnReady()
	{
		base.OnReady();
		currentAmmo = maxArrow;
		tripleShot = GetNodeOrNull<TripleShot>("TripleShot");
		if(tripleShot == null) GD.PrintErr("Crossbow: Tripleshot node not found");
	}

    protected override void Fire()
    {
		
        if(firePoint == null || projectileScene == null || canFire == false) return;
		if(currentAmmo <=0) return;

		var arrow = projectileScene.Instantiate<Arrow>();
		GetTree().Root.AddChild(arrow);
		arrow.GlobalPosition = firePoint.GlobalPosition;

		Vector3 direction = firePoint.GlobalBasis.Z;

		arrow.Initialize(direction, 2);
		canFire = false;
		currentAmmo--;
		//TakeDurabilityDamage(1);

		GetTree().CreateTimer(fireRate).Timeout += () =>
		{
			canFire = true;
			EmitSignal(SignalName.AttackFinished);
		};
    }

	public void Reload(int amount)
	{
		currentAmmo = Mathf.Min(currentAmmo + amount, maxArrow);
		GD.Print($"Reloaded ammo: {currentAmmo}/{maxArrow}");
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

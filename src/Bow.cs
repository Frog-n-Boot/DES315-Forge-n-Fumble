using Godot;
using System;

public partial class Bow : RangedWeapon
{
	[Export] public float drawTime = 0.2f;

	private bool isDrawing = false;
	private float drawProgress = 0f;

	protected override void OnReady()
	{
		base.OnReady();
	}

	public void StartDraw()
	{

		if(!canFire) return;
		isDrawing = true;
		drawProgress = 0f;
	}

	public void Release()
	{
		if(!isDrawing) return;
		isDrawing = false;

		float power = Mathf.Clamp(drawProgress / drawTime, 0.3f, 1f);
		PowerShot(power);
	}

    public override void _Process(double delta)
    {
        if(isDrawing)
			drawProgress += (float)delta;
    }

	private void PowerShot(float power)
	{
		if(firePoint == null || projectileScene == null) return;

		var arrow = projectileScene.Instantiate<Arrow>();
		GetTree().Root.AddChild(arrow);
		arrow.GlobalPosition = firePoint.GlobalPosition;

		Vector3 direction = firePoint.GlobalBasis.Z;

		arrow.Initialize(direction, power);

		canFire = false;
		GetTree().CreateTimer(fireRate).Timeout += () => canFire = true;

		TakeDurabilityDamage(1);
	}

}

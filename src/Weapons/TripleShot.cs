using Godot;
using System;

public partial class TripleShot : Node
{
	[Export] public int shotCount = 3;
	[Export] public float spreadAngle = 10f;
	[Export] public float cooldown = 3f;
	[Export] public float delatBetweenShots = 0.1f;

	private bool isActive = false;
	private bool isOnCooldown = false;

	public bool CanActivate() => !isActive && !isOnCooldown;
	public bool IsActive() => isActive;
	public bool IsOnCooldown() => isOnCooldown;

	public void TryActivate(Crossbow crossbow)
	{
		if(!CanActivate()) return;
		if(crossbow.currentAmmo <= 0) return;
		isActive = true;
		FireSpread(crossbow);
	}

	//Async is actually pretty cool, it lets a method pause and resume, using await will make the function wait until the condition is finished.
	private async void FireSpread(Crossbow crossbow)
	{
		for(int i = 0; i < shotCount; i++)
		{
			if(crossbow.currentAmmo <= 0) break;
			if(!IsInstanceValid(crossbow)) break;

			float angleOffSet = spreadAngle * (i - (shotCount - 1) / 2f);
			Vector3 direction = crossbow.firePoint.GlobalBasis.Z.Rotated(Vector3.Up, Mathf.DegToRad(angleOffSet));

			var arrow = crossbow.projectileScene.Instantiate<Arrow>();
			crossbow.GetTree().Root.AddChild(arrow);
			arrow.GlobalPosition = crossbow.firePoint.GlobalPosition;
			arrow.Initialize(direction, 2);

			crossbow.currentAmmo--;

			await crossbow.ToSignal(crossbow.GetTree().CreateTimer(delatBetweenShots), SceneTreeTimer.SignalName.Timeout);

		}

		isActive = false;
		isOnCooldown = true;
		GetTree().CreateTimer(cooldown).Timeout += () => isOnCooldown = false;
	}

}

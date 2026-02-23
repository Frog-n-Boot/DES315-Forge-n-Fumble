using Godot;
using System;
using System.Collections.Generic;

public partial class Turret : Node3D
{
	[Export] private Node3D bulletSpawnLocation;
	[Export] private PackedScene bulletScene;
	[Export] private float range = 20.0f;
	[Export] private float coneAngle = 45.0f;

	protected ItemCarrier itemCarrier;
	protected PlayerController player;
    private List<PlayerController> playersInZone = new List<PlayerController>();
	private int bulletCount= 2;

	private Node3D currentTarget;
	private float reloadTime = 1;
	Timer shootTimer;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		shootTimer = new Timer();
		shootTimer.WaitTime = 1.0f;
		shootTimer.OneShot= false;
		AddChild(shootTimer);
		shootTimer.Timeout += SpawnBullet;
		shootTimer.Start();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		currentTarget = FindClosestEnemy();

		if(currentTarget == null) return;

		Vector3 direction = currentTarget.GlobalPosition - GlobalPosition;
		direction.Y = 0;
		if(direction != Vector3.Zero)
		{
			float targetAngle = Mathf.Atan2(-direction.Z, direction.X);
			Rotation = new Vector3(Rotation.X, targetAngle, Rotation.Z);
		}
		//int playerCount = GetTree().GetNodesInGroup("Player").Count;
	}

	private void SpawnBullet()
	{
		if(bulletCount <= 0)
		{
			shootTimer.Stop();
			return;
		}

		Node3D target = FindClosestEnemy();
		if(target == null) return;

		var bullet = bulletScene.Instantiate<Bullet>();
		bullet.AddToGroup("Bullet");
		GetTree().Root.AddChild(bullet);
		bullet.GlobalPosition = new Vector3(bulletSpawnLocation.GlobalPosition.X, currentTarget.GlobalPosition.Y, bulletSpawnLocation.GlobalPosition.Z);

		Vector3 direction = (target.GlobalPosition - bulletSpawnLocation.GlobalPosition);
		direction.Y = 0;
		bullet.SetDirection(direction);
		bulletCount--;

		
	}

	private Node3D FindClosestEnemy()
	{
		var enemies = GetTree().GetNodesInGroup("Enemy");
		Node3D closest = null;
		float closestDistance= float.MaxValue;

		foreach(Node3D enemy in enemies)
		{
			Node3D enemy3D = enemy as Node3D;
			if(enemy3D == null) continue;

			Vector3 toEnemy = enemy3D.GlobalPosition - GlobalPosition;
			toEnemy.Y = 0;
			float distance = toEnemy.Length();

			if(distance > range) continue;

			float angle = Mathf.RadToDeg(Transform.Basis.X.AngleTo(toEnemy.Normalized()));

			if(angle <= coneAngle)
			{
				if(distance < closestDistance)
				{
					closestDistance = distance;
					closest = enemy;
				}
			}
	
		}
		return closest;
	}
	public void OnInputBodyEntered(Node3D body)
    {
        if (body.IsInGroup("Player"))
        {
            var p= body as PlayerController;
            itemCarrier = p as ItemCarrier;
            player = p;
            playersInZone.Add(p);
			CheckItem();
        }
    }
	private void CheckItem()
	{
		var items= itemCarrier.GetCarriedItems();
		foreach(var item in items)
		{
			if(item.name == "Stick")
			{
				GD.Print("Ingot was added");
				bulletCount += 10;
				shootTimer.Start();
			}
		}
	}
}

using Godot;
using System;
using System.Collections.Generic;

public partial class Turret : Node3D
{
	[Export] private Node3D bulletSpawnLocation;
	[Export] private PackedScene bulletScene;
	[Export] public float range = 20.0f;
	[Export] public float coneAngle = 65.0f;
	[Export] protected Node3D inputNode;
	[Export] protected Label label;

	protected ItemCarrier itemCarrier;
	protected PlayerController player;
	private List<PlayerController> playersInZone = new List<PlayerController>();
	private int bulletCount= 10;

	private Node3D currentTarget;
	private float reloadTime = 1;
	Timer shootTimer;

	public override void _Ready()
	{
		label.Text = $"{bulletCount}";
		SetupTimer();
		SetupArea();
	}

	private void SetupTimer()
	{
		shootTimer = new Timer();
		shootTimer.WaitTime = 1.0f;
		shootTimer.OneShot= false;
		AddChild(shootTimer);
		shootTimer.Timeout += SpawnBullet;
		shootTimer.Start();
	}

	private void SetupArea()
	{
		if(inputNode == null)
		{
			GD.Print($"{Name}: inputNode is not assigned");
			return;
		}

		var inputArea = inputNode.GetNode<Area3D>("Area3D");
		if(inputArea != null)
		{
			inputArea.BodyEntered += OnInputBodyEntered;
		}
		else
			GD.Print("Input are not found");
	}

	public override void _Process(double delta)
	{
		currentTarget = FindClosestEnemy();

		if(currentTarget == null) return;

		bulletSpawnLocation.LookAt(currentTarget.GlobalPosition, Vector3.Up);
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

		bullet.GlobalPosition = bulletSpawnLocation.GlobalPosition;

		Vector3 forward = -bulletSpawnLocation.GlobalTransform.Basis.Z;

		bullet.SetDirection(forward);
		bulletCount--;
		label.Text = $"{bulletCount}";

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
			if(item.name == "Iron_Ingot")
			{
				GD.Print("Ingot was added");
				bulletCount += 10;
				label.Text = $"{bulletCount}";
				shootTimer.Start();
				itemCarrier.RemoveItem(item);
			}
		}
	}
}

using Godot;
using System;
using System.Collections.Generic;

public partial class Ballista : Node3D
{
	[Export] private Node3D arrowSpawnPosition;
	[Export] private Node3D ballistaHead;
	[Export] private PackedScene crossbowScene;
	[Export] private Node3D ballistaBarrel;
	[Export] private PackedScene arrowScene;
	[Export] public float range = 20.0f;
	[Export] public float coneAngle = 360.0f;
	[Export] protected Node3D inputNode;
	[Export] int arrowCount= 10;

	[Export] protected Label3D label;

	[Export] AudioStreamPlayer3D audio;

	protected ItemCarrier itemCarrier;
	private List<PlayerController> playersInZone = new List<PlayerController>();
	

	private EnemyController currentTarget;

	public bool isDetached = false;
	private PlayerController interactingPlayer = null;

	Timer shootTimer;

	public override void _Ready()
	{
		label.Visible = true;
		label.Text = $"{arrowCount}";
		label.FontSize = 40;
		SetupTimer();
		SetupArea();
	}

	private void SetupTimer()
	{
		shootTimer = new Timer();
		shootTimer.WaitTime = 1.0f;
		shootTimer.OneShot= false;
		AddChild(shootTimer);
		shootTimer.Timeout += SpawnArrow;
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
			inputArea.BodyExited += OnInputBodyExited;
		}
		else
			GD.Print("Input are not found");
	}

	public override void _Process(double delta)
	{
		currentTarget = FindClosestEnemy();
		if(currentTarget != null)
		{
			Vector3 targetPos = GetEnemyCenter(currentTarget);
			ballistaHead.LookAt(targetPos, Vector3.Up);

		}

	}

	private void SpawnArrow()
	{
		if (arrowCount <= 0)
		{
			shootTimer.Stop();
			return;
		}

		EnemyController target = FindClosestEnemy();
		if (target == null) return;

		
		var arrow = arrowScene.Instantiate<Arrow>();
		arrow.AddToGroup("Arrow");
		GetTree().Root.AddChild(arrow);
		arrow.GlobalPosition = arrowSpawnPosition.GlobalPosition;

		Vector3 targetPosition = GetEnemyCenter(target);

		Vector3 direction = (targetPosition - arrowSpawnPosition.GlobalPosition).Normalized();
		arrow.Initialize(direction, 2);

		arrowCount--;
		label.Text = $"{arrowCount}";
		audio.Play();
	}

	private EnemyController FindClosestEnemy()
	{
		var enemies = GetTree().GetNodesInGroup("Enemy");
		EnemyController closest = null;
		float closestDist = float.MaxValue;

		foreach (Node node in enemies)
		{
			if (node is not EnemyController enemy) continue;

			Vector3 toEnemy = enemy.GlobalPosition - GlobalPosition;
			toEnemy.Y = 0;
			float distance = toEnemy.Length();
			
		   	if (distance > range) continue;

			float angle = Mathf.RadToDeg(-Transform.Basis.Z.AngleTo(toEnemy.Normalized()));
			if (angle <= coneAngle && distance < closestDist)
			{
				closestDist = distance;
				closest     = enemy;
			}
		}

		return closest;
	}

	private Vector3 GetEnemyCenter(EnemyController enemy)
	{
		var collisionShape = enemy.FindChild("CollisionShape3D") as CollisionShape3D;
		if(collisionShape != null) return collisionShape.GlobalPosition;

		return enemy.GlobalPosition + Vector3.Up * 1.0f;
	}
	private void AimAtTarget(Vector3 targetPos)
	{
		Vector3 toTarget = targetPos - arrowSpawnPosition.GlobalPosition;

		Vector3 horizontalDir = new Vector3(toTarget.X, 0, toTarget.Z);
		if(horizontalDir.LengthSquared() > 0.001f)
		{
			float yaw = Mathf.Atan2(horizontalDir.X, horizontalDir.Z);
			ballistaHead.Rotation = new Vector3(0, yaw, 0);
			arrowSpawnPosition.Rotation = ballistaHead.Rotation;
		}

		float horizontalDist = horizontalDir.Length();
		if(horizontalDist > 0.001f){
			float pitch = -Mathf.Atan2(toTarget.Y, horizontalDist);
			ballistaHead.Rotation = new Vector3(pitch, 0, 0);
			arrowSpawnPosition.Rotation = ballistaHead.Rotation;
		}
	}	
	
	public void OnInputBodyEntered(Node3D body)
	{
		if (body.IsInGroup("Player"))
		{
			GD.Print("Player entered input area");
			var p= body as PlayerController;
			itemCarrier = p as ItemCarrier;
			interactingPlayer = p;
			playersInZone.Add(p);
			p.SetNearbyBallista(this);
			CheckItem();
		}
	}

	public void OnInputBodyExited(Node3D body)
	{
		if(body is PlayerController p && p == interactingPlayer)
		{
			interactingPlayer = null;
			p.SetNearbyBallista(null);
		}
	}
	private void CheckItem()
	{
		var items= itemCarrier.GetCarriedItems();
		foreach(var item in items)
		{
			GD.Print($"Checking item: {item.name}");
			if(item.name == "Iron_Ingot")
			{
				GD.Print("Ingot was added");
				arrowCount += 10;
				label.Text = $"{arrowCount}";
				shootTimer.Start();
				itemCarrier.RemoveItem(item);
			}
		}
	}

	public void DetachHead(PlayerController player)
	{
		if(isDetached) return;

		isDetached = true;
		shootTimer.Stop();
		ballistaHead.Visible = false;
		
		var crossbow = crossbowScene.Instantiate<Crossbow>();
		player.GetTree().Root.AddChild(crossbow);
		crossbow.currentAmmo = arrowCount;
		crossbow.sourceBallista = this;
		arrowCount = 0;
		label.Text = "0";

		player.PickUpWeapon(crossbow);

		crossbow.SetMeta("source_ballista", this);
	}

	public void ReattachHead(Crossbow crossbow)
	{
		if(!isDetached) return;

		isDetached = false;
		arrowCount = crossbow.currentAmmo;
		label.Text = $"{arrowCount}";

		ballistaHead.Visible = true;

		if(arrowCount >0) shootTimer.Start();

		crossbow.QueueFree();
	}
}

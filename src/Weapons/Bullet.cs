using Godot;
using System;

public partial class Bullet : Node3D
{
	[Export] public int damage;
	[Export] public float speed;

	public static int defaultDamage = 10;
	public static float defaultSpeed = 20f;
	private Vector3 direction;
	
	private Node3D target;

	public void SetDirection(Vector3 direction)
	{
		this.direction = direction.Normalized();
	}
	public void SetTarget(Node3D target)
	{
		this.target = target;
	}

	public override void _Ready()
	{
		damage=  defaultDamage;
		speed = defaultSpeed;
		GetTree().CreateTimer(5.0f).Timeout += QueueFree;
	}

	public override void _Process(double delta)
	{
		GlobalPosition += direction * speed * (float)delta;
	}

	// public override void _Process(double delta)
	// {
	// 	if(target == null || !IsInstanceValid(target))
	// 	{
	// 		QueueFree();
	// 		return;
	// 	}
	// 	Vector3 direction = (target.GlobalPosition - GlobalPosition).Normalized();
	// 	GlobalPosition += direction * speed * (float)delta;
	// }
	
	public void OnCollisionDetected(Node3D body)
	{
		if (body.IsInGroup("Enemy"))
		{
			GD.Print("Enemy hit");
			EnemyController enemy = body as EnemyController;
			if(enemy == null) return;
			enemy.TakeDamage(10);
			QueueFree();
		}
	}
}

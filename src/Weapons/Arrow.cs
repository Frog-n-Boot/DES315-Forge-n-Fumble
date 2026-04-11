using Godot;
using System;

public partial class Arrow : Node3D
{
	[Export] public float speed {get; set;} =100f;
	[Export] public float damage {get; set;} =1f;
	[Export] public float lifeTime {get; set;} = 5f;
	[Export] public float gravityStrength {get; set;} = 9.8f;

	public static int defaultDamage = 10;
	public static float defaultSpeed = 20f;
	private Vector3 velocity;
	private bool hasHit = false;
	
	
	public override void _Ready()
	{
		GetTree().CreateTimer(lifeTime).Timeout += () => QueueFree();
	}

	public void Initialize(Vector3 direction, float power)
	{
		float finalSpeed = speed * power;
		velocity = direction.Normalized() * finalSpeed;

		GD.Print($"Arrow initialized with velocity: {velocity}, power: {power}, direction: {direction}");
		
		LookAt(GlobalPosition + direction, Vector3.Up);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(hasHit) return;

		GlobalPosition += velocity * (float)delta;

		if(velocity.LengthSquared() > 0.01f)
		{
			LookAt(GlobalPosition + velocity.Normalized(), Vector3.Up);
		}
		//GetTree().CreateTimer(3.0f).Timeout += () => velocity.Y -= gravityStrength * (float)delta;
	}
	public void OnCollisionDetected(Node3D body)
	{
		if(hasHit) return;

		hasHit = true;

		if (body.HasMethod("TakeDamage"))
		{
			body.Call("TakeDamage", damage);
		}

		GetTree().CreateTimer(2.0f).Timeout += () => QueueFree();
	}
}

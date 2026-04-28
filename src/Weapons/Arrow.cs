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
	private Vector3 _velocity;
	private bool hasHit = false;
	
	
	public override void _Ready()
	{
		GetTree().CreateTimer(lifeTime).Timeout += () => QueueFree();
	}

	public void Initialize(Vector3 direction, float power)
	{
		damage = defaultDamage;
		speed = defaultSpeed;

		GD.Print("Arrow initialized");
		float finalSpeed = speed * power;
		_velocity = direction.Normalized() * finalSpeed;

		GD.Print($"Arrow initialized with velocity: {_velocity}, power: {power}, direction: {direction}");
		if(direction.Normalized() != Vector3.Up && direction.Normalized() != Vector3.Down)
			LookAt(GlobalPosition + direction, Vector3.Up);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(hasHit) return;

		_velocity.Y -= gravityStrength * (float)delta;

		GlobalPosition += _velocity * (float)delta;

		if(_velocity.LengthSquared() > 0.01f)
			LookAt(GlobalPosition + _velocity.Normalized(), Vector3.Up);
		
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

using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Transactions;

public partial class PlayerController : CharacterBody3D
{
	// ========================================== Player Movement ========================================== 
	[ExportGroup("Player Movement Controls")]
	[Export]
	private float speed = 5.0f;
	[Export]
	private int maxHealth = 100;
	[Export]
	private int health;
	[Export]

	// ========================================== Device Managment ========================================== 
	public int deviceID {get; private set;}
	[Export]
	public int playerIndex {get; set;} = 0;

	// InputManger inputManager;
	private int curretnDevice = -2; // No Device Found


	// ========================================== Signals ========================================== 
	[Signal]
	public delegate void HealthChangedEventHandler(int current, int max);

	[Signal]
	public delegate void DiedEventHandler();

	public override void _Ready()
	{
		base._Ready();
		health = maxHealth;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		if (health <= 0)
		{
			Die();
		}
	}

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);
		
	}



	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * speed;
			velocity.Z = direction.Z * speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void TakeDamage(int ammout)
	{
		health -= ammout;
		EmitSignal(SignalName.HealthChanged, health, maxHealth);
	}

	private void Die()
	{
		EmitSignal(SignalName.Died);
		QueueFree();
	}
}

using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

public partial class SpinAttack : Node3D
{
	[Export] public int maxSpinCharges = 3;
	[Export] public float spinChargeTimeout = 1.0f;
	[Export] public float maxSpinTime = 5.0f;
	[Export] public float spinDuration = 2;
	[Export] public float spinThreshold = 360f;
	[Export]private CollisionShape3D collisionShape;
	[Export] private AnimationPlayer animPlayer;
	[Export] private float dazeTimer = 2f;
	[Export] Area3D spinArea;

	[Signal] public delegate void SpinChargeGainedEventHandler(int currentCharges, int maxCharges);
	[Signal] public delegate void SpinAttackReadyEventHandler(int charges);
	[Signal] public delegate void SpinAttackCancelledEventHandler();
	[Signal] public delegate void CharacterDazedEventHandler();

	private int spinCharges;
	private float spinChargeTimer;
	private float totalSpinTimer;
	private float accumulatedRotation;
	private float startRotationY;
	private bool isCharging;
	private Node3D parent;
	private MeleeWeapon currentWeapon;
	private bool isDazed;
	private HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>();
	private bool isSpinning = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready(){

		
		parent = GetParent<Node3D>();
		//DisableCollision();

		if(spinArea != null)
		{
			spinArea.Monitoring = false;
			if(!spinArea.IsConnected(Area3D.SignalName.BodyEntered, Callable.From<Node3D>(OnSpinHit)))
			{
				spinArea.BodyEntered += OnSpinHit;
			}
			
			spinArea.AddToGroup("SpinAttack");
		}
		if(animPlayer == null)
        {
            animPlayer = parent.GetNode<AnimationPlayer>("CollisionShape3D/AnimationPlayer");
        }
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(isDazed || !isCharging || currentWeapon == null) return;
		
		totalSpinTimer += (float)delta;

		if(totalSpinTimer >= maxSpinTime)
		{
			TriggerDaze();
			return;
		}

		spinChargeTimer -= (float)delta;
		if(spinChargeTimer <= 0)
		{
			CancelSpin();
			return;
		}

		float currentRotation = parent.Rotation.Y;
		float frameDelta = Mathf.AngleDifference(startRotationY, currentRotation);
		if(Mathf.Sign(frameDelta) == Mathf.Sign(accumulatedRotation) || accumulatedRotation == 0)
		{
			accumulatedRotation += frameDelta;
		}
		else
		{
			accumulatedRotation = frameDelta;
		}
		startRotationY = currentRotation;
		float totalDegrees = Mathf.RadToDeg(Mathf.Abs(accumulatedRotation));

		if(totalDegrees >= spinThreshold)
		{
			spinCharges++;
			//GD.Print($">>> Charge Gained! Total: {spinCharges}/{maxSpinCharges} <<<");

			accumulatedRotation = 0;
			spinChargeTimer = spinChargeTimeout;

			if(spinCharges >= maxSpinCharges)
			{
				ExecuteSpinAttack();
			}
		}
		else
		{
			startRotationY = currentRotation;
		}

		if(isSpinning && spinArea != null)
		{
			var overlappingBodies = spinArea.GetOverlappingBodies();
			foreach(var body in overlappingBodies)
				ProcessSpinHit(body);
		}
	}

	public void StartCharging()
	{
		if(isCharging || isDazed || currentWeapon == null)
        {
            //DisableCollision();
			return;
        } 

		isCharging = true;
		spinCharges = 0;
		spinChargeTimer = spinChargeTimeout;
		totalSpinTimer = 0f;
		accumulatedRotation = 0f;
		startRotationY = parent.Rotation.Y;
	}
	public void StopCharging()
	{
		if(!isCharging) return;
		if(spinCharges > 0 && currentWeapon != null)
			ExecuteSpinAttack();
		else
			CancelSpin();
	}
	
	private void ExecuteSpinAttack()
	{
		//GD.Print($"SPIN ATTACK! Power: {spinCharges}");
		hitEnemies.Clear();

		if(currentWeapon != null && animPlayer !=null)
		{
			var animation = animPlayer.GetAnimation("Spin_Attack");
			animation.LoopMode = Animation.LoopModeEnum.Linear;

			animPlayer.Play("Spin_Attack");
			
			animPlayer.SpeedScale = 2.0f;

			if(spinArea != null)
			{
				spinArea.Monitoring = true;
				isSpinning = true;

				var overlappingBodies = spinArea.GetOverlappingBodies();
				foreach(var body in overlappingBodies) 
					ProcessSpinHit(body);

				parent.GetTree().CreateTimer(spinDuration).Timeout += () =>
				{
					isSpinning = false;

					if(IsInstanceValid(spinArea))
						spinArea.Monitoring = false;
					
					animPlayer.Stop();
					//TriggerDaze();
				};
			}		
		}

		Reset();
	}

	private void CancelSpin()
	{
		//GD.Print("Spin Cancelled");
		Reset();
	}

	private void TriggerDaze()
	{
		//GD.Print("DAZED!");
		isDazed = true;

		if(parent is PlayerController player)
		{
			player.SetDazed(true);
			animPlayer.Play("Anim_Player_Dazed");
		}

		parent.GetTree().CreateTimer(dazeTimer).Timeout += () =>
		{
			isDazed = false;
 
			if(parent is PlayerController player)
			{
				animPlayer.Play("Anim_Player_Idle");
				var animLength = animPlayer.GetAnimation("Anim_Player_Idle").Length;
				parent.GetTree().CreateTimer(animLength + 2).Timeout += () => player.SetDazed(false);
			}

			animPlayer.SpeedScale = 1;
		};
		Reset();
	}

	private void Reset()
	{
		isCharging = false;
		spinCharges = 0;	
		hitEnemies.Clear();
		
	}

	private void OnSpinHit(Node3D body)
	{
		ProcessSpinHit(body);
	}

	private void ProcessSpinHit(Node3D body)
	{
		
		if(body is EnemyController enemy && currentWeapon != null)
		{
			if(hitEnemies.Contains(enemy)) return;

			hitEnemies.Add(enemy);

			int damage = currentWeapon.damage;
			enemy.TakeDamage(damage);

			Vector3 pushDirection = (body.GlobalPosition - parent.GlobalPosition).Normalized();
			pushDirection.Y = 0;
			enemy.ApplyKnockback(pushDirection, 30f);

			currentWeapon.TakeDurabilityDamage(1);
		}
		else if(currentWeapon == null && !hitEnemies.Any()) TriggerDaze();

		
	}
	public MeleeWeapon GetCurrentWeapon() => currentWeapon;

	public void SetWeapon(MeleeWeapon weapon) => currentWeapon = weapon;
	public bool IsCharging() => isCharging;
	public bool IsDazed() => isDazed;
}

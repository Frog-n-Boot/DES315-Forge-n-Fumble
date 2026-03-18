using Godot;
using System;
using System.ComponentModel;

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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready(){

		
		parent = GetParent<Node3D>();
		//DisableCollision();

		if(spinArea != null)
		{
			spinArea.Monitoring = false;
			spinArea.BodyEntered += OnSpinHit;

			spinArea.AddToGroup("SpinAttack");
		}
		if(animPlayer == null)
        {
            animPlayer = parent.GetNode<AnimationPlayer>("CollisionShape3D");
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
			GD.Print($">>> Charge Gained! Total: {spinCharges}/{maxSpinCharges} <<<");

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
		if(spinCharges > 0)
			ExecuteSpinAttack();
		else
			CancelSpin();
	}
	
	private void ExecuteSpinAttack()
	{
		GD.Print($"SPIN ATTACK! Power: {spinCharges}");

		if(currentWeapon != null && animPlayer !=null)
		{
			var animation = animPlayer.GetAnimation("Spin_Attack");
			animation.LoopMode = Animation.LoopModeEnum.Linear;

			animPlayer.Play("Spin_Attack");
			
			animPlayer.SpeedScale = 4.0f;

			if(spinArea != null)
			{
				spinArea.Monitoring = true;

				parent.GetTree().CreateTimer(spinDuration).Timeout += () =>
				{
					if(IsInstanceValid(spinArea))
						spinArea.Monitoring = false;
					
					animPlayer.Stop();
					TriggerDaze();
				};
			}
			//EnableCollision();
			// //var originalArea = currentWeapon.GetNode<Area3D>("StaticBody3D/Area3D");
			// var originalCollision = currentWeapon.GetNode<CollisionShape3D>("StaticBody3D/CollisionShape3D");
			// var originalShape = originalCollision.Shape;
			// var originalPosition= originalCollision.GlobalPosition;
			// var originalScale = originalCollision.Scale;

			// var sphereShape = new SphereShape3D();
			
			// originalCollision.Shape = sphereShape;
			// originalCollision.GlobalPosition = new Vector3(parent.GlobalPosition.X, parent.GlobalPosition.Y + 1, parent.GlobalPosition.Z);
			// originalCollision.Scale = new Vector3(5, 5, 5);
			
			// parent.GetTree().CreateTimer(spinDuration).Timeout += () =>
            // {
            //     DisableCollision();
            // };

			//EnableArea();
			
			
		}

		Reset();
	}

	private void CancelSpin()
	{
		GD.Print("Spin Cancelled");
		Reset();
	}

	private void TriggerDaze()
	{
		GD.Print("DAZED!");
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
				parent.GetTree().CreateTimer(animLength).Timeout += () => player.SetDazed(false);
			}

			animPlayer.SpeedScale = 1;
		};
		Reset();
	}

	private void Reset()
	{
		isCharging = false;
		spinCharges = 0;	
		
	}

	private void OnSpinHit(Node3D body)
	{
		GD.Print($"Spin area detected: {body.Name}");

		var enemyNode = body.GetParent();
		if(enemyNode is EnemyController enemy && currentWeapon != null)
		{
			int damage = currentWeapon.damage;
			enemy.TakeDamage(damage);

			Vector3 pushDirection = (body.GlobalPosition - parent.GlobalPosition).Normalized();
			pushDirection.Y = 0;
			enemy.ApplyKnockback(pushDirection, 30f);

			currentWeapon.TakeDurabilityDamage(1);
		}
	}

	public MeleeWeapon GetCurrentWeapon() => currentWeapon;

	public void SetWeapon(MeleeWeapon weapon) => currentWeapon = weapon;
	public bool IsCharging() => isCharging;
	public bool IsDazed() => isDazed;
}

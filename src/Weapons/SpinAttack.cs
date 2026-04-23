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
	[Export] private CollisionShape3D collisionShape;
	[Export] private AnimationPlayer animPlayer;
	[Export] private float dazeTimer = 2f;
	[Export] Area3D spinArea;

	[Signal] public delegate void SpinChargeGainedEventHandler(int currentCharges, int maxCharges);
	[Signal] public delegate void SpinAttackReadyEventHandler(int charges);
	[Signal] public delegate void SpinAttackCancelledEventHandler();
	[Signal] public delegate void CharacterDazedEventHandler();

	// Animation name constants — must match AnimationPlayer exactly
	private const string ANIM_START_SPIN = "StartSpin";
	private const string ANIM_SPIN       = "Spin";
	private const string ANIM_DAZED      = "Dazed";
	private const string ANIM_GET_UP     = "GetUp";
	private const string ANIM_IDLE       = "Idle";

	private const float BLEND_TIME = 0.2f;

	private int spinCharges;
	private float spinChargeTimer;
	private float totalSpinTimer;
	private float accumulatedRotation;
	private float startRotationY;
	private bool isCharging;
	private Node3D parent;
	private MeleeWeapon currentWeapon;
	private bool isDazed;
	private bool isDazedTriggered = false; // guard so TriggerDaze only fires once per spin
	private HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>();
	private bool isSpinning = false;

	public override void _Ready()
	{
		parent = GetParent<Node3D>();

		if (spinArea != null)
		{
			spinArea.Monitoring = false;
			if (!spinArea.IsConnected(Area3D.SignalName.BodyEntered, Callable.From<Node3D>(OnSpinHit)))
			{
				spinArea.BodyEntered += OnSpinHit;
			}

			spinArea.AddToGroup("SpinAttack");
		}

		if (animPlayer == null)
		{
			// animPlayer should be assigned in the Inspector via @Export.
			// Fallback: try the parent's AnimationPlayer (adjust path if needed).
			animPlayer = parent.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
			if (animPlayer == null)
				GD.PrintErr("[SpinAttack] AnimationPlayer not assigned and not found — assign it in the Inspector.");
		}

		// Listen for animation end so we can chain Dazed → GetUp → Idle
		if (animPlayer != null)
			animPlayer.AnimationFinished += OnAnimationFinished;
	}

	public override void _Process(double delta)
	{
		// Don't do anything while dazed or a spin is already executing
		if (isDazed || isDazedTriggered || !isCharging || currentWeapon == null) return;

		totalSpinTimer += (float)delta;

		if (totalSpinTimer >= maxSpinTime)
		{
			TriggerDaze();
			return;
		}

		spinChargeTimer -= (float)delta;
		if (spinChargeTimer <= 0)
		{
			CancelSpin();
			return;
		}

		float currentRotation = parent.Rotation.Y;
		float frameDelta = Mathf.AngleDifference(startRotationY, currentRotation);
		startRotationY = currentRotation;
		if(Mathf.Abs(frameDelta) > 0.001f)
		{
			if (accumulatedRotation == 0 || Mathf.Sign(frameDelta) == Mathf.Sign(accumulatedRotation))
			{
				accumulatedRotation += frameDelta;
			}
			else
			{
				accumulatedRotation = 0;
			}
		}
		
		
		float totalDegrees = Mathf.RadToDeg(Mathf.Abs(accumulatedRotation));

		if (totalDegrees >= spinThreshold)
		{
			spinCharges++;
			accumulatedRotation = 0;
			spinChargeTimer = spinChargeTimeout;

			if (spinCharges >= maxSpinCharges)
				ExecuteSpinAttack();
			else
				EmitSignal(SignalName.SpinChargeGained, spinCharges, maxSpinCharges);
		}
		else
		{
			startRotationY = currentRotation;
		}

		if (isSpinning && spinArea != null)
		{
			var overlappingBodies = spinArea.GetOverlappingBodies();
			foreach (var body in overlappingBodies)
				ProcessSpinHit(body);
		}
	}

	public void StartCharging()
	{
		if (isCharging || isDazed || currentWeapon == null) return;

		isCharging = true;
		spinCharges = 0;
		spinChargeTimer = spinChargeTimeout;
		totalSpinTimer = 0f;
		accumulatedRotation = 0f;
		startRotationY = parent.Rotation.Y;
	}

	public void StopCharging()
	{
		if (!isCharging) return;
		if (spinCharges > 0 && currentWeapon != null)
			ExecuteSpinAttack();
		else
			CancelSpin();
	}

	private void ExecuteSpinAttack()
	{
		hitEnemies.Clear();

		if (currentWeapon != null && animPlayer != null)
		{
			// Play StartSpin first — OnAnimationFinished chains it into Spin
			PlayAnim(ANIM_START_SPIN, 0f);
			if (parent is PlayerController player)
			{
				if (spinArea != null)
				{
					//player.isAttacking = true;
					spinArea.Monitoring = true;
					isSpinning = true;
				
					var overlappingBodies = spinArea.GetOverlappingBodies();
					foreach (var body in overlappingBodies)
						ProcessSpinHit(body);
				}
			}

			

			// Always schedule the daze regardless of spinArea — this is the only
			// place the timer fires, TriggerDaze's guard prevents double-calls.
			parent.GetTree().CreateTimer(spinDuration).Timeout += () =>
			{
				isSpinning = false;

				if (IsInstanceValid(spinArea))
					spinArea.Monitoring = false;
				
				TriggerDaze();
			};
		}

		// Reset charging state — isDazedTriggered stays false until TriggerDaze fires
		Reset();
	}

	private void CancelSpin()
	{
		Reset();
	}

	private void TriggerDaze()
	{
		if (isDazedTriggered) return;
		isDazedTriggered = true;

		isDazed = true;

		if (parent is PlayerController player)
		{
			player.SetDazed(true);
			player.OnDazedSequenceStarted();
		}

		PlayAnim(ANIM_DAZED, 0f);
		
		parent.GetTree().CreateTimer(dazeTimer).Timeout += () =>
		{
			isDazed = false;

			if (parent is PlayerController player)
			{
				player.SetDazed(false);

				// Force-end the sequence if the animation chain didn't do it
				if (player.isInDazedSequence)
				{
					isDazedTriggered = false;
					//player.isAttacking = false;
					player.OnDazedSequenceEnded();
				}
			}
		};
		
		Reset();
	}

	private void OnAnimationFinished(StringName animName)
	{
		string name = animName.ToString();

		// StartSpin finished → begin the looping Spin animation
		if (name == ANIM_START_SPIN)
		{
			if (isSpinning) // only go to Spin if the spin attack is still active
			{
				var spinAnim = animPlayer.GetAnimation(ANIM_SPIN);
				if (spinAnim != null)
					spinAnim.LoopMode = Animation.LoopModeEnum.Linear;

				PlayAnim(ANIM_SPIN, 0f);
				animPlayer.SpeedScale = 2.0f;
			}
		}

		// Dazed finished → play GetUp
		if (name == ANIM_DAZED)
		{
			animPlayer.SpeedScale = 1f;
			PlayAnim(ANIM_GET_UP, 0f);
			animPlayer.SpeedScale = 2f;
		}

		// GetUp finished → back to Idle, notify PlayerController
		if (name == ANIM_GET_UP)
		{
			isDazedTriggered = false;

			if (parent is PlayerController player)
			{
				player.OnDazedSequenceEnded();
			}
			else
			{
				PlayAnim(ANIM_IDLE, BLEND_TIME);
			}

		
		}
	}

	// -------------------------------------------------------------------------
	// Helper: play animation with blending only if not already playing it.
	// -------------------------------------------------------------------------
	private void PlayAnim(string animName, float blend = BLEND_TIME)
	{
		if (animPlayer == null) return;
		if (!animPlayer.HasAnimation(animName))
		{
			GD.PrintErr($"[SpinAttack] Animation not found: {animName}");
			return;
		}
		if (animPlayer.CurrentAnimation == animName && animPlayer.IsPlaying()) return;
		animPlayer.Play(animName, customBlend: blend);
	}

	private void Reset()
	{
		
		isCharging = false;
		spinCharges = 0;
		hitEnemies.Clear();
		// Note: isDazedTriggered is intentionally NOT cleared here — it stays true
		// until GetUp finishes, preventing any re-entry during the daze sequence.
	}

	private void OnSpinHit(Node3D body)
	{
		ProcessSpinHit(body);
	}

	private void ProcessSpinHit(Node3D body)
	{
		if (body is EnemyController enemy && currentWeapon != null)
		{
			if (hitEnemies.Contains(enemy)) return;

			hitEnemies.Add(enemy);

			int damage = currentWeapon.damage;
			enemy.TakeDamage(damage);

			Vector3 pushDirection = (body.GlobalPosition - parent.GlobalPosition).Normalized();
			pushDirection.Y = 0;
			enemy.ApplyKnockback(pushDirection, 30f);

			currentWeapon.TakeDurabilityDamage(1);
		}
		else if (currentWeapon == null && !hitEnemies.Any())
			TriggerDaze();
	}

	public MeleeWeapon GetCurrentWeapon() => currentWeapon;
	public void SetWeapon(MeleeWeapon weapon) => currentWeapon = weapon;
	public bool IsCharging() => isCharging;
	public bool IsDazed() => isDazed;
}
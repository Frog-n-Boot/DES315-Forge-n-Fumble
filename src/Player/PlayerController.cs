using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata;

namespace Godot;

public partial class PlayerController : CharacterBody3D, ItemCarrier
{
	[ExportCategory("Player Attributes")]
	[Export] public int PlayerIndex { get; set; } = 0;
	[Export] public float speed = 5.0f;
	[Export] public float sprintMultiplier = 1.8f;
	[Export] public float jumpVelocity = 4.5f;
	[Export] public int maxHealth = 100;
	[Export] public int health;
	[Export] public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	[Export] public int damage = 10;
	[Export] public PackedScene swordObject { get; private set; }
	[Export] public AnimationPlayer animPlayer;
	[Export] private AudioStreamPlayer3D weaponAudio;
	[Export] private AudioStreamPlayer3D damagedAudio;
	[Export] private AudioStreamPlayer3D deathAudio;
	[Export] private BoneAttachment3D rightHand;
	[Export] private BoneAttachment3D leftHand;
	[Export] private Area3D areaPickup;
	[Export] public float playerSpawnTimer;
	[Export] private Texture2D[] playerMaterialTextures;
	[Export] public float flashDuration = 0.2f;
	[Export] public float spinStartThreshold = 3.0f;
	[Export] public float spinStopThreshold = 1.0f;
	private PlayerSpawner playerSpawner;

	// Animation name constants
	private const string ANIM_IDLE       = "Idle";
	private const string ANIM_WALK       = "Walk";
	private const string ANIM_SWING      = "Swing";
	private const string ANIM_DEATH      = "Death";
	private const string ANIM_DAZED      = "Dazed";
	private const string ANIM_GET_UP     = "GetUp";
	private const string ANIM_SPIN       = "Spin";
	private const string ANIM_START_SPIN = "StartSpin";

	private const float BLEND_TIME = 0.4f;

	public int currentDevice = -2;

	private SequenceMinigame sequenceMinigame;
	private BarMinigame barMinigame;
	private Camera3D camera;
	private StaticBody3D world;
	private InputManager inputManager;
	private HashSet<string> actionsPressed = new HashSet<string>();
	private HashSet<string> actionsPressedLastFrame = new HashSet<string>();
	private HashSet<Node> processPickable = new HashSet<Node>();
	private BaseStationScript currentStation;
	private Label3D pickupPrompt;
	private Node3D pickupNode;

	[Signal] public delegate void PlayerHealthChangedEventHandler(int current, int max);
	[Signal] public delegate void DiedEventHandler();
	Forge forge;
	HealthPack healthPack;

	private int _healthPack;
	private Vector3 currentLookTarget;
	public BaseWeapon currentWeapon;
	private bool isAttacking = false;
	private int tick = 0;
	private Pickable nearbyPickable;
	private BaseWeapon nearbyWeapon;

	private Vector3 knockback = Vector3.Zero;
	private StandardMaterial3D material;

	private InputBuffer inputBuffer;

	private SpinAttack spinAttack;
	private float lastRotation = 0f;
	private float dropHoldTimer = 0f;
	private bool dropTriggered = false;
	private float reloadTime = 0;
	private bool reloadTriggered = false;

	private float attackHoldTimer = 0f;
	private bool attackHeld = false;

	private Ballista nearbyBallista = null;
	private float ballistaHoldTimer = 0f;
	private bool ballistaActionTriggered = false;

	public bool isDazed { get; private set; } = false;

	public bool isInDazedSequence = false;

	public override void _Ready()
	{
		playerSpawner = GetTree().Root.GetNodeOrNull<PlayerSpawner>("Scene/PlayerSpawner");
		health = maxHealth;
		inputBuffer = new InputBuffer();
		AddChild(inputBuffer);

		inputManager = GetNode<InputManager>("/root/InputManager");
		if (inputManager == null)
			GD.PrintErr("PlayerController: InputManager not found!");

		world = GetTree().Root.GetNodeOrNull<StaticBody3D>("testing_lab");
		if (world != null)
			camera = world.GetNodeOrNull<Camera3D>("Camera3D");

		if (camera == null)
			camera = GetViewport().GetCamera3D();

		forge = GetNodeOrNull<Forge>("/root/Forge");
		healthPack = GetNodeOrNull<HealthPack>("/root/HealthPack");

		// areaPickup is assigned via @Export — only fall back to path if missing
		if (areaPickup == null)
			areaPickup = GetNodeOrNull<Area3D>("Area3D");
		if (areaPickup == null)
			GD.PrintErr("PlayerController: areaPickup not assigned and not found!");
		else
		{
			areaPickup.AreaEntered += OnPickupAreaEntered;
			areaPickup.AreaExited += OnPickUpAreaExited;
		}

		if (animPlayer == null)
			GD.PrintErr("PlayerController: animPlayer not assigned in Inspector!");
		else
		{	
			animPlayer.SpeedScale = 2.0f;
			animPlayer.AnimationFinished += OnAnimationFinished;
		}
		// rightHand / leftHand are BoneAttachment3D nodes assigned via @Export.
		// Do NOT re-fetch by path — that would overwrite the inspector values with null.
		if (rightHand == null)
			GD.PrintErr("PlayerController: rightHand BoneAttachment3D not assigned in Inspector!");
		if (leftHand == null)
			GD.PrintErr("PlayerController: leftHand BoneAttachment3D not assigned in Inspector!");

		if (rightHand != null)
			FindExistingSword();

		spinAttack = GetNode<SpinAttack>("SpinAttack");

		if (currentWeapon is MeleeWeapon melee)
			spinAttack.SetWeapon(melee);

		lastRotation = Rotation.Y;

		
		PlayAnim(ANIM_IDLE);

		if (XRayManager.Instance == null){
			GD.PrintErr("[Player]  XRayManager.Instance is null — autoload not ready yet");
		}
		else{
			GD.Print($"[Player]  XRayManager found, registering {Name}");
		}

		CallDeferred(MethodName.RegisterWithManager);
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		XRayManager.Instance?.UnregisterPlayer(this);
	}

	private void RegisterWithManager()
	{
		if (XRayManager.Instance == null)
		{
			GD.PrintErr("[Player] XRayManager.Instance is null!");
			return;
		}
		GD.Print($"[Player] Registering {Name} with XRayManager");
		XRayManager.Instance.RegisterPlayer(this);
	}
	private void PlayAnim(string animName, float blend = BLEND_TIME)
	{
		if (animPlayer == null) return;
		if (!animPlayer.HasAnimation(animName))
		{
			GD.PrintErr($"[PlayerController] Animation not found: {animName}");
			return;
		}
		if (animPlayer.CurrentAnimation == animName && animPlayer.IsPlaying()) return;
		animPlayer.Play(animName, customBlend: blend);
	}

	private void UpdateLocomotionAnim()
	{
		if (isAttacking || isInDazedSequence) return;

		// Don't interrupt spin animations — SpinAttack manages those itself.
		string cur = animPlayer.CurrentAnimation;
		if (cur == ANIM_START_SPIN || cur == ANIM_SPIN) return;

		bool isMoving = new Vector2(Velocity.X, Velocity.Z).Length() > 0.1f;
		PlayAnim(isMoving ? ANIM_WALK : ANIM_IDLE);
	}

	public IEnumerable<ItemData> GetCarriedItems()
	{
		if (leftHand != null && leftHand.GetChildCount() > 0)
		{
			var pickable = leftHand.GetChild(0) as Pickable;
			if (pickable != null)
				yield return pickable.GetItemData();
		}

		if (rightHand != null && rightHand.GetChildCount() > 0)
		{
			var pickable = rightHand.GetChild(0) as Pickable;
			if (pickable != null)
			{
				yield return pickable.GetItemData();
			}
		}
	}

	public void RemoveItem(ItemData item)
	{
		foreach (var hand in new[] { leftHand, rightHand })
		{
			if (hand.GetChildCount() == 0) continue;

			var pickable = hand.GetChild(0) as Pickable;
			if (pickable != null && pickable.GetItemData() == item)
			{
				pickable.QueueFree();
				return;
			}
		}
	}

	private void FindExistingSword()
	{
		if (rightHand == null) return;

		foreach (Node child in rightHand.GetChildren())
		{
			if (child is BaseWeapon weapon)
			{
				currentWeapon = weapon;

				// Apply standard right-hand offset so the pre-placed sword sits correctly.
				weapon.Position = RightHandPosOffset;
				weapon.Rotation = RightHandRotOffset;
				weapon.Scale    = HandScale;

				if (weapon is Sword sword)
					sword.CheckDurability += OnSwordDurabilityChecked;

				currentWeapon.Broke += OnSwordBroke;
				currentWeapon.SetEnemyCollisionEnabled(true);
				GD.Print($"Player {PlayerIndex} found existing sword in hand!");
				return;
			}
		}
	}

	public void SetPlayerIndex(int index)
	{
		PlayerIndex = index;

		if (inputManager == null)
			inputManager = GetNode<InputManager>("/root/InputManager");

		if (inputManager != null)
		{
			currentDevice = inputManager.GetDeviceForPlayer(PlayerIndex);
			GD.Print($"Player initialized with index {PlayerIndex}, device {currentDevice}");
		}
		else
		{
			GD.PrintErr($"Player {PlayerIndex}: Could not find InputManager!");
		}

		UpdatePlayerAppearance();
	}

	public override void _Process(double delta)
	{
		if (spinAttack.IsDazed()) return;

		float rotationSpeed = Mathf.Abs(Rotation.Y - lastRotation) / (float)delta;
		lastRotation = Rotation.Y;

		if (rotationSpeed > spinStartThreshold && !spinAttack.IsCharging())
		{
			spinAttack.StartCharging();
		}

		if (rotationSpeed < spinStopThreshold && spinAttack.IsCharging())
		{
			spinAttack.StopCharging();
		}

		if (sequenceMinigame != null && sequenceMinigame.IsActiveFor(this))
			return;

		if (barMinigame != null && barMinigame.IsActiveFor(this))
			return;

		if (inputManager == null)
			return;

		currentDevice = inputManager.GetDeviceForPlayer(PlayerIndex);

		if (currentDevice == -2)
		{
			Visible = false;
			SetPhysicsProcess(false);
			return;
		}

		Visible = true;
		SetPhysicsProcess(true);

		actionsPressedLastFrame.Clear();
		foreach (var action in actionsPressed)
			actionsPressedLastFrame.Add(action);

		if (IsActionPressed("interact") && currentStation != null)
		{
			if (currentStation is GrindstoneStation grindstone && currentWeapon is Sword sword1)
			{
				if (grindstone.DepositSword(sword1))
				{
					rightHand.RemoveChild(sword1);
					currentWeapon = null;
				}
			}
			else if (leftHand.GetChildCount() > 0)
			{
				var item = leftHand.GetChild(0) as Pickable;
				if (item != null && item.GetItemData() != null)
				{
					if (currentStation.DepositItems(item.GetItemData()))
						item.QueueFree();
				}
			}
			else
			{
				currentStation.StartCrafting();
			}
		}
		else if (IsActionPressed("interact") && nearbyBallista != null)
		{
			if (currentWeapon == null && !nearbyBallista.isDetached)
			{
				ballistaHoldTimer += (float)delta;
				if (ballistaHoldTimer >= 0.5f && !ballistaActionTriggered)
				{
					ballistaActionTriggered = true;
					nearbyBallista.DetachHead(this);
					ballistaHoldTimer = 0f;
				}
			}
			else if (currentWeapon is Crossbow bow && bow.sourceBallista == nearbyBallista)
			{
				ballistaHoldTimer += (float)delta;
				if (ballistaHoldTimer >= 0.5f && !ballistaActionTriggered)
				{
					ballistaActionTriggered = true;
					nearbyBallista.ReattachHead(bow);
					currentWeapon = null;
					ballistaHoldTimer = 0f;
				}
			}
		}
		else
		{
			ballistaHoldTimer = 0f;
			ballistaActionTriggered = false;
		}

		if (IsActionPressed("pick_up"))
		{
			if (nearbyWeapon != null && rightHand.GetChildCount() == 0)
			{
				PickUpWeapon(nearbyWeapon);
			}
			else if (nearbyPickable != null)
				ProcessPickable(nearbyPickable);
		}

		if (Input.IsActionPressed("drop_item"))
		{
			dropHoldTimer += (float)delta;
			if (dropHoldTimer >= 0.5f && !dropTriggered)
			{
				dropTriggered = true;
				DropItem(rightHand);
			}
		}

		if (currentWeapon is Sword sword2)
		{
			if (inputBuffer.IsInputBuffered("attack") && !isAttacking)
			{
				sword2.PerformComboAttack(inputBuffer);
				StartAttack();
			}
		}
		else if (currentWeapon is Crossbow bow2)
		{
			bow2.shootingDirectionMesh.Visible = true;

			if (Input.IsActionPressed("attack"))
			{
				attackHoldTimer += (float)delta;
				attackHeld = true;
			}

			if (Input.IsActionJustReleased("attack") && attackHeld && !isAttacking)
			{
				if (attackHoldTimer >= 1f) bow2.tripleShot?.TryActivate(bow2);
				else bow2.Use();

				isAttacking = true;
				attackHoldTimer = 0f;
				attackHeld = false;
			}

			if (!Input.IsActionPressed("attack"))
			{
				attackHoldTimer = 0f;
				attackHeld = false;
			}

			if (Input.IsActionPressed("interact") && nearbyBallista == null && currentStation == null)
			{
				var heldItem = leftHand.GetChildCount() > 0 ? leftHand.GetChild(0) as Pickable : null;
				var hasIngot = heldItem != null && heldItem.itemData != null && heldItem.itemData.name.Contains("Iron_Ingot");

				if (hasIngot)
				{
					reloadTime += (float)delta;

					if (reloadTime >= 1 && !reloadTriggered)
					{
						reloadTriggered = true;
						bow2.Reload(3);
						heldItem.QueueFree();
						reloadTime = 0f;
					}
				}
				else
				{
					reloadTime = 0f;
					reloadTriggered = false;
				}
			}
			else if (!Input.IsActionPressed("interact"))
			{
				reloadTime = 0f;
				reloadTriggered = false;
			}
		}
		else if (inputBuffer.ConsumeInput("attack") && !isAttacking)
		{
			StartAttack();
		}

		// Update walk/idle every frame
		UpdateLocomotionAnim();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (isDazed)
		{
			Velocity = Vector3.Zero;
			return;
		}

		if (sequenceMinigame != null && sequenceMinigame.IsActiveFor(this))
		{
			GD.Print("PlayerLocked");
			return;
		}

		if (barMinigame != null && barMinigame.IsActiveFor(this))
		{
			GD.Print("PlayerLocked");
			return;
		}

		Vector3 velocity = Velocity;

		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		Vector2 inputDir = GetMovementVector();
		Vector3 lookDir = GetLookVector();

		if (inputDir != Vector2.Zero)
		{
			velocity.X = inputDir.X * speed + knockback.X;
			velocity.Z = inputDir.Y * speed + knockback.Z;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X + knockback.X, 0, speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z + knockback.Z, 0, speed);
		}

		knockback = knockback.Lerp(Vector3.Zero, 0.15f);
		Velocity = velocity;

		if (lookDir != Vector3.Zero)
		{
			float targetAngle = Mathf.Atan2(lookDir.X, lookDir.Z);
			Rotation = new Vector3(Rotation.X, targetAngle, Rotation.Z);
		}

		tick += 1;
		if (tick % 10 == 0)
		{
			if (health <= 0) Die();
		}

		MoveAndSlide();
	}

	public override void _Input(InputEvent @event)
	{
		if (currentDevice == -2 || inputManager == null) return;

		if (barMinigame != null && barMinigame.IsActiveFor(this)) barMinigame.HandleInput(@event);

		bool isFromOurDevice = false;

		if (currentDevice == -1 && (@event is InputEventKey || @event is InputEventMouseButton))
		{
			isFromOurDevice = true;
		}

		if (currentDevice >= 0)
		{
			if (@event is InputEventJoypadButton joyButton)
				isFromOurDevice = joyButton.Device == currentDevice;

			if (@event is InputEventJoypadMotion joyMotion)
				isFromOurDevice = joyMotion.Device == currentDevice;
		}

		if (!isFromOurDevice)
			return;

		foreach (var action in InputMap.GetActions())
		{
			if (@event.IsActionPressed(action))
			{
				actionsPressed.Add(action);
			}

			if (@event.IsActionReleased(action))
			{
				actionsPressed.Remove(action);
			}
		}

		if (@event.IsActionPressed("drop_item"))
		{
			dropHoldTimer = 0f;
			dropTriggered = false;
		}
		if (@event.IsActionReleased("drop_item"))
		{
			if (!dropTriggered)
				DropItem(leftHand);
		}

		if (IsActionJustPressed("attack"))
		{
			inputBuffer.BufferInput("attack");
		}

		if (@event.IsActionReleased("attack"))
		{
			GD.Print("Attack released");
			// Only end attack early for ranged weapons. Melee plays to completion
			// and resets via OnAnimationFinished / OnComboReset.
			if (currentWeapon is Crossbow)
				EndAttack();
		}
	}

	private void StartAttack()
	{
		if (currentWeapon == null)
		{
			GD.Print("No sword to attack with!");
			return;
		}

		isAttacking = true;

		if (currentWeapon is MeleeWeapon melee)
		{
			weaponAudio.Play();

			if (currentWeapon is Sword sword)
			{
				// Sword combo animations still come from the sword itself,
				// but if it returns null / empty fall back to Swing.
				string comboAnim = sword.GetComboAnimation();
				if (!string.IsNullOrEmpty(comboAnim) && animPlayer.HasAnimation(comboAnim))
					PlayAnim(comboAnim);
				else
					PlayAnim(ANIM_SWING);
			}
			else
			{
				PlayAnim(ANIM_SWING);
			}
		}
	}

	private void EndAttack()
	{
		if (currentWeapon == null) return;

		if (currentWeapon is MeleeWeapon melee)
		{
			if (!isAttacking) return;
			isAttacking = false;
		}
		else if (currentWeapon is Crossbow bow)
		{
			isAttacking = false;
		}

		// Return to locomotion immediately on end-attack
		UpdateLocomotionAnim();
	}

	private void OnAnimationFinished(StringName animName)
	{
		string name = animName.ToString();

		// Attack finished -> back to locomotion
		if (name == ANIM_SWING || name.Contains("Swing"))
		{
			isAttacking = false;
			UpdateLocomotionAnim();
		}

		// Dazed finished -> play GetUp
		if (name == ANIM_DAZED)
		{
			PlayAnim(ANIM_GET_UP, 0f);
		}

		// GetUp finished -> back to Idle
		if (name == ANIM_GET_UP)
		{
			// Guards against race condition with SpinAttack.cs
			if (!isInDazedSequence)
			{
				isInDazedSequence = false;
				PlayAnim(ANIM_IDLE);
			}
		}
	}

	private void OnPickupAreaEntered(Area3D area)
	{
		GD.Print($"AreaEntered: {area.Name} groups: {string.Join(", ", area.GetGroups())}");

		if (area.GetGroups().Count == 0) return;

		if (area.IsInGroup("pickable"))
		{
			nearbyPickable = area.GetParent() as Pickable;
			GD.Print($"NearbyPickable: {nearbyPickable?.Name ?? "null"}");
			if (nearbyPickable != null)
			{
				try { nearbyPickable.SetItemPromptTexture(IsUsingController()); }
				catch (Exception e) { GD.PrintErr($"[PlayerController] SetItemPromptTexture failed: {e.Message}"); }
			}
		}

		if (area.IsInGroup("Weapon"))
		{
			StaticBody3D areaParent = area.GetParent() as StaticBody3D;
			nearbyWeapon = areaParent?.GetParent() as BaseWeapon;
			if (nearbyWeapon != null)
			{
				try { nearbyWeapon.SetWeaponPromptTexture(IsUsingController()); }
				catch (Exception e) { GD.PrintErr($"[PlayerController] SetWeaponPromptTexture failed: {e.Message}"); }
			}
		}
	}

	private void OnPickUpAreaExited(Area3D area)
	{
		if (area.IsInGroup("pickable"))
		{
			if (nearbyPickable != null) nearbyPickable.HideItemPrompt();
			nearbyPickable = null;
		}

		if (area.IsInGroup("Weapon"))
		{
			if (nearbyWeapon != null) nearbyWeapon.HideWeaponPrompt();
			nearbyWeapon = null;
		}
	}

	// Hand transform offsets applied to items/weapons after reparenting.
	// BoneAttachment3D drives global position, so we set local offsets on the child.
	private static readonly Vector3 RightHandPosOffset = new Vector3(-0.28f,  0.16f,  -0.31f);
	private static readonly Vector3 RightHandRotOffset = new Vector3(
		Mathf.DegToRad(35f), Mathf.DegToRad(107f), Mathf.DegToRad(75f));
	private static readonly Vector3 LeftHandPosOffset  = new Vector3( 0.26f,  0.229f, -0.0f);
	private static readonly Vector3 LeftHandRotOffset  = new Vector3(
		Mathf.DegToRad(1f),  Mathf.DegToRad(13f),  Mathf.DegToRad(66f));
	private static readonly Vector3 HandScale = new Vector3(0.8f, 0.8f, 0.8f);

	private void ProcessPickable(Pickable pickable)
	{
		if (!IsInstanceValid(pickable)) return;
		if (processPickable.Contains(pickable)) return;

		processPickable.Add(pickable);

		if (leftHand.GetChildCount() > 0)
		{
			processPickable.Remove(pickable);
			return;
		}

		pickable.CallDeferred("reparent", leftHand);

		GetTree().CreateTimer(0.1f).Timeout += () =>
		{
			if (IsInstanceValid(pickable))
			{
				pickable.Position = LeftHandPosOffset;
				pickable.Rotation = LeftHandRotOffset;
				pickable.Scale    = HandScale;
			}
			processPickable.Remove(pickable);
		};
	}

	public void PickUpWeapon(BaseWeapon nearbyWeapon)
	{
		if (nearbyWeapon.isBeingPickedUp) return;
		nearbyWeapon.isBeingPickedUp = true;

		if (nearbyWeapon.pickUpArea != null) nearbyWeapon.pickUpArea.Monitoring = false;
		nearbyWeapon.HideWeaponPrompt();

		UpdateAnimationTreacks(nearbyWeapon.Name);
		nearbyWeapon.CallDeferred("reparent", rightHand);
		GetTree().CreateTimer(0.1f).Timeout += () =>
		{
			if (!IsInstanceValid(nearbyWeapon)) return;
			nearbyWeapon.Position = RightHandPosOffset;
			nearbyWeapon.Rotation = RightHandRotOffset;
			nearbyWeapon.Scale    = HandScale;

			currentWeapon = nearbyWeapon;
			nearbyWeapon.SetOwner(this);

			if (currentWeapon is Sword sword)
			{
				if (!sword.IsConnected(Sword.SignalName.ComboReset, Callable.From(OnComboReset)))
					sword.Connect(Sword.SignalName.ComboReset, Callable.From(OnComboReset));

				if (sword.IsConnected(Sword.SignalName.CheckDurability, Callable.From(OnSwordDurabilityChecked)))
					sword.Disconnect(Sword.SignalName.CheckDurability, Callable.From(OnSwordDurabilityChecked));
				sword.Connect(Sword.SignalName.CheckDurability, Callable.From(OnSwordDurabilityChecked));
				sword.SetHitboxEnabled(true);
			}

			nearbyWeapon.SetEnemyCollisionEnabled(true);

			if (currentWeapon.IsConnected(BaseWeapon.SignalName.Broke, Callable.From(OnSwordBroke)))
				currentWeapon.Disconnect(BaseWeapon.SignalName.Broke, Callable.From(OnSwordBroke));
			currentWeapon.Connect(BaseWeapon.SignalName.Broke, Callable.From(OnSwordBroke));

			currentWeapon.isBeingPickedUp = false;

			nearbyWeapon = null;

			if (spinAttack == null)
			{
				spinAttack = GetNodeOrNull<SpinAttack>("SpinAttack");
				if (spinAttack == null)
				{
					GD.Print("Spin attack still not found");
					return;
				}
			}
			if (currentWeapon is MeleeWeapon melee)
				spinAttack.SetWeapon(melee);
		};
	}

	private Vector3 GetLookVector()
	{
		if (currentDevice == -2)
			return currentLookTarget;

		var position = Position;

		if (currentDevice == -1)
		{
			var mousePos = GetViewport().GetMousePosition();
			var rayOrigin = camera.ProjectRayOrigin(mousePos);
			var rayDir = camera.ProjectRayNormal(mousePos);
			var plane = new Plane(Vector3.Up, position.Y);
			var intersect = plane.IntersectsRay(rayOrigin, rayDir);

			if (intersect != null)
			{
				var target = new Vector3(intersect.Value.X, position.Y, intersect.Value.Z);
				if (target.DistanceTo(position) > 0.1f)
					currentLookTarget = target - position;
			}
		}
		else
		{
			Vector2 input = new Vector2(
				Input.GetJoyAxis(currentDevice, JoyAxis.RightX),
				Input.GetJoyAxis(currentDevice, JoyAxis.RightY)
			);

			if (input.Length() > 0.2f)
				currentLookTarget = new Vector3(input.X, 0, input.Y).Normalized();
		}

		return currentLookTarget;
	}

	private Vector2 GetMovementVector()
	{
		if (currentDevice == -2)
			return Vector2.Zero;

		if (currentDevice == -1)
		{
			Vector2 input = Vector2.Zero;
			if (IsActionPressed("move_left")) input.X -= 1;
			if (IsActionPressed("move_right")) input.X += 1;
			if (IsActionPressed("move_up")) input.Y -= 1;
			if (IsActionPressed("move_down")) input.Y += 1;
			return input.Normalized();
		}
		else
		{
			Vector2 input = new Vector2(
				Input.GetJoyAxis(currentDevice, JoyAxis.LeftX),
				Input.GetJoyAxis(currentDevice, JoyAxis.LeftY)
			);

			if (input.Length() < 0.2f)
				return Vector2.Zero;

			return input;
		}
	}

	private bool IsActionPressed(string action)   => actionsPressed.Contains(action);
	private bool IsActionJustPressed(string action) => actionsPressed.Contains(action) && !actionsPressedLastFrame.Contains(action);
	private bool IsActionJustReleased(string action) => !actionsPressed.Contains(action) && actionsPressedLastFrame.Contains(action);

	private void UpdatePlayerAppearance()
	{
		Texture2D texture = playerMaterialTextures[PlayerIndex % playerMaterialTextures.Length];

		var meshInstance = GetNodeOrNull<MeshInstance3D>("CollisionShape3D/DwarfLow");
		if (meshInstance == null)
		{
			GD.PrintErr($"Player {PlayerIndex}: MeshInstance3D not found at CollisionShape3D/MeshInstance3D");
			return;
		}

		material = new StandardMaterial3D
		{
			AlbedoTexture = texture,
			EmissionEnabled = true,
			EmissionEnergyMultiplier = 0f
		};

		meshInstance.MaterialOverride = material;
	}

	public void TakeDamage(int damage)
	{
		health -= damage;
		damagedAudio.Play();
		DamageNumbers.Spawn(damage, GlobalPosition, GetParent());
		Flash();
		EmitSignal(SignalName.PlayerHealthChanged, health, maxHealth);
	}

	public void Heal(float amount)
	{
		health = Mathf.Min(health + (int)amount, maxHealth);
		EmitSignal(SignalName.PlayerHealthChanged, health, maxHealth);
	}

	public bool IsAlive() => health > 0;

	private void Die()
	{
		DropItem(rightHand);
		DropItem(leftHand);

		areaPickup.SetDeferred("monitoring", true);
		areaPickup.SetDeferred("monitorable", true);

		Visible = false;
		deathAudio.Play();

		// Play death animation before hiding/disabling
		PlayAnim(ANIM_DEATH, 0f);

		SetPhysicsProcess(false);
		SetProcess(false);

		GetNode<CollisionShape3D>("CollisionShape3D").SetDeferred("disabled", true);

		areaPickup.SetDeferred("monitoring", false);
		areaPickup.SetDeferred("monitorable", false);

		GetTree().CreateTimer(playerSpawnTimer).Timeout += () =>
		{
			GD.Print("Player spawning");
			GlobalPosition = playerSpawner.spawnPoints[PlayerIndex].GlobalPosition;
			health = maxHealth;
			Visible = true;
			SetPhysicsProcess(true);
			SetProcess(true);

			GetNode<CollisionShape3D>("CollisionShape3D").SetDeferred("disabled", false);
			areaPickup.SetDeferred("monitoring", true);
			currentWeapon = null;
			areaPickup.SetDeferred("monitorable", true);

			isAttacking = false;
			isInDazedSequence = false;
			PlayAnim(ANIM_IDLE);

			EmitSignal(SignalName.PlayerHealthChanged, health, maxHealth);
		};
	}

	private void OnSwordDurabilityChecked()
	{
		if (currentWeapon == null) return;
	}

	private void OnSwordBroke()
	{
		currentWeapon = null;
		if (spinAttack != null)
		{
			spinAttack.SetWeapon(null);
		}

		isAttacking = false;
		UpdateLocomotionAnim();
	}

	private void DropItem(Node3D hand)
	{
		if (hand.GetChildCount() == 0) return;

		var child = hand.GetChild(0);
		if (child == null || !IsInstanceValid(child)) return;

		areaPickup.SetDeferred("monitoring", false);

		Vector3 dropPosition = GlobalPosition + (Transform.Basis.Z * 2.5f);

		if (child is BaseWeapon weapon)
		{
			weapon.SetEnemyCollisionEnabled(false);

			weapon.CallDeferred("reparent", GetTree().Root);
			weapon.SetDeferred("global_position", dropPosition);
			weapon.SetDeferred("rotation", Vector3.Zero);

			if (weapon is Sword sword)
			{
				spinAttack.SetWeapon(null);
				sword.CheckDurability -= OnSwordDurabilityChecked;
			}
			else if (weapon is Crossbow bow)
			{
				bow.shootingDirectionMesh.Visible = false;

				if (bow.sourceBallista != null)
				{
					bow.sourceBallista.isDetached = false;
					bow.sourceBallista.ReattachHead(bow);
					bow.sourceBallista = null;
				}
			}
			currentWeapon.Broke -= OnSwordBroke;
			currentWeapon = null;

			GetTree().CreateTimer(0.1f).Timeout += () =>
			{
				areaPickup.SetDeferred("monitoring", true);

				if (IsInstanceValid(weapon)) weapon.ShowWeaponPrompt();
			};

			return;
		}

		var pickable = child as Pickable;
		if (pickable != null)
		{
			pickable.CallDeferred("reparent", GetTree().Root);
			GetTree().CreateTimer(0.1f).Timeout += () =>
			{
				if (IsInstanceValid(pickable))
					pickable.GlobalPosition = dropPosition;
				areaPickup.SetDeferred("monitoring", true);
			};
			pickable.SetDeferred("rotation", Vector3.Zero);
			return;
		}

		var node = child as Node3D;
		if (node != null)
		{
			node.CallDeferred("reparent", GetTree().Root);
			GetTree().CreateTimer(0.1f).Timeout += () =>
			{
				if (IsInstanceValid(node))
					node.GlobalPosition = dropPosition;
				areaPickup.SetDeferred("monitoring", true);
			};
			node.SetDeferred("rotation", Vector3.Zero);
		}
	}

	public void SetCurrentStation(BaseStationScript station)
	{
		currentStation = station;
		sequenceMinigame = station?.GetSequenceMinigame();
		barMinigame = station?.GetBarMinigame();
	}
	public void SetNearbyBallista(Ballista ballista) => nearbyBallista = ballista;

	private bool IsUsingController() => currentDevice >= 0;

	public void ApplyKnockback(Vector3 direction, float force)
	{
		knockback = direction * force;
	}

	private void Flash()
	{
		if (material == null) return;

		Color white = new Color(1, 1, 1);
		Color red = new Color(1, 0, 0);

		var tween = CreateTween();

		tween.TweenProperty(material, "emission_energy_multiplier", 2.0f, flashDuration / 4);
		tween.Parallel().TweenProperty(material, "emission", white, flashDuration / 4);

		tween.TweenProperty(material, "emission", red, flashDuration / 4);
		tween.TweenProperty(material, "emission_energy_multiplier", 0.0f, flashDuration / 2);
	}

	private void OnComboReset()
	{
		isAttacking = false;
		UpdateLocomotionAnim();
	}

	public void SetDazed(bool dazed)
	{
		isDazed = dazed;
	}

	// Called by SpinAttack to signal that the dazed sequence has started so
	// PlayerController knows not to interrupt it with Idle/Walk.
	public void OnDazedSequenceStarted()
	{
		isInDazedSequence = true;
	}

	// Called by SpinAttack once the GetUp animation has finished and the player
	// is fully recovered.
	public void OnDazedSequenceEnded()
	{
		isInDazedSequence = false;
		isAttacking = false;
		UpdateLocomotionAnim();
	}

	public void GiveItem(Pickable pickable)
	{
		ProcessPickable(pickable);
	}

	public void GiveWeapon(BaseWeapon weapon)
	{
		if (weapon.GetParent() != rightHand)
			weapon.Reparent(rightHand);

		// Apply the standard right-hand offset immediately since we're not going
		// through the deferred timer path here.
		weapon.Position = RightHandPosOffset;
		weapon.Rotation = RightHandRotOffset;
		weapon.Scale    = HandScale;

		UpdateAnimationTreacks(weapon.Name);
		PickUpWeapon(weapon);
	}

	private void UpdateAnimationTreacks(string nodeName)
	{
		foreach (var animName in animPlayer.GetAnimationList())
		{
			var anim = animPlayer.GetAnimation(animName);
			for (int i = 0; i < anim.GetTrackCount(); i++)
			{
				var path = anim.TrackGetPath(i).ToString();
				if (path.Contains("Sword") || path.Contains("DullSword"))
				{
					var newPath = System.Text.RegularExpressions.Regex.Replace(path, @"(Sword|DullSword)", nodeName);
					anim.TrackSetPath(i, newPath);
				}
			}
		}
	}

	public void DisconnectWeapon(Sword sword)
	{
		if (sword.IsConnected(Sword.SignalName.CheckDurability, Callable.From(OnSwordDurabilityChecked)))
			sword.Disconnect(Sword.SignalName.CheckDurability, Callable.From(OnSwordDurabilityChecked));
		if (sword.IsConnected(BaseWeapon.SignalName.Broke, Callable.From(OnSwordBroke)))
			sword.Disconnect(BaseWeapon.SignalName.Broke, Callable.From(OnSwordBroke));

		currentWeapon = null;
		isAttacking = false;
		UpdateLocomotionAnim();
	}
}
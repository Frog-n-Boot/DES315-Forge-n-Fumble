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
	[Export] private AudioStreamPlayer3D audio;

	private SequenceMinigame sequenceMinigame;

	[Export] private Node3D rightHand;
	[Export] private Node3D leftHand;
	[Export] private Area3D areaPickup;

	private Camera3D camera;
	private StaticBody3D world;
	private InputManager inputManager;
	public int currentDevice = -2;
	private HashSet<string> actionsPressed = new HashSet<string>();
	private HashSet<string> actionsPressedLastFrame = new HashSet<string>();
	private HashSet<Node> processPickable = new HashSet<Node>();

	private BaseStationScript currentStation;

	[Signal] public delegate void PlayerHealthChangedEventHandler(int current, int max);
	[Signal] public delegate void DiedEventHandler();
	Forge forge;
	HealthPack healthPack;

	private int _healthPack;
	private Vector3 currentLookTarget;
	private Sword currentSword;
	private bool isAttacking = false;
	private int tick = 0;
	private Pickable nearbyPickable;

	public IEnumerable<ItemData> GetCarriedItems()
	{
		if(leftHand.GetChildCount() > 0)
		{
			var pickable = leftHand.GetChild(0) as Pickable;
			if(pickable != null)
				yield return pickable.GetItemData();
		}

		if(rightHand.GetChildCount() > 0)
		{
			var pickable = rightHand.GetChild(0) as Pickable;
			if(pickable != null)
			{
				yield return pickable.GetItemData();
			}
		}
	}

	public void RemoveItem(ItemData item)
	{
		foreach(var hand in new[] { leftHand, rightHand })
		{
			if(hand.GetChildCount() == 0) continue;

			var pickable = hand.GetChild(0) as Pickable;
			if(pickable != null && pickable.GetItemData() == item)
			{
				pickable.QueueFree();
				return;
			}
		}
	}
	public override void _Ready()
{
	health = maxHealth;

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
	areaPickup = GetNodeOrNull<Area3D>("Area3D");
	areaPickup.AreaEntered += OnPickupAreaEntered;
	areaPickup.AreaExited += OnPickUpAreaExited;

	animPlayer.AnimationFinished += OnAnimationFinished;
	
	// ADD THIS: Find existing sword in hand
	rightHand = GetNodeOrNull<Node3D>("CollisionShape3D/RightHand");
	leftHand = GetNodeOrNull<Node3D>("CollisionShape3D/LeftHand");
	if (rightHand != null)
	{
		FindExistingSword();
	}
	else
	{
		GD.PrintErr("Hand node not found!");
	}
}


	private void FindExistingSword()
	{
		if (rightHand == null) return;
	
		foreach (Node child in rightHand.GetChildren())
		{
			if (child is Sword sword)
			{
				currentSword = sword;
				currentSword.CheckDurability += OnSwordDurabilityChecked;
				currentSword.Broke += OnSwordBroke;
				GD.Print($"Player {PlayerIndex} found existing sword in hand!");
				return;
			}
		}
	}

	// Called by PlayerSpawner after spawning — sets index, device and colour
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
		if(sequenceMinigame != null && sequenceMinigame.IsActiveFor(this))
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
		
		if (IsActionPressed("attack") && !isAttacking)
		{
			//GD.Print($"Player {PlayerIndex} attack input detected!");
			StartAttack();
			
		}
		if (IsActionPressed("interact") && currentStation != null)
		{
		   currentStation.StartCrafting();
		}
		if (IsActionPressed("pick_up") && nearbyPickable != null)
		{
			ProcessPickable(nearbyPickable);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if(sequenceMinigame != null && sequenceMinigame.IsActiveFor(this))
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
			velocity.X = inputDir.X * speed;
			velocity.Z = inputDir.Y * speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, speed);
		}

		Velocity = velocity;

		if (lookDir != Vector3.Zero)
		{
			float targetAngle = Mathf.Atan2(lookDir.X, lookDir.Z);
			Rotation = new Vector3(Rotation.X, targetAngle, Rotation.Z);
		}

		tick += 1;
		if(tick % 10 == 0)
		{
			if (health <= 0) Die();
		}

		MoveAndSlide();
	}

	public override void _Input(InputEvent @event)
{
	if (currentDevice == -2 || inputManager == null)
		return;

	bool isFromOurDevice = false;

	// KEYBOARD PLAYER
	if (currentDevice == -1 && (@event is InputEventKey || @event is InputEventMouseButton))
	{
		isFromOurDevice = true;
	}

	// CONTROLLER PLAYER
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

	if (IsActionJustPressed("drop_left"))
	{
		DropItem(leftHand);
	}
	if (IsActionJustPressed("drop_right"))
	{
		DropItem(rightHand);
	}

}


	private void StartAttack()
{
	//GD.Print($"Player {PlayerIndex} StartAttack called");
	//GD.Print($"currentSword is null: {currentSword == null}");
   // GD.Print($"animPlayer is null: {animPlayer == null}");
	//GD.Print($"isAttacking: {isAttacking}");
	
	if (currentSword == null)
	{
		GD.Print("No sword to attack with!");
		return;
	}

	isAttacking = true;
	currentSword.SetHitboxEnabled(true);
	animPlayer.Play("Anim_Attack");
	audio.Play();
	
	//GD.Print($"Player {PlayerIndex} attacking!");
}

	private void OnAnimationFinished(StringName animName)
	{
		if (animName == "Anim_Attack")
		{
			isAttacking = false;

			// Disable hitbox when swing is done
			if (currentSword != null)
				currentSword.SetHitboxEnabled(false);

			//GD.Print($"Player {PlayerIndex} attack finished.");
		}
	}

	// private void OnPickupBodyEntered(Node body)
	// {
	// 	if(body.IsInGroup("Player")) return;
	// 	if(processPickable.Contains(body)) return;

	// 	GD.Print($"BodyEntered: {body.Name} groups: {string.Join(", ", body.GetGroups())}");
	// 	if (body.IsInGroup("pickable"))
	// 	{
	// 		processPickable.Add(body);
	// 		ProcessPickable(body);
	// 	}
	  
		
	// }

	private void OnPickupAreaEntered(Area3D area)
	{
		GD.Print($"AreaEntered: {area.Name} groups: {string.Join(", ", area.GetGroups())}");

		if(area.GetGroups().Count == 0) return;

		if (area.IsInGroup("pickable"))
		{
			nearbyPickable = area.GetParent() as Pickable;
			GD.Print($"NearbyPickable: {nearbyPickable?.Name ?? "null"}");
		}
		


		bool hasSword = false;
		foreach (Node child in rightHand.GetChildren()){
			if (child is Sword || child.IsInGroup("Sword")){
			 hasSword = true;
			 
				break;
			}
		}

		if (hasSword)
			return;

		if (area.IsInGroup("Sword") && rightHand.GetChildCount() == 0)
		{
			StaticBody3D areaParent = area.GetParent() as StaticBody3D;
			Node3D swordNode = areaParent.GetParent() as Node3D;
			if(swordNode == null) return;

			Sword sword = swordNode as Sword;
			if(sword != null && sword.isBeingPickedUp) return;
			if(sword != null) sword.isBeingPickedUp = true;
			swordNode.CallDeferred("reparent", rightHand);
			GetTree().CreateTimer(0.1f).Timeout += () =>
			{
				if(!IsInstanceValid(swordNode)) return;
				swordNode.GlobalPosition = rightHand.GlobalPosition;
				swordNode.Rotation = Vector3.Zero;
				rightHand.Rotation = Vector3.Zero;
				
				currentSword = swordNode as Sword;
				if (currentSword != null)
				{
					currentSword.CheckDurability += OnSwordDurabilityChecked;
					currentSword.Broke += OnSwordBroke;
					currentSword.isBeingPickedUp = false;
				}
			};
		}
	}

	private void OnPickUpAreaExited(Area3D area)
	{
		if(area.IsInGroup("pickable"))
			nearbyPickable = null;
	}

	private void ProcessPickable(Pickable pickable)
	{
		if(!IsInstanceValid(pickable)) return;
		if(processPickable.Contains(pickable)) return;
		processPickable.Add(pickable);
		
		
			
		if(leftHand.GetChildCount() > 0)
		{
			processPickable.Remove(pickable);
			return;
		}

		pickable.CallDeferred("reparent", leftHand);

		GetTree().CreateTimer(0.1f).Timeout += () => {
			if (IsInstanceValid(pickable))
			{
				pickable.GlobalPosition = leftHand.GlobalPosition;
				pickable.Rotation = leftHand.Rotation;
			}
			processPickable.Remove(pickable);
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

	private bool IsActionPressed(string action)
	{
		return actionsPressed.Contains(action);
	}

	private bool IsActionJustPressed(string action)
	{
		return actionsPressed.Contains(action) && !actionsPressedLastFrame.Contains(action);
	}

	private bool IsActionJustReleased(string action)
	{
		return !actionsPressed.Contains(action) && actionsPressedLastFrame.Contains(action);
	}

	private void UpdatePlayerAppearance()
	{
		Color[] playerColors =
		{
			Colors.Blue,
			Colors.Red,
			Colors.Green,
			Colors.Yellow
		};

		Color playerColor = playerColors[PlayerIndex % playerColors.Length];

		var meshInstance = GetNodeOrNull<MeshInstance3D>("CollisionShape3D/MeshInstance3D");
		if (meshInstance == null)
		{
			GD.PrintErr($"Player {PlayerIndex}: MeshInstance3D not found at CollisionShape3D/MeshInstance3D");
			return;
		}

		meshInstance.MaterialOverride = new StandardMaterial3D
		{
			AlbedoColor = playerColor
		};

		GD.Print($"Set player {PlayerIndex} color to {playerColor}");
	}

	public void TakeDamage(int damage)
	{
		health -= damage;
		EmitSignal(SignalName.PlayerHealthChanged, health, maxHealth);
	}

	private void Die()
	{
		Visible = false;
		SetPhysicsProcess(false);
		SetProcess(false);

		GetNode<CollisionShape3D>("CollisionShape3D").SetDeferred("disabled", true);

		areaPickup.SetDeferred("monitoring", false);
		areaPickup.SetDeferred("monitorable", false);

		GetTree().CreateTimer(3.0f).Timeout += () =>{
			GlobalPosition = new Vector3(0, 1, 0);
			health = maxHealth;
			Visible = true;
			SetPhysicsProcess(true);
			SetProcess(true);

			GetNode<CollisionShape3D>("CollisionShape3D").SetDeferred("disabled", false);
			areaPickup.SetDeferred("monitoring", true);
			areaPickup.SetDeferred("monitorable", true);

			EmitSignal(SignalName.PlayerHealthChanged, health, maxHealth);
		};

	}

	private void OnSwordDurabilityChecked()
	{
		if (currentSword == null) return;
		//GD.Print($"Sword durability remaining: {currentSword.durability}");
	}

	private void OnSwordBroke()
	{
		//GD.Print("Sword broke! Auto crafting a new one if ingredients are available...");
		currentSword = null;
		isAttacking = false;
	}
	private void DropItem(Node3D hand)
	{
		if(hand.GetChildCount() == 0) return;

		var child = hand.GetChild(0);
		if(child == null || !IsInstanceValid(child)) return;

		areaPickup.SetDeferred("monitoring", false);

		Vector3 dropPosition = GlobalPosition + (Transform.Basis.Z * 2.5f);

		if(child is Sword sword)
		{
			sword.CallDeferred("reparent", GetTree().Root);
			sword.SetDeferred("global_position", dropPosition);
			sword.SetDeferred("rotation", Vector3.Zero);

			currentSword.CheckDurability -= OnSwordDurabilityChecked;
			currentSword.Broke -= OnSwordBroke;
			currentSword = null;
			GetTree().CreateTimer(0.1f).Timeout += () =>
				areaPickup.SetDeferred("monitoring", true);
			return;
		}

		var pickable = child as Pickable;
		if(pickable != null)
		{
			pickable.CallDeferred("reparent", GetTree().Root);
			GetTree().CreateTimer(0.1f).Timeout += () =>
			{
				if(IsInstanceValid(pickable))
					pickable.GlobalPosition = dropPosition;
				areaPickup.SetDeferred("monitoring", true);
			};
			pickable.SetDeferred("rotation", Vector3.Zero);
			return;
		}

		var node = child as Node3D;
		if(node != null)
		{
			node.CallDeferred("reparent", GetTree().Root);
			GetTree().CreateTimer(0.1f).Timeout += () =>
			{
				if(IsInstanceValid(node))
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
	}
}

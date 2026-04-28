using Godot;
using System;
using System.Collections.Generic;

public partial class TutorialManager : Node
{
	//[Export] EnemyController enemy;
	[ExportCategory("General")]
	[Export] private Marker3D[] roomSpawnPoints;
	[Export] private Marker3D[] cameraAnchorPoints;
	[Export] private CameraController cameraController; 
	[Export] private PauseMenu pauseMenu;
	[Export] private ProgressBar skipTutorialProgressBar;

	[ExportCategory("Room 1")]
	[Export] private SmeltingStation smeltingStation;
	[Export] private Node3D itemDropOffBox;

	[ExportCategory("Room 2")]
	[Export] private GrindstoneStation grindstoneStation;
	[Export] private ForgingStation forgingStation;
	[Export] private Node3D weaponDropOffBox;

	[ExportCategory("Room 3")]
	[Export]private EnemyController room3Enemy;

	[ExportCategory("Room 4")]
	[Export] private EnemyController room4Enemy;

	[Signal] public delegate void TutorialCompletedEventHandler();
	
	public int currentRoom {get; private set;}= 0;
	private int ingotsCrafted = 0;
	private int swordsCrafted = 0;
	private bool room3Completed = false;
	private bool room4Completed = false;

	private InputManager inputManager;

	private bool isCompletingRoom = false;
	private float  progressTime = 0;

	private bool isTutorialActive = true;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//smeltingStation.IngotCrafted += OnIngotCrafted;

		//grindstoneStation.SwordCrafted += OnSwordsCrafted;
		room3Enemy.Died +=OnRoom3EnemyKilled;
		room4Enemy.Died += OnRoom4EnemyKilled;
		skipTutorialProgressBar.MaxValue = 2;
		skipTutorialProgressBar.Value = 0;
		skipTutorialProgressBar.Visible = false;
		//enemy.Died += OnTutorialEnemyDied;
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionPressed("skip_tutorial"))
		{
			skipTutorialProgressBar.Visible = true;
			progressTime+= (float)delta;
			skipTutorialProgressBar.Value = progressTime;
			if(progressTime >= 2)
			{
				CompleteTutorial();
			}
		}
		else
		{
			progressTime = 0;
			skipTutorialProgressBar.Visible = false;
			skipTutorialProgressBar.Value = 0;
		}
		if(Input.IsActionJustReleased("pause"))
		{
			pauseMenu.TogglePause();
		}
	}

	private void OnDropOffAreaEntered(Area3D area)
	{
		if(currentRoom != 0) return;

		GD.Print($"Area entered {area.Name}");

		var pickable = area.GetParent() as Pickable;
		if(pickable != null && pickable.itemData != null && pickable.itemData.name.Contains("Ingot"))
		{
			GD.Print($"Ingot detected: {pickable.itemData.name}");

			if(!IsInstanceValid(pickable)) return;

			var itemParent = pickable.GetParent();
			bool enteringItemIsHeld = itemParent.Name.ToString().Contains("Hand") ||itemParent is PlayerController;

			if(enteringItemIsHeld) return;

			Area3D dropOffArea = itemDropOffBox.GetNode<Area3D>("Area3D");
			int ingotCount = 1;

			foreach(Area3D overlappingAreas in dropOffArea.GetOverlappingAreas())
			{
				if(overlappingAreas == area) continue;

				var item = overlappingAreas.GetParent() as Pickable;
				if(item != null && item.itemData != null && item.itemData.name.Contains("Ingot"))
				{
					var parent = item.GetParent();
					bool isInPlayerHand = parent.Name.ToString().Contains("Hand") || itemParent is PlayerController;
					if(!isInPlayerHand) ingotCount++;
				} 
			}

			GD.Print($"Total dropped ingots: {ingotCount}/3");
			if(ingotCount >= 3 && currentRoom == 0) CompleteRoom();	
		}
	}
	private void OnWeaponDropOffBodyEntered(Node3D body)
	{
		if(currentRoom != 1) return;

		GD.Print($"Body entered {body.Name}");

		var weapon = body.GetParent() as BaseWeapon;

		if(weapon == null) return;

		GD.Print($"Weapon detected: {weapon.Name}");

		var weaponParent = weapon.GetParent();
		bool isInPlayerHand = weaponParent.Name.ToString().Contains("Hand") || weaponParent is PlayerController;

		if(isInPlayerHand){
			GD.Print("Weapon is still in player's hand"); 
			return;
		}

		Area3D dropOffArea = weaponDropOffBox.GetNode<Area3D>("Area3D");
		int weaponCount = 1;

		foreach(Node3D overlappingBody in dropOffArea.GetOverlappingBodies())
		{
			if(overlappingBody == body) continue;

			var w = overlappingBody.GetParent() as BaseWeapon;
			if(w != null)
			{
				GD.Print("Weapon is not null");
				var parent = w.GetParent();
				bool isHeld = parent.Name.ToString().Contains("Hand") || parent is PlayerController;

				if(!isHeld) {
					weaponCount++;
					GD.Print($"Found dropped weapon: {w.Name}");
				}
			} 
		}

		GD.Print($"Total dropped weapons: {weaponCount}/2");

		if(weaponCount >= 2 && currentRoom == 1) CompleteRoom();	
		
	}

	private void OnDropOffBodyEntered(Node3D body)
	{
		if(currentRoom == 0)
		{
			GD.Print($"Item dropeed");
			if(body is Pickable pickable && pickable.Name =="Copper_Ingot")
			{
				GD.Print($"Item dropeed {pickable.Name}");
				Area3D dropOffArea = itemDropOffBox.GetNode<Area3D>("Area3D");
				int ingotCount = 0;

				foreach(Node3D node in dropOffArea.GetOverlappingBodies())
				{
					if(node is Pickable item && item.Name.ToString().Contains("Ingot")) ingotCount++;

				}

				if(ingotCount >= 3) CompleteRoom();
			}
		}

		if(currentRoom == 1)
		{
			if(body is BaseWeapon weapon)
			{
				GD.Print($"Weapon dropped {weapon.Name}");

				Area3D dropOffArea = weaponDropOffBox.GetNode<Area3D>("Area3D");
				int weaponCount = 0;

				foreach(Node3D node in dropOffArea.GetOverlappingBodies())
				{
					if(node is BaseWeapon w) weaponCount++;
				}

				if(weaponCount >= 2) CompleteRoom();
			}
		}
	}

	public void OnRoom3EnemyKilled(EnemyController enemy, Vector3 pos)
	{
		if(currentRoom != 2) return;
		CompleteRoom();
	}

	public void OnRoom4EnemyKilled(EnemyController enemy, Vector3 pos)
	{
		if(currentRoom != 3) return;
		CompleteTutorial();  
	}
	
	
	private void CompleteRoom()
	{
		if(isCompletingRoom) return;
		isCompletingRoom = true;
		
		GD.Print($"Room {currentRoom} complete");
		currentRoom++;
		isCompletingRoom = false;
		if(currentRoom == 4)
		{
			TeleportPlayers();
			return;
		}
		if(currentRoom > 4)
		{
			CompleteTutorial();
		}
		TeleportPlayers();
		
		
		
	}	

	private void TeleportPlayers()
	{
		int markerIndex = currentRoom - 1;
		if (markerIndex < 0 || markerIndex >= roomSpawnPoints.Length) return;

		foreach (Node n in GetTree().GetNodesInGroup("Player"))
		{
			if (n is PlayerController player)
				player.GlobalPosition = roomSpawnPoints[markerIndex].GlobalPosition;
		}

		if (cameraController != null && cameraAnchorPoints != null && markerIndex < cameraAnchorPoints.Length)
		{
			Vector3 anchorPos = cameraAnchorPoints[markerIndex].GlobalPosition;
			cameraController.cameraAnchor.GlobalPosition = anchorPos;
			cameraController.GlobalPosition = new Vector3(anchorPos.X, cameraController.GlobalPosition.Y, anchorPos.Z + cameraController.cameraZOffset);
		}
	}

	private bool completed = false;

	private void CompleteTutorial()
	{
		if(completed) return;
		completed = true;
		isTutorialActive = false;
		GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");

	}

	private void SkipTutorial() => CompleteTutorial();

	public bool IsTutorialActive() => isTutorialActive;
}

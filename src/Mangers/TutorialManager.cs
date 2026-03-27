using Godot;
using System;
using System.Collections.Generic;

public partial class TutorialManager : Node
{
	//[Export] EnemyController enemy;
	[Export] private Marker3D[] roomSpawnPoints;
	[Export] private SmeltingStation smeltingStation;
	[Export] private GrindstoneStation grindstoneStation;
	[Export] private ForgingStation forgingStation;
	[Export]private EnemyController room3Enemy;
	[Export] private EnemyController room4Enemy;
	[Signal] public delegate void TutorialCompletedEventHandler();
	
	private int currentRoom = 0;
	private int ingotsCrafted = 0;
	private int swordsCrafted = 0;
	private bool room3Completed = false;
	private bool room4Completed = false;

	private InputManager inputManager;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		smeltingStation.IngotCrafted += OnIngotCrafted;
		grindstoneStation.SwordCrafted += OnSwordsCrafted;
		room3Enemy.Died +=OnRoom3EnemyKilled;
		room4Enemy.Died += OnRoom4EnemyKilled;

		//enemy.Died += OnTutorialEnemyDied;
	}

	 public override void _Input(InputEvent @event)
    {
        if(@event.IsActionPressed("skip_room"))
			CompleteRoom();
    }  

	public void OnIngotCrafted()
    {
		if(currentRoom!= 0) return;
		ingotsCrafted++;
		GD.Print($"Ingots: {ingotsCrafted}/3");
		if(ingotsCrafted >= 3) CompleteRoom();
        
    }

	public void OnSwordsCrafted()
    {
		if(currentRoom!=1) return;
		swordsCrafted++;
		GD.Print($"Swords sharpened: {swordsCrafted}/1");
		if(swordsCrafted>=2) CompleteRoom();
        
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
		GD.Print($"Room {currentRoom} complete");
		currentRoom++;
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
		if(markerIndex <0 || markerIndex >= roomSpawnPoints.Length) return;

		foreach(Node n in GetTree().GetNodesInGroup("Player"))
		{
			if(n is PlayerController player)
				player.GlobalPosition = roomSpawnPoints[markerIndex].GlobalPosition;
		}
	}

	private bool completed = false;

	private void CompleteTutorial()
	{
		if(completed) return;
		completed = true;

		GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");

	}

	private void SkipTutorial() => CompleteTutorial();
}

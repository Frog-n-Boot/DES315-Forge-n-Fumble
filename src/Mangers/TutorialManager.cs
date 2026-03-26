using Godot;
using System;
using System.Collections.Generic;

public partial class TutorialManager : Node
{
	//[Export] EnemyController enemy;
	[Export] private Marker3D[] playerMarker;
	[Export] private SmeltingStation smeltingStation;
	[Export] private GrindstoneStation grindstoneStation;
	[Export]private EnemyController fisrtEnemy;
	[Export] private EnemyController secondEnemy;
	[Signal] public delegate void TutorialCompletedEventHandler();
	
	private int currentRoom = 0;
	private int ingotsCrafted = 0;
	private int swordsCrafted = 0;
	private int enemyBallistaKilled = 0;
	private int enemyKilled = 0;

	private InputManager inputManager;
	

	[Signal] public delegate void RoomCompletedEventHandler(int room);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		smeltingStation.IngotCrafted += OnIngotCrafted;
		grindstoneStation.SwordCrafted += OnSwordsCrafted;
		fisrtEnemy.TutorialEnemyBallistaKilled +=OnBallistaEnemyKilled;
		secondEnemy.TutorialEnemyKilled += OnEnemyKilled;

		//enemy.Died += OnTutorialEnemyDied;
	}

	 public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
			//CompleteTutorial();
           GetSpawnPoints();
        }
		if (@event.IsActionPressed("ui_up"))
        {
           currentRoom++;
        }	
    }  

	public void OnIngotCrafted()
    {
		GetTree().CreateTimer(1).Timeout += () =>{
            if(currentRoom != 0) return;
			ingotsCrafted++;
			if(ingotsCrafted >= 3) CompleteRoom();
        };
        
    }

	public void OnSwordsCrafted()
    {
        GetTree().CreateTimer(1).Timeout += () =>{
            if(currentRoom != 1) return;
			GD.Print("Signal R5ecieved");
			swordsCrafted++;
			GD.Print($"Sword craft counter: {swordsCrafted}");
			if(swordsCrafted >= 1) CompleteRoom();
        };
        
    }

	public void OnBallistaEnemyKilled()
    {
        if(currentRoom != 2) return;
		enemyBallistaKilled++;
		if(enemyBallistaKilled >= 1) CompleteRoom();
    }
	public void OnEnemyKilled()
    {
		CompleteTutorial();  
    }
    
	
	private void CompleteRoom()
    {
        EmitSignal(SignalName.RoomCompleted, currentRoom);
		
		
		if(currentRoom >= playerMarker.Length)
        {
            CompleteTutorial();
			return;
        }
		GetSpawnPoints();
		
		currentRoom++;
    }

	private bool completed = false;

	private void CompleteTutorial()
	{
		if(completed) return;
		completed = true;

		EmitSignal(SignalName.TutorialCompleted);

		GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");

	}

	private void GetSpawnPoints()
    {
		
		foreach(Node p in GetTree().GetNodesInGroup("Player"))
        {
            if(p is PlayerController player)			
				player.GlobalPosition = playerMarker[currentRoom ].GlobalPosition;
        }
		
    }
	private void OnTutorialEnemyDied(EnemyController enemy, Vector3 deathPosition) => CompleteTutorial();
		
}

using Godot;
using System;

public partial class TutorialManager : Node
{
	[Export] EnemyController enemy;
	[Signal] public delegate void TutorialCompletedEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		enemy.Died += OnTutorialEnemyDied;
	}

    public override void _Input(InputEvent @event)
    {
       if(@event.IsActionPressed("ui_cancel"))
			CompleteTutorial();
    }   
	
	private bool completed = false;
	private void CompleteTutorial()
	{
		if(completed) return;
		completed = true;

		EmitSignal(SignalName.TutorialCompleted);
		GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
	}
	private void OnTutorialEnemyDied(EnemyController enemy, Vector3 deathPosition) => CompleteTutorial();
		
}

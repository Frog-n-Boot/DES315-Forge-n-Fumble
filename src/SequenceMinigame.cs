using Godot;
using System;

public partial class SequenceMinigame : Node
{
	[Export] Label sequenceText;
	private string[] sequence;
	public bool isActive = false;
	private float timePerInput = 3.0f;
	private float timeLeft;
	private int currentStep = 0;
	private string[] possibleInputs = { "Left", "Right", "Up", "Down"};
	private PlayerController activePlayer;
	private int activeDevice;


	[Signal] public delegate void SequenceCompletedEventHandler();
	[Signal] public delegate void SequenceFailedEventHandler();
	
	public void Start(int length, PlayerController player)
	{
		activePlayer = player;
		activeDevice = player.currentDevice;
		sequence = new string[length];
		for(int i = 0; i < length; i++)
		{
			sequence[i] = possibleInputs[GD.Randi() % possibleInputs.Length];
			GD.Print($"Sequence is: ", sequence[i]);
		}
		currentStep = 0;
		timeLeft = timePerInput;
		isActive = true;
		UpdateLabel();
	}

	public override void _Process(double delta)
	{
		if(!isActive) return;

		timeLeft -= (float)delta;

		if(timeLeft <= 0)
		{
			sequenceText.Text = "";
			isActive = false;
			currentStep = 0;
			EmitSignal(SignalName.SequenceFailed);
		}
	}

	private void UpdateLabel()
	{
		bool isController = activeDevice >= 0;
		string display = "";
		for(int i = 0; i < sequence.Length; i++)
		{
			if(i == currentStep)
				display += $"[{GetArrow(sequence[i], isController)}] ";
			else if(i < currentStep)
				display += $"✔";
			else
				display += $"{GetArrow(sequence[i], isController)} ";
		}
		sequenceText.Text = display;

	}
	private string GetArrow(string input, bool isController) => input switch
	{
		"Left" => isController ? "D-LEFT" : "A",
		"Right" => isController ? "D-RIGHT" : "D",
		"Up" => isController ? "D-UP" : "W",
		"Down"=> isController ? "D-DOWN" : "S",
		_ => "?"
	};

	public override void _Input(InputEvent @event)
	{
		if(!isActive) return;

		string input = null;
		if(activeDevice == -1){
			if(@event is not InputEventKey) return;

			if(@event.IsActionPressed("move_left")) input = "Left";
			if(@event.IsActionPressed("move_right")) input = "Right";
			if(@event.IsActionPressed("move_up")) input = "Up";
			if(@event.IsActionPressed("move_down")) input = "Down";
			
		}
		else
		{
			if(@event is not InputEventJoypadButton) return;
			if(@event.IsActionPressed("sequence_left")) input = "Left";
			if(@event.IsActionPressed("sequence_right")) input = "Right";
			if(@event.IsActionPressed("sequence_up")) input = "Up";
			if(@event.IsActionPressed("sequence_down")) input = "Down";
		}


		if(input == null) return;

		CheckInput(input);
	}

	private void CheckInput(string input)
	{
		if(input == sequence[currentStep]){
			currentStep++;
			timeLeft = timePerInput;
			UpdateLabel();
			if(currentStep >= sequence.Length)
			{
				sequenceText.Text = "";
				EmitSignal(SignalName.SequenceCompleted);
				isActive = false;
			}
		}
		else
		{
			sequenceText.Text = "";
			EmitSignal(SignalName.SequenceFailed);
			isActive = false;
			currentStep = 0;
		}
	}

	public bool IsActiveFor(PlayerController player)
	{
		return isActive && activePlayer == player;
	}
	

}

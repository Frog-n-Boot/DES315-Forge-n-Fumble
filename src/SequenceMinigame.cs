using Godot;
using System;

public partial class SequenceMinigame : Node
{
	[Export] RichTextLabel sequenceText;
	[Export] Sprite3D sprite3D;
	[Export] AudioStreamPlayer3D audio;

	[Signal] public delegate void SequenceCompletedEventHandler();
	[Signal] public delegate void SequenceFailedEventHandler();

	private string[] sequence;
	public bool isActive = false;
	private float timePerInput = 3.0f;
	private float timeLeft;
	private int currentStep = 0;
	private string[] possibleInputs = { "Left", "Right", "Up", "Down"};
	private PlayerController activePlayer;
	private int activeDevice;
	private CameraController cameraController;


	
	public void Start(int length, PlayerController player)
	{
		cameraController = GetTree().Root.GetNode<CameraController>("TestingLab/Camera3D");
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

		bool isZommedOut = cameraController.GetCameraZoomOut();
		if (isZommedOut)
		{
			sprite3D.Scale = new Vector3(10, 10, 10);
		}
		else if(isZommedOut == false)
		{
			 sprite3D.Scale = new Vector3(5.39f, 4.0f, 4.0f);
		}

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
		bool isZommedOut = cameraController.GetCameraZoomOut();

		bool isController = activeDevice >= 0;
		string display = "";
		for(int i = 0; i < sequence.Length; i++)
		{
			if(i == currentStep)
				display += $"[color=yellow]{GetArrow(sequence[i], isController)}[/color]";
			else if(i < currentStep)
				display += $"[color=green]{GetArrow(sequence[i], isController)}[/color]";
			else
				display += $"[color=white]{GetArrow(sequence[i], isController)}[/color]";
		}
		sequenceText.Text = display;

	}
	private string GetArrow(string input, bool isController) => input switch
	{
		"Left" => isController ? "←" : "←",
		"Right" => isController ? "→" : "→",
		"Up" => isController ? "↑" : "↑",
		"Down"=> isController ? "↓" : "↓",
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
			audio.Play();
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

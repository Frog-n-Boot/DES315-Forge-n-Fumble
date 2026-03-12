using Godot;
using System;

public partial class TesultMenu : CanvasLayer
{
	[Export] private Label titleLabel;
	[Export] private Button retryButton;
	[Export] private Button mainMenuButton;

	public void ShowWin()
	{
		GetTree().Paused = true;
		titleLabel.Text = "            You Won!";
		Show();
		retryButton.GrabFocus();
	}

	public void ShowFail()
	{
		GetTree().Paused = true;
		titleLabel.Text ="Booo! Get out of here!!!!";
		Show();
		retryButton.GrabFocus();
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Hide();
		retryButton.Pressed += () => GetTree().ReloadCurrentScene();
		mainMenuButton.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");

	}
}

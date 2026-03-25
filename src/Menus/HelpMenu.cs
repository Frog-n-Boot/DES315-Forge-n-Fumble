using Godot;
using System;

public partial class HelpMenu : CanvasLayer
{
	[Export] public Button backButton;
	[Export] private PauseMenu pauseMenu;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Hide();
		backButton.Pressed +=OnBackButtonPressed;
	}

	public void OnBackButtonPressed()
	{
		Hide();
		pauseMenu.Show();
		pauseMenu.resumeButton.GrabFocus();
	}
}

using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	[Export] public Button resumeButton;
	[Export] Button settingsButton;
	[Export] Button mainMenuButton;
	//[Export] private CanvasLayer settingsMenu;
	[Export] private SettingsMenu settingsMenu;
	public override void _Ready()
	{
		Hide();
		settingsMenu.Hide();
		GetTree().Paused = false;
		resumeButton.Pressed += OnResumePressed;
		settingsButton.Pressed +=OnSettingsPressed;
		mainMenuButton.Pressed +=OnMainMenuPressed;

	}

    public override void _Input(InputEvent @event)
    {
        if(@event.IsActionPressed("pause"))
			TogglePause();
	}

	private void TogglePause()
	{
		if(GetTree().Paused)
			Resume();
        else
        {
			Pause();
        }
	}
	private void Pause()
	{
		Show();
		settingsMenu.Hide();
		resumeButton.GrabFocus();
		GetTree().Paused = true;
	}
	private void Resume()
	{
		Hide();
		GetTree().Paused = false;
	}

	public void OnResumePressed() => Resume();
	public void OnSettingsPressed(){
		settingsMenu.isInGame = true;
		settingsMenu.Show();
		settingsMenu.masterSlider.GrabFocus();
		Hide();
	}
	public void OnMainMenuPressed()
	{
		GetTree().Paused = false;
		SceneManager.instance.LoadMainMenu();
	}
}

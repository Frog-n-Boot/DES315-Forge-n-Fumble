using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	[Export] public Button resumeButton;
	[Export] Button settingsButton;
	[Export] Button mainMenuButton;
	//[Export] private CanvasLayer settingsMenu;
	[Export] private SettingsMenu settingsMenu;
	[Export] private TutorialManager tutorialManager;

	private Forge forge;
	public override void _Ready()
	{
		Hide();
		settingsMenu.Hide();
		GetTree().Paused = false;
		resumeButton.Pressed += OnResumePressed;
		settingsButton.Pressed +=OnSettingsPressed;
		mainMenuButton.Pressed +=OnMainMenuPressed;
		forge = GetTree().Root.GetNodeOrNull<Forge>("World/Forge");	

	}

	public override void _Input(InputEvent @event)
	{
		if(tutorialManager != null && tutorialManager.IsTutorialActive()) return;
		if(@event.IsActionPressed("pause"))
			TogglePause();
	}

	public void TogglePause()
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
		settingsMenu?.Hide();
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
		//GetTree().Paused = false;
		SceneManager.instance.LoadMainMenu();
	}

}

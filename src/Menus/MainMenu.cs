using Godot;
using System;

public partial class MainMenu : Control
{
	[Export] Button startButton;
	[Export] Button settingsButton;
	[Export] Button quitMenu;
	[Export] private AudioStreamPlayer2D audio;
	

	public override void _Ready()
	{
		startButton.GrabFocus();
	}

	public void OnStartButtonPressed(){
		audio.Play();
		GetTree().CreateTimer(0.3f).Timeout += () =>
		{
			SceneManager.instance.LoadGame(); 
		};
	}

	public void OnSettingsButtonPressed(){
		audio.Play();
		GetTree().CreateTimer(0.3f).Timeout += () =>
		{
			SceneManager.instance.LoadSettings();
		};
	}
	public void OnQuitButtonPressed(){
		audio.Play();
		GetTree().CreateTimer(0.3f).Timeout += () =>
		{
			SceneManager.instance.QuitGame();
		};
	} 


}

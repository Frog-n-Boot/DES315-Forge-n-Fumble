using Godot;
using System;

public partial class MainMenu : Control
{
	[Export] Button startButton;
	[Export] Button tutorialButton;
	[Export] Button settingsButton;
	[Export] Button quitMenu;
	[Export] private AudioStreamPlayer2D audio;
	[Export] private AudioStream menuMusic;
	[Export] private AudioStream levelMusic;
	

	public override void _Ready()
	{
		AudioManager.instance.PlayMusic(menuMusic);
		startButton.GrabFocus();
	}

	public void OnStartButtonPressed(){
		audio.Play();
		GetTree().CreateTimer(0.3f).Timeout += () =>
		{
			AudioManager.instance.PlayMusic(levelMusic);
			SceneManager.instance.LoadGame(); 
		};
	}
	public void OnTutorialButtonPressed(){
		audio.Play();
		GetTree().CreateTimer(0.3f).Timeout += () =>
		{
			SceneManager.instance.LoadTutorial(); 
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

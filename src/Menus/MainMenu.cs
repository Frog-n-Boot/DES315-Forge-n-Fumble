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
		//AudioManager.instance.PlayMusic(menuMusic);//
		startButton.GrabFocus();
		startButton.Modulate = new Color(0.8f, 0.8f, 0.8f, 1);
		tutorialButton.Modulate = new Color(0.8f, 0.8f, 0.8f, 1);
		settingsButton.Modulate = new Color(0.8f, 0.8f, 0.8f, 1);
		quitMenu.Modulate = new Color(0.8f, 0.8f, 0.8f, 1);
		
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
	public void OnStartButtonMouseEntered()
	{
		startButton.Modulate = new Color(1.25f, 1.25f, 1.25f, 1);
	}
	public void OnStartButtonMouseExited()
	{
		startButton.Modulate = new Color(0.8f, 0.8f, 0.8f, 1);
	}
	public void OnTutorialButtonMouseEntered()
	{
		tutorialButton.Modulate = new Color(1.25f, 1.25f, 1.25f, 1);
	}
	public void OnTutorialButtonMouseExited()
	{
		tutorialButton.Modulate = new Color(0.8f, 0.8f, 0.8f, 1);
	}
	public void OnSettingsButtonMouseEntered()
	{
		settingsButton.Modulate = new Color(1.25f, 1.25f, 1.25f, 1);
	}
	public void OnSettingsButtonMouseExited()
	{
		settingsButton.Modulate = new Color(0.8f, 0.8f, 0.8f, 1);
	}
	public void OnQuitButtonMouseEntered()
	{
		quitMenu.Modulate = new Color(1.25f, 1.25f, 1.25f, 1);
	}
	public void OnQuitButtonMouseExited()
	{
		quitMenu.Modulate = new Color(0.8f, 0.8f, 0.8f, 1);
	}


}

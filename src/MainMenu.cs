using Godot;
using System;

public partial class MainMenu : Control
{
	[Export] Button startButton;
	[Export] Button settingsButton;
	[Export] Button quitMenu;
	

	public override void _Ready()
    {
        startButton.GrabFocus();
    }

	public void OnStartButtonPressed() => SceneManager.instance.LoadGame();

	public void OnSettingsButtonPressed() => SceneManager.instance.LoadSettings();
	public void OnQuitButtonPressed() => SceneManager.instance.QuitGame();

}

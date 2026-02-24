using Godot;
using System;

public partial class MainMenu : Control
{
	[Export] Button startButton;
	[Export] Button settingsButton;
	[Export] Button quitMenu;
	[Export] Button backButton;
	[Export] CheckBox fullScreenCheckBox;
	

	public override void _Ready()
    {
        startButton.GrabFocus();
    }

	public void OnStartButtonPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/testing_lab.tscn");
    }
	public void OnSettingsButtonPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Settings.tscn");
    }
	public void OnQuitButtonPressed()
    {
        GetTree().Quit();
    }
	public void OnBackButtonPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }
	public void ToggleFullscreen()
    {
		GetWindow().Size = new Vector2I(1000, 1000);
		GetWindow().ContentScaleSize = new Vector2I(1000, 1000);
    }
}

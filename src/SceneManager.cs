using Godot;
using System;

public partial class SceneManager : Node
{
	public static SceneManager instance { get; private set;}
	public string previousScene{ get; private set;}


	public override void _Ready()
	{
		instance = this;
	}

	public void LoadScene(string path)
	{
		GetTree().ChangeSceneToFile(path);
	}

	public void LoadMainMenu() => LoadScene("res://scenes/MainMenu.tscn");
	public void LoadGame() => LoadScene("res://scenes/testing_lab.tscn");
	public void LoadSettings()
	{
		previousScene = GetTree().CurrentScene.SceneFilePath;
		LoadScene("res://scenes/Settings.tscn");
	}
	public void QuitGame() =>GetTree().Quit();

	public void GoBack()
	{
		if(!string.IsNullOrEmpty(previousScene))
			LoadScene(previousScene);
		else
			LoadMainMenu();
	}

}

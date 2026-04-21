using Godot;
using System;

public partial class SceneManager : Node
{
	public static SceneManager instance { get; private set;}
	public string previousScene{ get; private set;}

	private XRayManager xRayManager;
	public override void _Ready()
	{
		instance = this;
		
	}

	public void LoadScene(string path)
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile(path);
		
	}
	public void ReloadCurrentScene()
	{
		GetTree().Paused = false;
		GetTree().ReloadCurrentScene();
	}

	public void LoadMainMenu() => LoadScene("res://scenes/MainMenu.tscn");
	public void LoadGame(){
		GetTree().Paused = false;

		GetTree().CallGroup("Weapon", "queue_free");
		GetTree().CallGroup("DullSword", "queue_free");
		GetTree().CallGroup("Enemy", "queue_free");
		GetTree().CallGroup("pickable", "queue_free");

		var forge = GetNode<Forge>("/root/Forge");
		if( forge != null) forge.ResetHealth();

		LoadScene("res://scenes/Main.tscn");
	}
	public void LoadTutorial() => LoadScene("res://scenes/Tutorial.tscn");
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

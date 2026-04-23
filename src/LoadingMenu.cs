using Godot;
using System;
using System.Threading.Tasks;

public partial class LoadingMenu : Node2D
{
	[Export] private string scenePath;
	[Export] private TextureProgressBar textureProgressBar;
	[Export] private AudioStream levelMusic;
	//[Export] private PackedScene levelScene;
	private bool transfitioning = false;
	private bool sceneLoading =false;
	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{	
		ResourceLoader.LoadThreadedRequest(scenePath);
		sceneLoading = true;
	}

	public override void _Process(double delta)
	{
		if (sceneLoading)
		{
			Godot.Collections.Array progress = new Godot.Collections.Array();

			
			var status = ResourceLoader.LoadThreadedGetStatus(scenePath, progress);
	   		textureProgressBar.Value = (float)progress[0] * 100;

	 		
			if(status == ResourceLoader.ThreadLoadStatus.Loaded && !transfitioning)
			{	
				AudioManager.instance.PlayMusic(levelMusic);
				transfitioning = true;
				//await ToSignal(GetTree().CreateTimer(3.0f), "timeout");
				var loadedScene = (PackedScene)ResourceLoader.LoadThreadedGet(scenePath);
				GetTree().ChangeSceneToPacked(loadedScene);
				sceneLoading = false;
			}
		}   
	}
}

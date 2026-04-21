using Godot;
using System;

public partial class LoadingMenu : Node3D
{
	[Export] private string scenePath;
	[Export] private ProgressBar progressBar;
	[Export] private PackedScene levelScene;
	
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
			Godot.Collections.Array progress = [];

			
			var status = ResourceLoader.LoadThreadedGetStatus(scenePath, progress);
	   		progressBar.Value = (float)progress[0] * 100;

	 		
			if(status == ResourceLoader.ThreadLoadStatus.Loaded)
			{		
				GetTree().ChangeSceneToPacked(levelScene);
				sceneLoading = false;
			}
		}
		
	   
	   
	}
}

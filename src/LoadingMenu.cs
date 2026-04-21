using Godot;
using System;

public partial class LoadingMenu : Node2D
{
	[Export] private string scenePath;
	[Export] private TextureProgressBar textureProgressBar;
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
       		textureProgressBar.Value = (float)progress[0] * 100;

     		
			if(status == ResourceLoader.ThreadLoadStatus.Loaded)
            {		
				GetTree().ChangeSceneToPacked(levelScene);
				sceneLoading = false;
            }
        }
        
       
       
    }
}

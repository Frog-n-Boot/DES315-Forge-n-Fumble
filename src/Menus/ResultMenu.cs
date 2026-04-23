using Godot;
using System;

public partial class ResultMenu : CanvasLayer
{
	[Export] private Label titleLabel;
	[Export] private Button retryButton;
	[Export] private Button mainMenuButton;

	public void ShowWin()
	{
		GetTree().Paused = true;
		titleLabel.Text = "            You Won!";
		Show();
		retryButton.GrabFocus();
	}

	public void ShowFail()
	{
		GetTree().Paused = true;
		titleLabel.Text ="Booo! Get out of here!!!!";
		Show();
		retryButton.GrabFocus();
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		retryButton.Modulate = new Color(0.8f, 0.8f, 0.8f, 1);
		mainMenuButton.Modulate = new Color(0.8f, 0.8f, 0.8f, 1);
		
		Hide();
		retryButton.Pressed += () =>{
			GetTree().Paused = false;
			
			var forge = GetNode<Forge>("/root/Forge");
			if(forge != null)
			{
				forge.ResetHealth();
			}

			GetTree().CallGroup("Weapon", "queue_free");
			GetTree().CallGroup("DullSword", "queue_free");
			GetTree().CallGroup("Enemy", "queue_free");
			GetTree().CallGroup("pickable", "queue_free");
			
			GetTree().CreateTimer(0.5f).Timeout += () => GetTree().ReloadCurrentScene();
		};

		mainMenuButton.Pressed += () => {
			GetTree().Paused = false;

			GetTree().CallGroup("Weapon", "queue_free");
			GetTree().CallGroup("DullSword", "queue_free");
			GetTree().CallGroup("Enemy", "queue_free");
			GetTree().CallGroup("pickable", "queue_free");

			GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
		};

	}
	
	public void OnRetryButtonMouseEntered()
	{
		retryButton.Modulate = new Color(1.25f, 1.25f, 1.25f, 1);
	}
	public void OnRetryButtonMouseExited()
	{
		retryButton.Modulate = new Color(0.8f, 0.8f, 0.8f, 1);
	}
	
	public void OnMainMenuButtonMouseEntered()
	{
		mainMenuButton.Modulate = new Color(1.25f, 1.25f, 1.25f, 1);
	}
	public void OnMainMenuButtononMouseExited()
	{
		mainMenuButton.Modulate = new Color(0.8f, 0.8f, 0.8f, 1);
	}
	
}

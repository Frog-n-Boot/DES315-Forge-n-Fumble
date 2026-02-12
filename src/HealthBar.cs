using Godot;
using System;

public partial class HealthBar : ProgressBar
{
	private PlayerController player;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	
		base._Ready();
		player = GetNode<PlayerController>($"../../../");
		player.PlayerHealthChanged += OnHealthChange;

		MaxValue = 100;
		Value = 100;
	}

	private void OnHealthChange(int current, int max)
	{
		MaxValue = max;
		Value = current;
	}
}

using Godot;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;

public abstract partial class BaseStationScript : Node3D
{
	[Export] protected Node3D inputNode;
	[Export] protected Node3D outputNode;
	[Export] protected ProgressBar timeProgressBar;
	[Export] protected Label stationName;
	[Export] protected float craftDuration = 3.0f;
	[Export] protected CraftingRecipes[] recipes;

	protected PlayerController player;

	protected bool outputOccupied = false;

	protected Timer craftingTimer;

	protected ItemCarrier itemCarrier;
	
	protected CraftingRecipes pendingRecipe;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetupArea();
		SetupTimer();
		stationName.Text = GetStationName();
		OnReady();
	}

	protected virtual void OnReady(){}

    public override void _Process(double delta)
    {
        if(!craftingTimer.IsStopped() && !outputOccupied)
		{
			timeProgressBar.Value = (1.0 - craftingTimer.TimeLeft / craftDuration) * 100.0f;
		}
    }


	protected abstract string GetStationName();

	protected bool GetRequiredItems(out List<ItemData> itemsToConsume, out CraftingRecipes matchingRecipe)
	{
		itemsToConsume = new List<ItemData>();
		matchingRecipe = null;
		if(itemCarrier == null)
		{
			return false;
		}

		var inventoryItems = itemCarrier.GetCarriedItems();

		foreach(var recipe in recipes)
		{
			var found = new List<ItemData>();
			bool recipeMatched = true;
			foreach(var requiredName in recipe.requiredItemNames)
			{
				var match = inventoryItems.FirstOrDefault(i => i.name == requiredName && !found.Contains(i));
				if(match == null)
				{
					recipeMatched = false;
					break;
				}
				found.Add(match);
			}
			if (recipeMatched)
			{
				itemsToConsume = found;
				matchingRecipe = recipe;
				return true;
			}
		}

		return false;
	}

	protected void ProduceOutput()
	{
		if(pendingRecipe == null) return;

		var instance = pendingRecipe.ouputScene.Instantiate<Node3D>();

		if(!string.IsNullOrEmpty(pendingRecipe.ouputGroup))
			instance.AddToGroup(pendingRecipe.ouputGroup);

		GetTree().Root.AddChild(instance);
		instance.GlobalPosition = outputNode.GlobalPosition;
		timeProgressBar.Value = 0;
		outputOccupied = true;
		pendingRecipe = null;
		

	}
	protected virtual void OnCraftingRequirementsMet()
	{
		craftingTimer.Start();
	}
	public void StartCrafting()
	{
		if(outputOccupied || !craftingTimer.IsStopped() || itemCarrier == null)
		{
			return;
		}
		if(GetRequiredItems(out var itemsToConsume, out var recipes))
		{
			
			pendingRecipe = recipes;
			OnCraftingRequirementsMet();
		}
	}

	private void SetupArea()
	{
		if(inputNode == null || outputNode == null)
		{
			GD.Print($"{Name}: inputNode or outputNode is not assigned");
			return;
		}

		var inputArea = inputNode.GetNode<Area3D>("Area3D");
		if(inputArea != null)
		{
			inputArea.BodyEntered += OnInputBodyEntered;
			inputArea.BodyExited += OnInputBodyExited;
		}
		else
			GD.Print("Input are not found");
		

		var outputArea = outputNode.GetNode<Area3D>("Area3D");
		if(outputArea != null)
		{
			outputArea.BodyExited += OnOutputBodyExit;
		}
		else
			GD.Print("Output area not found");

	}
	private void SetupTimer()
	{
		craftingTimer = new Timer
		{
			WaitTime = craftDuration,
			OneShot = true,
			Name = "CraftingTimer"
		};
		AddChild(craftingTimer);
		craftingTimer.Timeout += OnCraftingTimerTimeout;

	}
	private void OnCraftingTimerTimeout()
	{
		if (!outputOccupied)
		{
			ProduceOutput();
		}
	}

	protected virtual void OnInputBodyEntered(Node3D body)
	{
		if (body.IsInGroup("Player"))
		{
			player = body as PlayerController;
			itemCarrier = player as ItemCarrier;
			StartCrafting();
		}
		
	}
	protected virtual void OnInputBodyExited(Node3D body)
	{
		if(body.IsInGroup("Player"))
			player.SetCurrentStation(null);
	}

	public void OnOutputBodyExit(Node3D body)
	{
		if (body.IsInGroup("Player")){
         	outputOccupied = false;
		 	StartCrafting();
		}
		
	}

	protected void SpawnAtOutput(PackedScene scene)
	{
		if(scene == null)
		{
			GD.Print("Scene is null");
			return;
		}

		var instance = scene.Instantiate<Node3D>();
		GetTree().Root.AddChild(instance);
		instance.GlobalPosition = outputNode.GlobalPosition;
		timeProgressBar.Value = 0;
		outputOccupied = true;
	}

	public virtual SequenceMinigame GetSequenceMinigame() => null;
}

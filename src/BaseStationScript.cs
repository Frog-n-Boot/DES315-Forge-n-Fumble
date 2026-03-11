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

	private List<ItemData> itemsToDeposit = new List<ItemData>();
	 private List<PlayerController> playersInZone = new List<PlayerController>();
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
        if(!craftingTimer.IsStopped() && !isOutputOccupied())
		{
			timeProgressBar.Value = (1.0 - craftingTimer.TimeLeft / craftDuration) * 100.0f;
		}
    }


	protected abstract string GetStationName();

	public bool DepositItems(ItemData item)
    {
        if (!IsItemNeeded(item))
            return false;
        ProduceOutput();
        itemsToDeposit.Add(item);
		StartCrafting();
		
		return true;
    }

	private bool IsItemNeeded(ItemData item)
    {
        foreach(var recipe in recipes)
			foreach(var requiredName in recipe.requiredItemNames)
				if(requiredName == item.name)
					return true;
		return false;
    }
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
			var usedIndices = new List<int>();
			var found = new List<ItemData>();
			bool recipeMatched = true;
			foreach(var requiredName in recipe.requiredItemNames)
			{
				bool matchFound = false;
				for(int i = 0; i < itemsToDeposit.Count; i++)
                {
                    if(itemsToDeposit[i].name == requiredName && !usedIndices.Contains(i))
                    {
                        usedIndices.Add(i);
						found.Add(itemsToDeposit[i]);
						matchFound = true;
						break;
                    }
                }

				if(!matchFound)
				{
					recipeMatched = false;
					break;
				}

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

		outputNode.AddChild(instance);
		instance.GlobalPosition = outputNode.GlobalPosition;
		timeProgressBar.Value = 0;
		pendingRecipe = null;

		CallDeferred(nameof(StartCrafting));
	}
	
	protected virtual void OnCraftingRequirementsMet()
	{
		GD.Print("Crafting requirements met, starting timer");
		craftingTimer.Start();
	}

	public void StartCrafting()
	{
		if(isOutputOccupied() || !craftingTimer.IsStopped())
		{
			return;
		}
		if(GetRequiredItems(out var itemsToConsume, out var recipes))
		{	

			pendingRecipe = recipes;
			OnCraftingRequirementsMet();
			foreach(var item in itemsToConsume)
				itemsToDeposit.Remove(item);
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
		if (!isOutputOccupied())
		{
			ProduceOutput();
		}
	}

	protected virtual void OnInputBodyEntered(Node3D body)
	{
        if (body.IsInGroup("Player"))
        {
            var p= body as PlayerController;
            itemCarrier = p as ItemCarrier;
            player = p;
            playersInZone.Add(p);
            p.SetCurrentStation(this);
        }
		
	}
	
	protected virtual void OnInputBodyExited(Node3D body)
	{
        if(body.IsInGroup("Player")){
            var p = body as PlayerController;
            playersInZone.Remove(p);
            p?.SetCurrentStation(null);

			if(player == p)
			{
				player = playersInZone.Count > 0 ? playersInZone[0] : null;
				itemCarrier = player as ItemCarrier;
			}
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

	protected bool isOutputOccupied()
	{
		foreach(Node3D child in outputNode.GetChildren())
		{
			if(child.IsInGroup("pickable") || child.IsInGroup("Weapon"))
				return true;
		}
		return false;
	}
	public virtual SequenceMinigame GetSequenceMinigame() => null;

	public void SetCraftDuration(float duration)
	{
		craftDuration = duration;
		craftingTimer.WaitTime = duration;
	}

}

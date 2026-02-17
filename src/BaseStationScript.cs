using Godot;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;

public partial class BaseStationScript : Node3D
{
	[Export] private Node3D inputNode;
	[Export] private Node3D outputNode;
	[Export] private bool isAnvil;
	[Export] public PackedScene swordObject { get; private set; }
	[Export]  private PackedScene meltedIngot;
	[Export] private ProgressBar timeProgressBar;
	[Export] private Label stationName;

	private double timeLeft = 3;
	private float timeDuration = 3;

	[Signal] public delegate void InputPortEventHandler();
	[Signal] public delegate void CheckItemsEventHandler(bool Check);

	[Signal] public delegate void CheckCraftingEventHandler();
	[Signal] public delegate void StartSmeltingEventHandler();
	[Signal] public delegate void StartCraftingEventHandler();

	InventorySystem inventory;
	protected PlayerController player;
	public List<ItemData> requiredItems = new List<ItemData>();

	Area3D inputArea;
	Area3D outputArea;

	private int totalStickCount = 0;
	private int totalIngotCount = 0;
	private int totalMeltedIngotCount = 0;

	private Timer smeltingTimer;
	private Timer forgingTimer;
	private bool smeltingOutputOccupied = false;
	private bool forgingOutputOccupied = false;
	private int tick = 0;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		inventory = GetNode<InventorySystem>("/root/InventorySystem");
		if(meltedIngot == null)
        {
            meltedIngot = GD.Load<PackedScene>("res://assets/models/MeltedIngot.tscn");
        }

		CheckCrafting += Craft;
        if (isAnvil)
        {
			
            StartCrafting += ForgeSword;
        }

		else
        {
            StartSmelting += Smelt;
        }

		SetupArea();
		SetupTimer();
		SetupUI();
	}

	private void SetupArea()
	{
		inputArea = inputNode.GetNode<Area3D>("Area3D");
		if(inputArea == null)
		{
			GD.Print("Area is null");
			return;
		}
		inputArea.BodyEntered += OnInputBodyEntered;

		outputArea = outputNode.GetNode<Area3D>("Area3D");
		if(outputArea == null)
		{
			GD.Print("Area is null");
			return;
		}
		outputArea.BodyExited += OnOutputBodyExit;

	}
	private void SetupTimer()
	{
		smeltingTimer = new Timer();
		smeltingTimer.WaitTime = 3.0f;
		smeltingTimer.OneShot = true;
		smeltingTimer.Name = "SmeltingTimer";
		AddChild(smeltingTimer);

		smeltingTimer.Timeout += OnSmeltTimerTimeout;

		forgingTimer = new Timer();
		forgingTimer.WaitTime = 3.0f;
		forgingTimer.OneShot = true;
		forgingTimer.Name = "ForgingTimer";
		AddChild(forgingTimer);

		forgingTimer.Timeout += OnForgeTimerTimeout;
	}

	private void SetupUI()
    {
        if (isAnvil)
        {
            stationName.Text = "Anvil";
        }
        else
        {
            stationName.Text = "Forge";
        }
    }
	private void OnSmeltTimerTimeout()
    {
		if(smeltingOutputOccupied == false)
        {
            Smelt();
        }
		//GetTree().CreateTimer(3.0f).Timeout += Smelt;
    	//EmitSignal(SignalName.CheckCrafting);
    }

	private void OnForgeTimerTimeout()
    {
		if(forgingOutputOccupied == false)
        {
            ForgeSword();
        }

		//GetTree().CreateTimer(3.0f).Timeout += ForgeSword;
    	//EmitSignal(SignalName.CheckCrafting);
    }

	public override void _Process(double delta)
    {
		if(forgingOutputOccupied == false)
        	{
            	if(forgingTimer != null && forgingTimer.IsStopped() == false)
        		{
					timeLeft -= delta;
					timeProgressBar.Value = (timeLeft / timeDuration) * 100.0;
        		}
       		}

		if(smeltingOutputOccupied == false)
       	{
            if(smeltingTimer != null && smeltingTimer.IsStopped() == false)
        	{
				timeLeft -= delta;
				timeProgressBar.Value = (timeLeft / timeDuration) * 100.0;
        	}
        }
    }

	private void Checkitems()
	{
		 if (inventory == null)
		{
		 	GD.Print("Inventory is null");
            return;
		}

		var items = inventory.GetItems();

		List<ItemData> itemsToRemove = new List<ItemData>();


		foreach(var item in items){
            if (isAnvil)
            {
                if(item.name == "Stick")
				{
					GD.Print("Stick was put in");
					requiredItems.Add(item);
					itemsToRemove.Add(item);
				}

				else if(item.name == "MeltedIngot")
				{
					GD.Print("Melted Ingot was put in");
					requiredItems.Add(item);
					itemsToRemove.Add(item);
				}
            }
            else
            {
            	if(item.name == "Ingot")
				{
					GD.Print("Ingot was put in");
					requiredItems.Add(item);
					itemsToRemove.Add(item);
				}
            }


		}

		foreach(var item in itemsToRemove)
		{
			inventory.RemoveItem(item);
		}

		Craft();

	}

	private void Craft()
	{
		if(requiredItems.Count <=0)
			return;

		ItemData stick = null;
        ItemData ingot = null;
		ItemData meltedOre = null;

		foreach(var item in requiredItems)
		{
			if(item.name == "Stick")
			{
				stick = item;
			}
			if(item.name == "Ingot")
			{
				ingot = item;
			}
			if(item.name == "MeltedIngot")
			{
				meltedOre = item;
				
			}
		}

        if (isAnvil)
        {
			if(stick != null && meltedOre != null)
            {
				forgingTimer.Start();		
				requiredItems.Remove(stick);
				requiredItems.Remove(meltedOre);
            }
        }

        else
        {
    		if(ingot != null)
        	{
				smeltingTimer.Start();
				//EmitSignal(SignalName.StartSmelting);		
				requiredItems.Remove(ingot);
        	}
        }
	}

	private void Smelt()
    {
		
        GD.Print("Smelting");
		Node3D droppedItem = null;
		droppedItem = meltedIngot.Instantiate<Node3D>();

		if(droppedItem != null)
		{
			smeltingTimer.Stop();
			timeProgressBar.Value = 0;
			timeLeft = 3;
			GetTree().Root.AddChild(droppedItem);
			droppedItem.GlobalPosition = outputNode.GlobalPosition;
			smeltingOutputOccupied = true;
		}
    }

	private void ForgeSword()
    {
		Node3D swordNode = swordObject.Instantiate<Node3D>();
        swordNode.AddToGroup("Sword");
		GetTree().Root.AddChild(swordNode);
		//player.AddChild(swordNode);
		//swordNode.Reparent(player);
		swordNode.GlobalPosition = outputNode.GlobalPosition;

		GD.Print("Sword was crafted");
		forgingTimer.Stop();
		timeProgressBar.Value = 0;
		timeLeft = 3;
		forgingOutputOccupied = true;
    }

	public void OnInputBodyEntered(Node3D body)
	{
		if (body.IsInGroup("Player"))
		{
			player = body as PlayerController;
			Checkitems();
		}
		
	}

	public void OnOutputBodyExit(Node3D body)
	{
		if (body.IsInGroup("Player")){
            if (isAnvil)
            {
                forgingOutputOccupied = false;
            }
            else
            {
                smeltingOutputOccupied = false;
            }
		}
		
	}
}

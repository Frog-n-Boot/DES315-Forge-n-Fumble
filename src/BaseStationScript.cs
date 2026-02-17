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
	[Export] public PackedScene swordObject { get; private set; }
	[Export] private Label stickCount;
	[Export] private Label ingotCount;

	[Signal] public delegate void InputPortEventHandler();
	[Signal] public delegate void CheckItemsEventHandler(bool Check);

	InventorySystem inventory;
	protected PlayerController player;
	public List<ItemData> requiredItems = new List<ItemData>();

	Area3D area;

	private int totalStickCount = 0;
	private int totalIngotCount = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		inventory = GetNode<InventorySystem>("/root/InventorySystem");
		SetupArea();
	}

	private void SetupArea()
	{
		area = inputNode.GetNode<Area3D>("Area3D");
		if(area == null)
		{
			GD.Print("Area is null");
			return;
		}
		area.BodyEntered += OnBodyEntered;

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
			if(item.name == "Stick")
			{
				GD.Print("Stick was put in");
				requiredItems.Add(item);
				var stickItem = requiredItems.Find(item => item.name == "Stick");
				inventory.RemoveItem(item);
				stickItem.count += 1;
				itemsToRemove.Add(item);
				totalStickCount += stickItem.count;
				stickCount.Text = $"{totalStickCount}";
			}
			else if(item.name == "Ingot")
			{
				GD.Print("Ingot was put in");
				requiredItems.Add(item);
				var ingotItem = requiredItems.Find(item => item.name == "Ingot");
				inventory.RemoveItem(item);
				ingotItem.count += 1;
				itemsToRemove.Add(item);
				totalIngotCount += ingotItem.count;
				ingotCount.Text = $"{totalIngotCount}";
			}
		}

		//UpdateItemsUI();

		foreach(var item in itemsToRemove)
		{
			inventory.RemoveItem(item);
		}

		//Craft();

	}

	private void UpdateItemsUI()
	{
		// int totalStickCount = 0;
		// int totalIngotCount = 0;

		// foreach(var item in requiredItems)
		// {
		// 	if(item.name == "Stick")
		// 	{
		// 		totalStickCount += item.count;
		// 	}
		// 	else if(item.name == "Ingot")
		// 	{
		// 		totalIngotCount += item.count;
		// 	}
		// }

		// stickCount.Text = $"{totalStickCount}";
		// ingotCount.Text = $"{totalIngotCount}";
	}
	private void Craft()
	{
		if(requiredItems.Count <=0)
			return;

		ItemData stick = null;
        ItemData ingot = null;

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
		}

		if(stick != null && ingot != null)
		{
			Node3D swordNode = swordObject.Instantiate<Node3D>();
            swordNode.AddToGroup("Sword");
			GetTree().Root.AddChild(swordNode);
			//player.AddChild(swordNode);
			//swordNode.Reparent(player);
			swordNode.GlobalPosition = outputNode.GlobalPosition;


			requiredItems.Remove(stick);
			requiredItems.Remove(ingot);

			GD.Print("Sword was crafted");
		}
	}
	public void OnBodyEntered(Node3D body)
	{
		if (body.IsInGroup("Player"))
		{
			player = body as PlayerController;
			Checkitems();
			EmitSignal(SignalName.CheckItems, true);

		}
		
	}
}

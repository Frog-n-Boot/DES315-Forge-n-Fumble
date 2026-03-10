using Godot;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

[GlobalClass]
public partial class Pickable : Node3D
{

	#region Variables
	[Export(PropertyHint.File, "*.tres")] public string itemDataPath = "";

	public ItemData itemData;
	[ExtenderProvidedProperty] public float highLightDistance = 5.0f;

	[Signal] public delegate void PickedUpEventHandler();

	#endregion

	#region Ready

	public void Initialize(ItemData itemdata)
	{
		itemData = itemdata;
	}
	public override void _Ready()
	{
		if (!string.IsNullOrEmpty(itemDataPath))
		{
			itemData = GD.Load<ItemData>(itemDataPath);
		}
		else
		{
			GD.Print("Failed to load resource");
		}

		if(itemData == null)
		{
			GD.Print("Failed to load path");
			return;
		}
	}
	
	#endregion

	#region GetItemData
	public ItemData GetItemData()
	{
		return itemData;
	}
	#endregion

	#region PickUp
	public void PickUp()
	{
		EmitSignal(SignalName.PickedUp);
		//QueueFree();
	}
	#endregion

	
	public void Highlight(bool enabled)
	{
		
	}


}

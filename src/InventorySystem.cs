using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class InventorySystem : Node
{
	#region Variables
	[Signal] public delegate void InventoryUpdateEventHandler();

	[Export] public int maxSlots = 4;

	public List<ItemData> items = new List<ItemData>();

	#endregion

	#region AddItem
	public bool AddItem(ItemData item)
	{
		//Checks if item is null
		if(item == null)
		{
			GD.Print("Item is null");
			return false;
		}

		//check if the first element's name in items is the same as the item name
		ItemData existingItem = items.FirstOrDefault(i => i.name == item.name);

		//if the item is not null Emit the signal and add onto the item's count
		if(existingItem != null)
		{
			existingItem.count++;
			EmitSignal(SignalName.InventoryUpdate);
			return true;
		}

		else
		{
			// inventory is full
			if (maxSlots > 0 && items.Count >= maxSlots)
			{
				GD.Print("Inventory is full");
				return false;
			}

			//Set item count to 1 aqnd add item into the items array
			item.count = 1;
			items.Add(item);
			EmitSignal(SignalName.InventoryUpdate);
			return true;
		}

	}
	#endregion

	#region RemoveItem

	/*Checks if the item count is bigger and remove the passed item */
	public bool RemoveItem(ItemData item)
	{
		if(item.count > 0)
		{
			item.count --;

			if(item.count == 0)
			{
				items.Remove(item);
			}
			return true;
		}
		return false;
	}
	#endregion

	#region GetItem

	//List of items
	public List<ItemData> GetItems()
	{
		return new List<ItemData>(items);
	}
	#endregion

	#region HasItem

	//Check sif it has the item
	public bool HasItem(ItemData item)
	{
		return items.Contains(item);
	}
	#endregion

	#region Clear

	//Clears the list of items
	public void Clear()
	{
		items.Clear();
		EmitSignal(SignalName.InventoryUpdate);
	}
	#endregion
}

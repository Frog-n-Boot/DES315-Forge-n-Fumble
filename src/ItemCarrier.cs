using Godot;
using System.Collections.Generic;
using System;

public interface ItemCarrier
{
	IEnumerable<ItemData> GetCarriedItems();
	void RemoveItem(ItemData item);
}

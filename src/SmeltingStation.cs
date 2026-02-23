using Godot;
using System;
using System.Collections.Generic;

public partial class SmeltingStation : BaseStationScript
{
	[Export] private PackedScene meltedIngotScene;

	protected override void OnReady()
    {
       	if(meltedIngotScene == null)
        {
            meltedIngotScene = GD.Load<PackedScene>("res://assets/models/MeltedIngot.tscn");
        }
    }

    protected override void OnCraftingRequirementsMet()
    {
        if(GetRequiredItems(out var items, out var recipe))
        {
            foreach(var item in items)
                itemCarrier.RemoveItem(item);
            
            craftingTimer.Start();
        }
    }

    protected override string GetStationName() => "Forge";
}

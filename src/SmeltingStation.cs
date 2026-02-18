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

    protected override string GetStationName() => "Forge";
}

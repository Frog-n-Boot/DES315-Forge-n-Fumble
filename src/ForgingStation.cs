using Godot;
using System;
using System.Collections.Generic;

public partial class ForgingStation : BaseStationScript
{
	[Export] public PackedScene dullSwordScene {get; private set;}

	protected override void OnReady()
    {
       	if(dullSwordScene == null)
        {
            dullSwordScene = GD.Load<PackedScene>("res://assets/models/MeltedIngot.tscn");
        }
    }

    protected override string GetStationName() => "Anvil";

}

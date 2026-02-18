using Godot;
using System;
using System.Collections.Generic;

public partial class GrindstoneStation : BaseStationScript
{
	[Export] public PackedScene swordScene {get; private set;}
	
	protected override void OnReady()
    {
       	if(swordScene == null)
        {
            swordScene = GD.Load<PackedScene>("res://assets/models/MeltedIngot.tscn");
        }
    }

    protected override string GetStationName() => "Grindstone";

}

#if TOOLS
using Godot;
using System;

public partial class FixResources : EditorScript
{
	public override void _Run()
    {
        var healthPack = new ItemData();
        healthPack.name = "HealthPack";
        ResourceSaver.Save(healthPack, "res://assets/resources/HealthPack.tres");

        var ingot = new ItemData();
        ingot.name = "Ingot";
        ResourceSaver.Save(ingot, "res://assets/resources/Ingot.tres");

        var stick = new ItemData();
        stick.name = "Stick";
        ResourceSaver.Save(stick,"res://assets/resources/Stick.tres");

		GD.Print("Resources created successfully ");
    }
}
#endif
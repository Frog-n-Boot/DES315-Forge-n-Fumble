using Godot;
using System;

[Tool]
public partial class FixResources : EditorScript
{
	public override void _Run()
    {
        var healthPack = new ItemData();
		healthPack.name = "HealthPack";
		ResourceSaver.Save(healthPack, "res://Resources/HealthPack.tres");

		var ingot = new ItemData();
		ingot.name = "Ingot";
		ResourceSaver.Save(ingot, "res://Resources/Ingot.tres");

		var stick = new ItemData();
		stick.name = "Stick";
		ResourceSaver.Save(stick,"res://Resources/Stick.tres");

		GD.Print("Resources created successfully ");
    }
}

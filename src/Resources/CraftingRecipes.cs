using Godot;
using System;

[GlobalClass]
public partial class CraftingRecipes : Resource
{
    [Export] public string recipeName;
    [Export] public string[] requiredItemNames;
    [Export] public string ouputName;
    [Export] public PackedScene ouputScene;
    [Export] public string ouputGroup = "";

}

using Godot;
using System;

[GlobalClass]
public partial class ItemData : Resource
{
    [Export] public string name {get; set;} = "";
    [Export] public int count {get; set;} = 0;
    [Export] public PackedScene itemScene;
    [Export] public Texture2D itemTexture;
    
}

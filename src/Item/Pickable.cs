using Godot;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

[GlobalClass]
public partial class Pickable : Node3D
{

	#region Variables
	[Export(PropertyHint.File, "*.tres")] public string itemDataPath = "";
	
	[Export] public bool shouldDespawn = false;
	[Export] public float despawnTime = 30f;
	[Export] public Texture2D[] itemPromptTexture;
	[Export] public TextureRect itemTextureRect;

	public ItemData itemData;
	[ExtenderProvidedProperty] public float highLightDistance = 5.0f;

	[Signal] public delegate void PickedUpEventHandler();

	#endregion

	#region Ready

	public void Initialize(ItemData itemdata)
	{
		itemData = itemdata;
		
	}
	public override void _Ready()
	{
		if (!string.IsNullOrEmpty(itemDataPath))
		{
			itemData = GD.Load<ItemData>(itemDataPath);
		}
		else
		{
			GD.Print("Failed to load resource");
		}

		if(itemData == null)
		{
			GD.Print("Failed to load path");
			return;
		}
		if(shouldDespawn)
			GetTree().CreateTimer(despawnTime).Timeout += QueueFree;
	}
	
	#endregion

	#region GetItemData
	public ItemData GetItemData()
	{
		return itemData;
	}
	#endregion

	#region PickUp
	public void PickUp()
	{
		//itemTextureRect.Hide();
		EmitSignal(SignalName.PickedUp);
		//QueueFree();
	}
	#endregion

	public void SetItemPromptTexture(bool isController)
	{
		//itemTextureRect.Show();
		itemTextureRect.Texture = isController? itemPromptTexture[0] : itemPromptTexture[1];
	}
	public void HideItemPrompt()
	{
		if(itemTextureRect != null)
			itemTextureRect.Hide();
		
	}
	public void Highlight(bool enabled)
	{
		
	}


}

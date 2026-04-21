using Godot;
using System;

public abstract partial class BaseWeapon : Node3D
{
	[Export] public int damage;
	[Export] public int maxDurability;
	[Export] public Texture2D weaponTexture;
	[Export] public Color weaponTint = new Color(1, 1, 1, 1);
	[Export] public Texture2D[] weaponPromptTexture;
	[Export] public TextureRect weaponTextureRect;
	[Export] public Area3D pickUpArea;
	[Export] private StaticBody3D weaponBody;
	
	public int durability;
	public bool isBeingPickedUp = false;
	private int lastHitDamage;
	public bool isCarried = false;
	public bool IsCarried() => isCarried;

	

	[Signal] public delegate void BrokeEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		durability = maxDurability;
		lastHitDamage = damage;

		SetEnemyCollisionEnabled(false);	
		OnReady();
	}

	protected virtual void OnReady() {}

	public virtual void TakeDurabilityDamage(int amount)
	{
		durability -= amount;
		//EmitSignal(SignalName.DurabilityChanged, durability, maxDurability);
		
		if(durability <= 0)
		{
			durability = 0;
			damage = lastHitDamage;
			OnBroke();
		}
	}

	
	protected virtual void OnBroke()
	{
		EmitSignal(SignalName.Broke);
	}
	public abstract void Use();

	public void SetWeaponPromptTexture(bool isController)
	{
		if(isCarried) return;
		weaponTextureRect.Show();
		weaponTextureRect.Texture = isController? weaponPromptTexture[0] : weaponPromptTexture[1];
	}
	public void HideWeaponPrompt()
	{
		if(weaponTextureRect != null)
			weaponTextureRect.Hide();

		if(pickUpArea != null)
			pickUpArea.Monitoring = false;

	}

	public void ShowWeaponPrompt()
	{
		weaponTextureRect.Show();
		if(pickUpArea != null) pickUpArea.Monitoring = true;
	}
	public void SetEnemyCollisionEnabled(bool enabled)
	{
		if(weaponBody == null) return;
		
		if(enabled) {
			weaponBody.AddToGroup("Weapon");
		}
		else
		{
			weaponBody.RemoveFromGroup("Weapon");
		}
		

		//weaponBody.SetCollisionLayerValue(4, enabled);
		GD.Print($"{Name} enemy collision: {enabled}, In Weapon group: {IsInGroup("Weapon")}");
	}
}

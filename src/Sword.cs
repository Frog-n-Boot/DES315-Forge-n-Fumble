using Godot;
using System;

public partial class Sword : MeleeWeapon
{
	[Export] public PackedScene sharpenedVersion;
	[Signal] public delegate void CheckDurabilityEventHandler();
	[Signal] public delegate void ComboResetEventHandler();
	private int comboCount = 0;
	private const int maxCombo = 3;
	private float comboResetTimer = 0f;
	private const float comboWindow = 1.0f;
	private int comboDamage;

	protected override void OnReady()
	{
		base.OnReady();
		durability = maxDurability;
		comboDamage = damage;
	}

	public override void _Process(double delta)
	{
		if(comboResetTimer > 0)
		{
			comboResetTimer -= (float)delta;
			if(comboResetTimer <=0 && comboCount > 0)
			{
				comboCount = 0;
				EmitSignal(SignalName.ComboReset);
			}
				
		}
	}

	public bool PerformComboAttack(InputBuffer buffer)
	{
		if (buffer.ConsumeInput("attack"))
		{
			comboCount++;
			if(comboCount > maxCombo)
				comboCount = 1;

			comboResetTimer = comboWindow;
			comboDamage = damage;
			return true;			
		}

		return false;
	}

	public string GetComboAnimation()
	{
		return comboCount switch
		{
			1 => "Sword_Attack_1",
			2 => "Sword_Attack_2",
			3 => "Sword_Attack_3",
			_ => "Sword_Idle"
		};
	}
	public int GetComboDamage()
	{
		int baseDamage = comboCount switch
		{
			1 => damage,
			2 => damage + 1,
			3 => damage + 3,
			_ => damage
		};
		return durability == 1 ? baseDamage * 3: baseDamage;
	}
	public void DamageWeapon(int amount)
	{
		durability -= amount;
		EmitSignal(SignalName.CheckDurability);
		TakeDurabilityDamage(0);
	}
	
}

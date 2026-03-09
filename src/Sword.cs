using Godot;
using System;

public partial class Sword : MeleeWeapon
{

	[Signal] public delegate void CheckDurabilityEventHandler();
	[Signal] public delegate void ComboResetEventHandler();
	private int comboCount = 0;
	private const int maxCombo = 3;
	private float comboResetTimer = 0f;
	private const float comboWindow = 1.0f;

	protected override void OnReady()
	{

		base.OnReady();
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
		return comboCount switch
		{
			1 => 1,
			2 => 2,
			3 => 20,
			_ => 1
		};
	}
	public void DamageWeapon(int amount)
	{
		durability -= amount;
		EmitSignal(SignalName.CheckDurability);
		TakeDurabilityDamage(0);
	}
}

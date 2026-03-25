using Godot;
using System;
using System.Collections.Generic;

public partial class InputBuffer : Node
{
	[Export] public float bufferDuration = 0.2f;

	private Dictionary<string, float> bufferedInputs = new Dictionary<string, float>();

	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		List<string> keysToRemove = new List<string>();

		foreach(var key in keysToRemove)
		{
			bufferedInputs[key] -= (float)delta;
			if(bufferedInputs[key] <= 0)
				keysToRemove.Add(key);
		}

		foreach(var key in keysToRemove)
			bufferedInputs.Remove(key);
	}

	public void BufferInput(string inputName) => bufferedInputs[inputName] = bufferDuration;

	public bool IsInputBuffered(string inputName)
	{
		return bufferedInputs.ContainsKey(inputName) && bufferedInputs[inputName] > 0;
	}

	public bool ConsumeInput(string inputName)
	{
		if (IsInputBuffered(inputName)){
			bufferedInputs.Remove(inputName);
			return true;
		}
		return false;
	}

	public void ClearInput(string inputName)
	{
		if(bufferedInputs.ContainsKey(inputName))
			bufferedInputs.Remove(inputName);
	}

	public void ClearAll() => bufferedInputs.Clear();

}

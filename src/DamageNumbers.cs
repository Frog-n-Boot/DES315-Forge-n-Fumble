using Godot;
using System;

public partial class DamageNumbers
{
	public static void Spawn(int damage, Vector3 position, Node parent)
	{
		var label = new Label3D();
		label.Text = damage.ToString();
		label.FontSize = 128;
		label.Modulate = Colors.Red;
		label.Position = position + Vector3.Up;
		parent.AddChild(label);

		var tween = label.CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(label, "position", label.Position + Vector3.Up * 2f, 1f);
		tween.TweenProperty(label, "modulate:a", 0f, 1f);
		tween.Chain().TweenCallback(Callable.From(label.QueueFree));
	}
}

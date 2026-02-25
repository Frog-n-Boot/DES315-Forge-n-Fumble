using Godot;
using System;

public partial class CollissionHandlerComponent : Node
{
	[Signal] public delegate void CollisionDetectedEventHandler(Node3D body, string groupName);
	[Export] public float collisionCooldown { get; set;} = 0.01f;

	private bool canCollide = true;

	public void InitializeArea(Area3D area)
    {
        area.BodyEntered += OnBodyEntered;

    }
	private void OnBodyEntered(Node3D body)
    {
        if(!canCollide) return;

		canCollide = false;

		string group = GetBodyGroup(body);
        if (!string.IsNullOrEmpty(group))
        {
            EmitSignal(SignalName.CollisionDetected, body, group);
        }
		GetTree().CreateTimer(collisionCooldown).Timeout += () => canCollide = true;
    }

	private string GetBodyGroup(Node3D body)
    {
        if(body.IsInGroup("Player")) return "Player";
		if(body.IsInGroup("Forge")) return "Forge";
		if(body.IsInGroup("Sword")) return "Sword";
		if(body.IsInGroup("Enemy")) return "Enemy";
        if(body.IsInGroup("Bullet")) return "Bullet";
		return "";
    }
}

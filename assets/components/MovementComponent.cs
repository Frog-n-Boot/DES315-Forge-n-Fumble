using Godot;
using System;

public partial class MovementComponent : Node
{
	[Export] public float speed {get; set;} = 5.0f;
	[Export] public float rotationSpeed {get; set;} = 10.0f;

	private Vector3 velocity = Vector3.Zero;

	public void MoveTowards(Node3D mover, Vector3 targetPosition, double delta)
    {
        Vector3 direction = (targetPosition - mover.GlobalPosition).Normalized();
		velocity = direction*speed;
		mover.GlobalPosition += velocity * (float)delta;

		if(direction.LengthSquared() > 0.01f)
        {
            RotateTowards(mover, direction, delta);
        }
    }
	public void MoveInDirection(Node3D mover, Vector3 direction, double delta)
    {
        velocity = direction.Normalized() * speed;
		mover.GlobalPosition += velocity * (float)delta;
    }
	private void RotateTowards(Node3D mover, Vector3 direction, double delta)
    {
        Vector3 targetRotation = new Vector3(0, Mathf.Atan2(direction.X, direction.Z), 0);
		mover.Rotation = mover.Rotation.Lerp(targetRotation, rotationSpeed * (float)delta);
    }

	public Vector3 GetVelocity() => velocity;
}

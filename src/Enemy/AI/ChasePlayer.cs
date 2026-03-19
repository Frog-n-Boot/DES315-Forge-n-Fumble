using Godot;

[GlobalClass]
public partial class ChasePlayer : EnemyBehaviourData
{
    public override bool CanActivate(EnemyController c, float healthPercent)
        => c.Player != null &&
           c.GlobalPosition.DistanceTo(c.Player.GlobalPosition) <= DetectionRange;

    public override void Activate(EnemyController c)
    {
        if (c.CurrentState is not EnemyController.ChasePlayerState)
            c.TransitionTo(new EnemyController.ChasePlayerState());
    }
}
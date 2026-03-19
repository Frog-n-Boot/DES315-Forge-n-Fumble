using Godot;

[GlobalClass]
public partial class AttackPlayer : EnemyBehaviourData
{
    public override bool CanActivate(EnemyController c, float healthPercent)
        => c.Player != null &&
           c.GlobalPosition.DistanceTo(c.Player.GlobalPosition) <= AttackRange;

    public override void Activate(EnemyController c)
    {
        if (c.CurrentState is not EnemyController.AttackPlayerState)
            c.TransitionTo(new EnemyController.AttackPlayerState());
    }
}
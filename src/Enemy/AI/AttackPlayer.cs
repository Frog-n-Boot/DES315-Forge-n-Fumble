using Godot;
[GlobalClass]
public partial class AttackPlayer : EnemyBehaviourData
{
    public override bool CanActivate(EnemyController c, float healthPercent)
        => c.NearestPlayer != null &&
           c.GlobalPosition.DistanceTo(c.NearestPlayer.GlobalPosition) <= AttackRange;

    public override void Activate(EnemyController c)
    {
        if (c.CurrentState is not EnemyController.AttackPlayerState)
            c.TransitionTo(new EnemyController.AttackPlayerState());
    }
}
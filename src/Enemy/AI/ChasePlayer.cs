using Godot;
[GlobalClass]
public partial class ChasePlayer : EnemyBehaviourData
{
    public override bool CanActivate(EnemyController c, float healthPercent)
        // FIX: NearestPlayer is always the closest living player, updated every 0.25 s
        => c.NearestPlayer != null &&
           c.GlobalPosition.DistanceTo(c.NearestPlayer.GlobalPosition) <= DetectionRange;

    public override void Activate(EnemyController c)
    {
        if (c.CurrentState is not EnemyController.ChasePlayerState)
            c.TransitionTo(new EnemyController.ChasePlayerState());
    }
}
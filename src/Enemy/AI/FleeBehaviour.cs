using Godot;

[GlobalClass]
public partial class FleeAny : EnemyBehaviourData
{
    public override bool CanActivate(EnemyController c, float healthPercent)
        => healthPercent < FleeThreshold;

    public override void Activate(EnemyController c)
    {
        if (c.CurrentState is not EnemyController.FleeState)
            c.TransitionTo(new EnemyController.FleeState());
    }
}
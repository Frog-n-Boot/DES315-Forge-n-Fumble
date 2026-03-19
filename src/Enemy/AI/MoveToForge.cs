using Godot;

[GlobalClass]
public partial class MoveToForge : EnemyBehaviourData
{
    public override bool CanActivate(EnemyController c, float healthPercent)
        => c.Forge != null;

    public override void Activate(EnemyController c)
    {
        if (c.CurrentState is not EnemyController.MoveToForgeState)
            c.TransitionTo(new EnemyController.MoveToForgeState());
    }
}
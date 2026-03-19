using Godot;

[GlobalClass]
public partial class AttackForge : EnemyBehaviourData
{
    public override bool CanActivate(EnemyController c, float healthPercent)
        => c.Forge != null &&
           c.GlobalPosition.DistanceTo(c.Forge.GlobalPosition) <= AttackRange;

    public override void Activate(EnemyController c)
    {
        if (c.CurrentState is not EnemyController.AttackForgeState)
            c.TransitionTo(new EnemyController.AttackForgeState());
    }
}
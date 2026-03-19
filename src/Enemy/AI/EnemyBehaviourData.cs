using Godot;

[GlobalClass]
public partial class EnemyBehaviourData : Resource
{
    [Export] public float AttackRange    { get; set; } = 2f;
    [Export] public float DetectionRange { get; set; } = 15f;
    [Export] public float FleeThreshold  { get; set; } = 0.25f;

    public virtual bool CanActivate(EnemyController c, float healthPercent) => false;
    public virtual void Activate(EnemyController c) { }

    public bool TryActivate(EnemyController c, float healthPercent)
    {
        if (!CanActivate(c, healthPercent)) return false;
        Activate(c);
        return true;
    }
}
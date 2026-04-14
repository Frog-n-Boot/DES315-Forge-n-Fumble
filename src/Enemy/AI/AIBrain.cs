using Godot;

public partial class AIBrain : Node
{
    [Export] private EnemyController _controller;

    public override void _Process(double delta)
    {
        if (_controller == null || !_controller.IsAlive()) return;
        DecisionTree();
    }

    private void DecisionTree()
    {
        if (_controller.enemyData == null) return;

        float healthPercent = _controller.GetHealthPercent();

        // Defence is checked first — survival and evasion override everything
        if (TryActivateCategory(_controller.enemyData.defenceBehaviours, healthPercent))
            return;

        // Attack is checked second — offensive behaviours
        if (TryActivateCategory(_controller.enemyData.attackBehaviours, healthPercent))
            return;

        // Misc is the fallback — pathing, idle, etc.
        if (TryActivateCategory(_controller.enemyData.miscBehaviours, healthPercent))
            return;

        // Absolutely nothing activated — hard idle
        if (_controller.CurrentState is not EnemyController.IdleState)
            _controller.TransitionTo(new EnemyController.IdleState());
    }

    // Walks a category array in order, activates the first behaviour whose
    // condition is met. Returns true if something activated.
    private bool TryActivateCategory(
        Godot.Collections.Array<EnemyBehaviourData> category,
        float healthPercent)
    {
        if (category == null) return false;

        foreach (EnemyBehaviourData behaviour in category)
        {
            if (behaviour == null) continue;
            if (behaviour.TryActivate(_controller, healthPercent))
                return true;
        }

        return false;
    }
}
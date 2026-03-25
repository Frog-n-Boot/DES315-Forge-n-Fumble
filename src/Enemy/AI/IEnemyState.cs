public interface IEnemyState
{
    void Enter(EnemyController c);
    void Update(EnemyController c, double delta);
    void PhysicsUpdate(EnemyController c, double delta);
    void Exit(EnemyController c);
}
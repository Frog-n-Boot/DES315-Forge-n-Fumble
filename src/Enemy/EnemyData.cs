using Godot;

[GlobalClass]
public partial class EnemyData : Resource
{
	[ExportGroup("Identity")]
	[Export] public string   enemyName      = "Enemy";
	[Export] public int      maxHealth      = 10;
	[Export] public int      damage         = 10;
	[Export] public int      damageToPlayer = 10;
	[Export] public float    speed          = 5.0f;
	[Export] public Color    color          = new Color(1, 0, 0);
	[Export] public Vector3  scale          = Vector3.One;
	[Export] public float    lootDropChance = 0.5f;
	[Export] public Material enemyMat;
	[Export] public Mesh     enemyMesh;
	[Export] public AudioStream deathSound;
	[Export] public float soundPitch;

	[ExportGroup("Scene References")]
	[Export] public float collisionCooldown = 0.01f;
	[Export] public float flashDuration     = 0.2f;
	[Export] public bool  isStationary      = false;

	[ExportGroup("AI")]
	[Export] public float fleeHealthThreshold = 0.25f;

	[ExportSubgroup("Attack Behaviours")]
	// Offensive behaviours e.g. AttackPlayer, AttackForge, ChasePlayer
	// Index 0 = highest priority
	[Export] public Godot.Collections.Array<EnemyBehaviourData> attackBehaviours = new();

	[ExportSubgroup("Defence Behaviours")]
	// Defensive behaviours e.g. Flee
	// Evaluated before attack — survival first
	[Export] public Godot.Collections.Array<EnemyBehaviourData> defenceBehaviours = new();

	[ExportSubgroup("Misc Behaviours")]
	// Fallback behaviours e.g. MoveToForge, Patrol
	// Evaluated last when nothing else activates
	[Export] public Godot.Collections.Array<EnemyBehaviourData> miscBehaviours = new();
}

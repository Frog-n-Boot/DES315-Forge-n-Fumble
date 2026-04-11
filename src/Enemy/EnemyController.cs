using Godot;

using System;
using System.Runtime.InteropServices;


public partial class EnemyController : CharacterBody3D

{

    public enum EnemyType { Normal, Fast, Strong }
    #region Signals

    [Signal] public delegate void DiedEventHandler(EnemyController enemy, Vector3 deathPosition);
    [Signal] public delegate void EnemyHealthChangedEventHandler(int currentHealth, int maxHealth);
    [Signal] public delegate void DamagedTargetEventHandler(Node3D target, int damage);
    [Signal] public delegate void StateChangedEventHandler(string newStateName);

    #endregion
    #region Exports

    [Export] public EnemyData enemyData;
    [Export] public MeshInstance3D mesh;
    [Export] public Area3D collisionArea;
    [Export] public NavigationAgent3D navigationAgent;
    [Export] private bool isStationary = false;

    #endregion


    #region Runtime Stats
    public string EnemyName     => enemyData.enemyName;
    public int    MaxHealth      => enemyData.maxHealth;
    public int    Damage         => enemyData.damage;
    public int    DamageToPlayer => enemyData.damageToPlayer;
    public float  Speed          => enemyData.speed;
    public float  LootDropChance => enemyData.lootDropChance;
    public bool   IsStationary   => enemyData.isStationary;
    #endregion


    #region Target References
    public CharacterBody3D Player        { get; private set; }
    public CharacterBody3D NearestPlayer { get; private set; }
    public Node3D          Forge         { get; private set; }
    public Node3D          moveTarget    { get; set; }
    private const float PlayerRefreshInterval = 0.25f;
    private float _playerRefreshTimer = 0f;
    public CharacterBody3D FindNearestPlayer()
    {
        var group = GetTree().GetNodesInGroup("Player");
        CharacterBody3D nearest = null;
        float bestDist = float.MaxValue;

        foreach (var node in group)
        {
            if (node is not PlayerController candidate) continue;
            if (candidate.health <= 0) continue;

            float dist = GlobalPosition.DistanceSquaredTo(candidate.GlobalPosition);
            if (dist < bestDist)
            {
                bestDist = dist;
                nearest  = candidate;
            }
        }
        return nearest;
    }


    private void RefreshNearestPlayer()
    {
        NearestPlayer = FindNearestPlayer();
        Player        = NearestPlayer;
    }


    private void FindForge()
    {
        if (Forge != null) return;
        Forge = GetTree().GetFirstNodeInGroup("Forge") as Node3D;
        if (Forge == null)
            GD.PrintErr($"EnemyController ({Name}): No node found in group 'Forge'.");
    }

    #endregion


    #region Health
    public int currentHealth { get; set; }
    public int CurrentHealth => currentHealth;
    private StandardMaterial3D material;

    private void InitHealth()
    {
        currentHealth = MaxHealth;
        EmitSignal(SignalName.EnemyHealthChanged, currentHealth, MaxHealth);
    }


    private void TakeHealthDamage(int amount)
    {
        if (amount <= 0) return;
        int oldHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, MaxHealth);
        if (oldHealth != currentHealth)
        {
            EmitSignal(SignalName.EnemyHealthChanged, currentHealth, MaxHealth);
            if (currentHealth <= 0) OnHealthDepleted();
        }
    }


    private void HealHealth(int amount)
    {
        if (amount <= 0) return;
        int oldHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, MaxHealth);
        if (oldHealth != currentHealth)
            EmitSignal(SignalName.EnemyHealthChanged, currentHealth, MaxHealth);
    }
    public bool IsAlive() => currentHealth > 0;
    public float GetHealthPercent() => MaxHealth > 0 ? (float)currentHealth / MaxHealth : 0f;
    #endregion


    #region State Machine
    public IEnemyState CurrentState { get; private set; }

    public void TransitionTo(IEnemyState newState)
    {
        CurrentState?.Exit(this);
        CurrentState = newState;
        CurrentState.Enter(this);
        EmitSignal(SignalName.StateChanged, newState.GetType().Name);
    }

    #endregion


    #region Navigation

    private float   rotationSpeed = 10.0f;
    private Vector3 knockback     = Vector3.Zero;
    private bool    _navReady         = false;
    private bool    _pendingNavTarget = false;
    private Vector3 _pendingTargetPos = Vector3.Zero;
    private const float NavUpdateInterval      = 0.15f;
    private const float NavTargetMoveThreshold = 0.25f;
    private float   _navUpdateTimer  = NavUpdateInterval; 
    private Vector3 _lastNavTarget   = Vector3.Zero;

    private const float StuckCheckInterval = 0.75f;
    private const float StuckMoveThreshold = 0.15f;
    private float   _stuckTimer    = 0f;
    private Vector3 _stuckCheckPos = Vector3.Zero;

    private int enemyCount = 0;

    private const int DiagInterval = 60; // ~1 second at 60 fps
    private int _diagFrame = 0;

    private void InitNavAgent()
    {
        if (navigationAgent == null)
            navigationAgent = GetNodeOrNull<NavigationAgent3D>("NavigationAgent3D");
        if (navigationAgent == null)
        {
            GD.PrintErr($"[{Name}] DIAG: NavigationAgent3D not found — movement is impossible.");
            return;
        }
       
        navigationAgent.VelocityComputed += OnAvoidanceVelocityComputed;

        GD.Print($"[{Name}] DIAG InitNavAgent: agent found. Speed={Speed} PathDist={navigationAgent.PathDesiredDistance} TargetDist={navigationAgent.TargetDesiredDistance}");
    }


    private void OnAvoidanceVelocityComputed(Vector3 safeVelocity)
    {
        Velocity = safeVelocity;
    }
   
    private void PrintDiagnostics(Vector3 targetPosition, Vector3 nextPoint, Vector3 direction)
    {
        bool navFinished    = navigationAgent?.IsNavigationFinished() ?? true;
        int  pathPointCount = 0;
        try { pathPointCount = navigationAgent?.GetCurrentNavigationPath().Length ?? 0; } catch { }
        GD.Print("──────────────────────────────────────────────");
        GD.Print($"[{Name}, {enemyCount}] DIAG @ physics frame {_diagFrame}");
        GD.Print($"  State         : {CurrentState?.GetType().Name ?? "null"}");
        GD.Print($"  IsStationary  : {IsStationary}");
        GD.Print($"  _navReady     : {_navReady}");
        GD.Print($"  Speed         : {Speed}");
        GD.Print($"  MyPosition    : {GlobalPosition}");
        GD.Print($"  TargetPos     : {targetPosition}");
        GD.Print($"  DistToTarget  : {GlobalPosition.DistanceTo(targetPosition):F2}");
        GD.Print($"  NextPathPoint : {nextPoint}");
        GD.Print($"  Direction     : {direction}  LenSq={direction.LengthSquared():F4}");
        GD.Print($"  Velocity      : {Velocity}");
        GD.Print($"  Knockback     : {knockback}  LenSq={knockback.LengthSquared():F4}");
        GD.Print($"  NavFinished   : {navFinished}");
        GD.Print($"  PathPoints    : {pathPointCount}");
        GD.Print($"  NavAgent null : {navigationAgent == null}");
        if (navigationAgent != null)
        {
            GD.Print($"  NavTarget set : {navigationAgent.TargetPosition}");
            GD.Print($"  MapRid valid  : {navigationAgent.GetNavigationMap().IsValid}");
            GD.Print($"  NavAgent MaxSpeed : {navigationAgent.MaxSpeed}");
            GD.Print($"  TargetDesiredDist : {navigationAgent.TargetDesiredDistance}");
        }
        GD.Print($"  Forge         : {(Forge != null ? Forge.GlobalPosition.ToString() : "null")}");
        GD.Print($"  NearestPlayer : {(NearestPlayer != null ? NearestPlayer.GlobalPosition.ToString() : "null")}");
        GD.Print($"  moveTarget    : {(moveTarget != null ? moveTarget.Name : "null")}");
        GD.Print("──────────────────────────────────────────────");
        enemyCount++;
    }


    public void SetNavTarget(Vector3 target)
    {
        if (navigationAgent == null)
        {
            GD.PrintErr($"[{Name}] SetNavTarget({target}): navigationAgent is NULL.");
            return;
        }
        if (!_navReady)
        {
            GD.Print($"[{Name}] SetNavTarget({target}): nav not ready — queued.");
            _pendingNavTarget = true;
            _pendingTargetPos = target;
            return;
        }
        GD.Print($"[{Name}] SetNavTarget({target}): applied.");
        navigationAgent.TargetPosition = target;
        _lastNavTarget    = target;
        _pendingNavTarget = false;
    }

    public void NavigateTo(Vector3 targetPosition, double delta)
    {
        if (navigationAgent == null)
        {
            GD.PrintErr($"[{Name}] NavigateTo: navigationAgent is NULL — cannot move.");
            return;
        }

        _navUpdateTimer -= (float)delta;
        if (_navUpdateTimer <= 0f)
        {
            _navUpdateTimer = NavUpdateInterval;
            if (_lastNavTarget.DistanceSquaredTo(targetPosition) >
                NavTargetMoveThreshold * NavTargetMoveThreshold)
            {
                _lastNavTarget = targetPosition;
                SetNavTarget(targetPosition);
            }
        }

        // 1. Stop if the agent has reached its destination.
        if (navigationAgent.IsNavigationFinished())
        {
            Velocity = Vector3.Zero;
            return;
        }

        // 2. Knockback overrides normal movement.
        if (knockback.LengthSquared() > 0.01f)
        {
            Velocity  = knockback;
            knockback = knockback.Lerp(Vector3.Zero, 0.15f);
            return;
        }

        Vector3 nextPoint = navigationAgent.GetNextPathPosition();
        Vector3 direction = nextPoint - GlobalPosition;
        direction.Y = 0f;

        if (direction.LengthSquared() < 0.001f)
            return;

        direction = direction.Normalized();
        RotateTowards(direction, delta);

        navigationAgent.Velocity = direction * Speed;

        _stuckTimer += (float)delta;
        if (_stuckTimer >= StuckCheckInterval)
        {
            if (GlobalPosition.DistanceSquaredTo(_stuckCheckPos) <
                StuckMoveThreshold * StuckMoveThreshold)
            {
                GD.Print($"[{Name}] Stuck detected — forcing repath.");
                _lastNavTarget = Vector3.Zero; // invalidate so threshold passes
                SetNavTarget(targetPosition);
            }
            _stuckCheckPos = GlobalPosition;
            _stuckTimer    = 0f;
        }

        _diagFrame++;
        //if (_diagFrame % DiagInterval == 0)
            //PrintDiagnostics(targetPosition, nextPoint, direction);
    }

    public void MoveTowards(Vector3 targetPosition, double delta)
    {
        Vector3 direction = (targetPosition - GlobalPosition).Normalized();
        if (direction.LengthSquared() > 0.01f)
            RotateTowards(direction, delta);
        knockback = knockback.Lerp(Vector3.Zero, 0.15f);

        Vector3 desiredVelocity = direction * Speed;

        // Apply knockback on top if active.
        if (knockback.LengthSquared() > 0.01f)
            desiredVelocity = knockback;

        // Route through the avoidance system so enemies don't stack even
        // during the direct final approach.
        if (navigationAgent != null && navigationAgent.AvoidanceEnabled)
            navigationAgent.Velocity = desiredVelocity;
        else
            Velocity = desiredVelocity;
    }

    private void RotateTowards(Vector3 direction, double delta)
    {
        Vector3 targetRotation = new Vector3(0, Mathf.Atan2(direction.X, direction.Z), 0);
        Rotation = Rotation.Lerp(targetRotation, rotationSpeed * (float)delta);
    }

    public void ApplyKnockback(Vector3 direction, float force)
    {
        knockback = direction * force;
    }

    #endregion


    #region Collision
    private bool canCollide = true;

    private void InitializeCollisionArea(Area3D area)
    {
        area.BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (!canCollide) return;
        canCollide = false;
        string group = GetBodyGroup(body);
        if (!string.IsNullOrEmpty(group))
            OnCollisionDetected(body, group);
        GetTree().CreateTimer(enemyData.collisionCooldown).Timeout += () => canCollide = true;
    }

    private string GetBodyGroup(Node3D body)
    {
        if (body.IsInGroup("Player")) return "Player";
        if (body.IsInGroup("Forge"))  return "Forge";
        if (body.IsInGroup("Weapon")) return "Weapon";
        if (body.IsInGroup("Enemy"))  return "Enemy";
        if (body.IsInGroup("Bullet")) return "Bullet";
        if (body.IsInGroup("Arrow"))  return "Arrow";
        return "";
    }
    #endregion

    #region Private State
    private Forge   forgeScript;
    private bool    isDying        = false;
    private Vector3 _spawnPosition = Vector3.Zero;
    #endregion


    #region Lifecycle
    public override void _Ready()
    {
        if (enemyData == null)
        {
            GD.PrintErr($"[{Name}] DIAG _Ready: enemyData is NULL — aborting.");
            return;
        }
        GD.Print($"[{Name}] DIAG _Ready: start. SpawnPos={_spawnPosition}");

        AutoFindNodes();
        ApplyVisuals();
        SetupCollision();
        InitNavAgent();
        InitHealth();
        RefreshNearestPlayer();
        FindForge();

        GD.Print($"[{Name}] DIAG _Ready: Forge={Forge?.GlobalPosition.ToString() ?? "null"}  NearestPlayer={NearestPlayer?.GlobalPosition.ToString() ?? "null"}");
        if (_spawnPosition != Vector3.Zero)
            GlobalPosition = _spawnPosition;
        GD.Print($"[{Name}] DIAG _Ready: GlobalPosition after spawn={GlobalPosition}");
        TransitionTo(new MoveToForgeState());
        GD.Print($"[{Name}] DIAG _Ready: done. State={CurrentState?.GetType().Name}  IsStationary={IsStationary}");
    }


    public override void _Process(double delta)
    {
        if(isStationary) return;
        //if (IsStationary) return;
        _playerRefreshTimer -= (float)delta;
        if (_playerRefreshTimer <= 0f)
        {
            RefreshNearestPlayer();
            _playerRefreshTimer = PlayerRefreshInterval;
        }
        CurrentState?.Update(this, delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        if(isStationary) return;
       // if (IsStationary) return;
        if (!_navReady)
        {
            _navReady = true;
            GD.Print($"[{Name}] DIAG _PhysicsProcess: first frame — nav now ready.");
            GD.Print($"  MapRid valid  : {navigationAgent?.GetNavigationMap().IsValid}");
            GD.Print($"  PendingTarget : {_pendingNavTarget}  pos={_pendingTargetPos}");
            GD.Print($"  moveTarget    : {moveTarget?.Name ?? "null"}  pos={moveTarget?.GlobalPosition.ToString() ?? "null"}");

            if (_pendingNavTarget && navigationAgent != null)
            {
                GD.Print($"[{Name}] DIAG: flushing pending nav target {_pendingTargetPos}");
                navigationAgent.TargetPosition = _pendingTargetPos;
                _lastNavTarget    = _pendingTargetPos;
                _pendingNavTarget = false;
            }
            else if (moveTarget != null && navigationAgent != null)
            {
                GD.Print($"[{Name}] DIAG: setting nav target from moveTarget {moveTarget.GlobalPosition}");
                navigationAgent.TargetPosition = moveTarget.GlobalPosition;
                _lastNavTarget = moveTarget.GlobalPosition;
            }
            else
            {
                GD.PrintErr($"[{Name}] DIAG: nav ready but NO target available — enemy will not move!");
            }
            return;
        }
        CurrentState?.PhysicsUpdate(this, delta);
        MoveAndSlide();
    }
    #endregion


    #region Setup
    private void ApplyVisuals()
    {
        if (enemyData == null) return;
        if (mesh != null)
        {
            if (enemyData.enemyMesh != null) mesh.Mesh             = enemyData.enemyMesh;
            if (enemyData.enemyMat  != null) mesh.MaterialOverride = enemyData.enemyMat;
        }
        if (enemyData.enemyMat != null)
        {
            material                          = enemyData.enemyMat.Duplicate() as StandardMaterial3D;
            material.EmissionEnabled          = true;
            material.EmissionEnergyMultiplier = 0f;
            mesh.MaterialOverride             = material;
        }
        if (enemyData.scale != Vector3.Zero)
            Scale = enemyData.scale;
    }

    private void SetupCollision()
    {
        if (collisionArea == null)
            collisionArea = GetNodeOrNull<Area3D>("Area3D");
        if (collisionArea != null)
            InitializeCollisionArea(collisionArea);
        else
            GD.PrintErr($"[{Name}] DIAG SetupCollision: Area3D not found.");
    }

    private void AutoFindNodes()
    {
        if (mesh == null)
            mesh = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
        if (mesh == null)
            GD.PrintErr($"[{Name}] DIAG AutoFindNodes: MeshInstance3D not found.");
    }
    #endregion


    #region Initialize (Spawner API)
    public void Initialize(EnemyData data, Node3D target, Vector3 spawnPosition)
    {
        enemyData      = data;
        moveTarget     = target;
        _spawnPosition = spawnPosition;
        GD.Print($"[{Name}] DIAG Initialize: data={data?.enemyName}  target={target?.Name}  spawnPos={spawnPosition}");
    }

    public static EnemyController Create(EnemyData data, Node3D target, Node parent, Vector3 spawnPosition)
    {
        var enemyScene = GD.Load<PackedScene>("res://assets/prefabs/Enemy.tscn");
        var enemy      = enemyScene.Instantiate<EnemyController>();
        enemy.Initialize(data, target, spawnPosition);
        parent.CallDeferred(Node.MethodName.AddChild, enemy);
        return enemy;
    }
    #endregion


    #region Forge
    private Forge GetForge()
    {
        if (forgeScript == null)
            forgeScript = GetNode<Forge>("/root/Forge");
        return forgeScript;
    }
    #endregion

    #region Collision Handling
    private void OnCollisionDetected(Node3D body, string groupName)
    {
        if (isDying) return;
        switch (groupName)
        {
            case "Forge":
                EmitSignal(SignalName.DamagedTarget, body, Damage);
                Forge forge = GetForge();
                if (forge != null) { forge.TakeDamage(Damage); GD.Print(forge.health); }
                
                Vector3 forgePushDir = (GlobalPosition - body.GlobalPosition).Normalized();
                forgePushDir.Y = 0;
                ApplyKnockback(forgePushDir, 15f);
                break;
            case "Player":
                EmitSignal(SignalName.DamagedTarget, body, Damage);
                if (body is PlayerController playerController)
                {
                    playerController.TakeDamage(DamageToPlayer);
                    Vector3 pushDir = (playerController.GlobalPosition - GlobalPosition).Normalized();
                    pushDir.Y = 0f;
                    playerController.ApplyKnockback(pushDir.Normalized(), 20f);
                }
                break;
            case "Weapon":
                BaseWeapon weapon = FindWeaponInHierarchy(body);             
                if (weapon != null)
                {
                   
                    TakeDamage(weapon is Sword sword ? sword.GetComboDamage() : weapon.damage);
                    weapon.TakeDurabilityDamage(1);
                    Vector3 pushDir = (GlobalPosition - weapon.GlobalPosition).Normalized();
                    pushDir.Y = 0f;
                    ApplyKnockback(pushDir.Normalized(), 20f);
                    
                }
                break;
            case "Bullet":
                if (body.GetParent() is Bullet bullet) { TakeDamage(bullet.damage); bullet.QueueFree();}
                break;
            case "Arrow":
                if (body.GetParent() is Arrow arrow) { TakeDamage((int)arrow.damage); arrow.QueueFree();}
                break;
        }
    }
    #endregion

    #region Health Events
    private void OnHealthDepleted()
    {
        EmitSignal(SignalName.Died, this, GlobalPosition);     
        QueueFree();
    }
    #endregion

    #region Public API
    public void TakeDamage(int amount)
	{
		Flash();
		TakeHealthDamage(amount);
		DamageNumbers.Spawn(amount, GlobalPosition, GetParent());
	}
    public void Die() { EmitSignal(SignalName.Died, this, GlobalPosition); QueueFree(); }
    public void SetTarget(Node3D newTarget) => moveTarget = newTarget;
    #endregion


    #region Helpers
    private BaseWeapon FindWeaponInHierarchy(Node current)
    {
        while (current != null)
        {
            if (current is BaseWeapon weapon) return weapon;
            current = current.GetParent();
        }
        return null;
    }
    #endregion


    #region Flash
    private void Flash()
    {
        if (material == null) return;
        Color white = new Color(1, 1, 1);
        Color red   = new Color(1, 0, 0);
        var tween = CreateTween();
        tween.TweenProperty(material, "emission_energy_multiplier", 2.0f, enemyData.flashDuration / 4);
        tween.Parallel().TweenProperty(material, "emission", white, enemyData.flashDuration / 4);
        tween.TweenProperty(material, "emission", red, enemyData.flashDuration / 4);
        tween.TweenProperty(material, "emission_energy_multiplier", 0.0f, enemyData.flashDuration / 2);
    }
    #endregion


    // ── Base State ───────────────────────────────────────────────────────────
    public class EnemyStateBase : IEnemyState
    {
        public virtual void Enter(EnemyController c) { }
        public virtual void Update(EnemyController c, double delta) { }
        public virtual void PhysicsUpdate(EnemyController c, double delta) { }
        public virtual void Exit(EnemyController c) { }
    }

    // ── States ───────────────────────────────────────────────────────────────
    public class IdleState : EnemyStateBase { }

    public class ChasePlayerState : EnemyStateBase
    {
        public override void Enter(EnemyController c)
        {
            GD.Print($"[{c.Name}] Enter ChasePlayerState. NearestPlayer={c.NearestPlayer?.GlobalPosition.ToString() ?? "null"}");
            c.moveTarget = c.NearestPlayer;
            if (c.NearestPlayer != null)
                c.SetNavTarget(c.NearestPlayer.GlobalPosition);
        }
        public override void PhysicsUpdate(EnemyController c, double delta)
        {
            if (c.NearestPlayer == null) return;
            c.moveTarget = c.NearestPlayer;
            c.NavigateTo(c.NearestPlayer.GlobalPosition, delta);
        }
    }

    public class MoveToForgeState : EnemyStateBase
    {
        // Distance at which we abandon the nav mesh and walk directly into
        // the forge. Keep this tight (just larger than the nav mesh gap) so
        // enemies are clear of rocks before switching to direct movement.
        private const float DirectApproachDistance = 2.0f;

        public override void Enter(EnemyController c)
        {
            GD.Print($"[{c.Name}] Enter MoveToForgeState. Forge={c.Forge?.GlobalPosition.ToString() ?? "null"}");
            c.moveTarget = c.Forge;
            if (c.Forge != null)
                c.SetNavTarget(c.Forge.GlobalPosition);
        }

        public override void PhysicsUpdate(EnemyController c, double delta)
        {
            
            
            if (c.Forge == null) return;

            float dist = c.GlobalPosition.DistanceTo(c.Forge.GlobalPosition);

            // Nav mesh ends before the forge collision shape boundary.
            // Once close enough (or nav says finished), drive the next path
            // point rather than the forge centre directly — this keeps the
            // enemy following the computed path around rocks instead of
            // cutting straight through them.
            if (dist <= DirectApproachDistance || c.navigationAgent.IsNavigationFinished())
                c.MoveTowards(c.navigationAgent.GetNextPathPosition(), delta);
            else
                c.NavigateTo(c.Forge.GlobalPosition, delta);
        }
    }

    public class AttackPlayerState : EnemyStateBase
    {
        public override void Enter(EnemyController c)
        {
            GD.Print($"[{c.Name}] Enter AttackPlayerState. NearestPlayer={c.NearestPlayer?.GlobalPosition.ToString() ?? "null"}");
            c.moveTarget = c.NearestPlayer;
            if (c.NearestPlayer != null)
                c.SetNavTarget(c.NearestPlayer.GlobalPosition);
        }
        public override void PhysicsUpdate(EnemyController c, double delta)
        {
            if (c.NearestPlayer == null) return;
            c.moveTarget = c.NearestPlayer;
            c.NavigateTo(c.NearestPlayer.GlobalPosition, delta);
        }
    }

    public class AttackForgeState : EnemyStateBase
    {
        // Same threshold as MoveToForgeState — keeps the two states consistent.
        private const float DirectApproachDistance = 2.0f;

        public override void Enter(EnemyController c)
        {
            GD.Print($"[{c.Name}] Enter AttackForgeState. Forge={c.Forge?.GlobalPosition.ToString() ?? "null"}");
            c.moveTarget = c.Forge;
            if (c.Forge != null)
                c.SetNavTarget(c.Forge.GlobalPosition);
        }

        public override void PhysicsUpdate(EnemyController c, double delta)
        {
            if (c.Forge == null) return;

            float dist = c.GlobalPosition.DistanceTo(c.Forge.GlobalPosition);

            if (dist <= DirectApproachDistance || c.navigationAgent.IsNavigationFinished())
                c.MoveTowards(c.navigationAgent.GetNextPathPosition(), delta);
            else
                c.NavigateTo(c.Forge.GlobalPosition, delta);
        }
    }

    public class FleeState : EnemyStateBase
    {
        public override void PhysicsUpdate(EnemyController c, double delta)
        {
            Vector3 fleeDir = Vector3.Zero;
            if (c.NearestPlayer != null)
                fleeDir += (c.GlobalPosition - c.NearestPlayer.GlobalPosition).Normalized();
            if (c.Forge != null)
                fleeDir += (c.GlobalPosition - c.Forge.GlobalPosition).Normalized();
            if (fleeDir.LengthSquared() < 0.001f) return;
            c.MoveTowards(c.GlobalPosition + fleeDir.Normalized() * 20f, delta);
        }
    }
}
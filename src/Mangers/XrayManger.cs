using Godot;
using Godot.Collections;
using System.Collections.Generic;

/// <summary>
/// XRayManager.cs — AutoLoad compatible.
///
/// Register as an AutoLoad in Project Settings → AutoLoad.
/// It waits for the main scene to finish loading before scanning
/// groups and connecting to PlayerSpawner, so _Ready order does
/// not matter.
///
/// SCENE SETUP
/// ───────────
///   • Tag every wall/prop MeshInstance3D (or its parent) with
///     the "Xrayed" group.
///   • Tag the XRayOverlay MeshInstance3D under each player with
///     the "XRayOverlay" group.
///   • Enemies get no tag and are ignored automatically.
/// </summary>
[GlobalClass]
public partial class XRayManager : Node
{
    // ── Exports ───────────────────────────────────────────────

    /// <summary>
    /// NodePath to PlayerSpawner. Because this is an AutoLoad,
    /// the spawner lives in the game scene — use its scene-local
    /// path (e.g. "PlayerSpawner") and we search for it after the
    /// scene loads, rather than an absolute /root/ path.
    /// </summary>
    [Export] public string PlayerSpawnerName { get; set; } = "PlayerSpawner";

    [ExportGroup("Visuals")]
    [Export(PropertyHint.Range, "0.01,0.5")]  public float XRayRadius  { get; set; } = 0.14f;
    [Export(PropertyHint.Range, "0.005,0.2")] public float DitherBand  { get; set; } = 0.045f;
    [Export] public Color XRayTint   { get; set; } = new Color(0.25f, 0.85f, 1.0f, 1.0f);
    [Export(PropertyHint.Range, "0,10")] public float PulseSpeed  { get; set; } = 2.0f;
    [Export(PropertyHint.Range, "0,1")]  public float PulseAmount { get; set; } = 0.15f;

    // ── Group names ───────────────────────────────────────────
    private const string GroupXrayed  = "Xrayed";
    private const string GroupOverlay = "XRayOverlay";

    // ── Private state ─────────────────────────────────────────
    private readonly List<ShaderMaterial> _wallMaterials    = new();
    private readonly List<ShaderMaterial> _overlayMaterials = new();
    private readonly List<Node3D>         _players          = new();
    private readonly Vector3[]            _posBuffer        = new Vector3[4];

    private PlayerSpawner _spawner;
    private bool          _initialised = false;

    // ── Lifecycle ─────────────────────────────────────────────

    public override void _Ready()
    {
        // AutoLoads run before the game scene exists.
        // Wait until the scene tree's root gains a new child
        // (i.e. the main scene has been added) then initialise.
        GetTree().Root.ChildEnteredTree += OnRootChildEntered;
    }

    public override void _Process(double delta)
    {
        if (_initialised)
            PushUniforms();
    }

    // ── Wait for main scene ───────────────────────────────────

    private void OnRootChildEntered(Node node)
    {
        // The main scene is the second child of root (first is this AutoLoad).
        // Fire once, then disconnect so we don't re-initialise on sub-scene loads.
        GetTree().Root.ChildEnteredTree -= OnRootChildEntered;

        // Defer so the newly added scene has completed its own _Ready chain.
        CallDeferred(MethodName.Initialise);
    }

    // ── Initialisation ────────────────────────────────────────

    private void Initialise()
    {
        CollectXrayedMaterials();
        ConnectToSpawner();
        SeedExistingPlayers();

        _initialised = true;
        GD.Print("[XRayManager] Initialised.");
    }

    /// <summary>
    /// Walk every node in the "Xrayed" group and collect their
    /// ShaderMaterials. Done once — static level geometry never changes.
    /// </summary>
    private void CollectXrayedMaterials()
    {
        _wallMaterials.Clear();

        foreach (Node node in GetTree().GetNodesInGroup(GroupXrayed))
            WalkForWalls(node);

        GD.Print($"[XRayManager] Found {_wallMaterials.Count} wall material(s).");
    }

    private void ConnectToSpawner()
    {
        // Search the current scene tree for the spawner by name.
        _spawner = GetTree().Root.FindChild(PlayerSpawnerName, true, false) as PlayerSpawner;

        if (_spawner == null)
        {
            GD.PrintErr($"[XRayManager] PlayerSpawner '{PlayerSpawnerName}' not found. " +
                         "Players must be registered manually via RegisterPlayer().");
            return;
        }

        _spawner.PlayerSpawned += OnPlayerSpawned;
        GD.Print("[XRayManager] Connected to PlayerSpawner.");
    }

    /// <summary>
    /// Register players that were already spawned before we initialised
    /// (e.g. player 0 is spawned in PlayerSpawner._Ready via CallDeferred,
    /// which runs before our deferred Initialise call — so we catch them here).
    /// </summary>
    private void SeedExistingPlayers()
    {
        if (_spawner != null)
        {
            for (int i = 0; i < InputManager.MAX_PLAYERS; i++)
            {
                if (_spawner.GetPlayer(i) is Node3D p)
                    RegisterPlayer(p);
            }
            return;
        }

        // Fallback: scan the "Player" group if spawner wasn't found.
        foreach (Node node in GetTree().GetNodesInGroup("Player"))
        {
            if (node is Node3D p)
                RegisterPlayer(p);
        }
    }

    // ── Public API ────────────────────────────────────────────

    /// <summary>Register a spawned player. Called automatically via PlayerSpawner signal.</summary>
    public void RegisterPlayer(Node3D playerRoot)
    {
        if (_players.Contains(playerRoot))
            return;

        _players.Add(playerRoot);
        CollectOverlaysUnder(playerRoot);

        GD.Print($"[XRayManager] Registered player '{playerRoot.Name}'  (total: {_players.Count})");
    }

    /// <summary>Deregister a despawned player. Call this from PlayerSpawner.DespawnPlayer().</summary>
    public void UnregisterPlayer(Node3D playerRoot)
    {
        _players.Remove(playerRoot);
        RemoveOverlaysUnder(playerRoot);

        GD.Print($"[XRayManager] Unregistered player '{playerRoot.Name}'  (total: {_players.Count})");
    }

    // ── PlayerSpawner signal handler ──────────────────────────

    private void OnPlayerSpawned(PlayerController player, int playerIndex)
    {
        RegisterPlayer(player);
    }

    // ── Material collection helpers ───────────────────────────

    private void WalkForWalls(Node node)
    {
        if (node is MeshInstance3D mesh)
            CollectFromMesh(mesh, _wallMaterials);

        foreach (Node child in node.GetChildren())
            WalkForWalls(child);
    }

    private void CollectOverlaysUnder(Node root)
    {
        if (root is MeshInstance3D mesh && root.IsInGroup(GroupOverlay))
        {
            CollectFromMesh(mesh, _overlayMaterials);
            return;
        }

        foreach (Node child in root.GetChildren())
            CollectOverlaysUnder(child);
    }

    private void RemoveOverlaysUnder(Node root)
    {
        if (root is MeshInstance3D mesh && root.IsInGroup(GroupOverlay))
        {
            for (int i = 0; i < mesh.GetSurfaceOverrideMaterialCount(); i++)
            {
                if (mesh.GetActiveMaterial(i) is ShaderMaterial mat)
                    _overlayMaterials.Remove(mat);
            }
            return;
        }

        foreach (Node child in root.GetChildren())
            RemoveOverlaysUnder(child);
    }

    private void CollectFromMesh(MeshInstance3D mesh, List<ShaderMaterial> target)
    {
        for (int i = 0; i < mesh.GetSurfaceOverrideMaterialCount(); i++)
        {
            if (mesh.GetActiveMaterial(i) is ShaderMaterial mat && !target.Contains(mat))
                target.Add(mat);
        }
    }

    // ── Per-frame uniform push ────────────────────────────────

    private void PushUniforms()
    {
        int count = Mathf.Clamp(_players.Count, 0, 4);

        for (int i = 0; i < 4; i++)
            _posBuffer[i] = i < count ? _players[i].GlobalPosition : Vector3.Zero;

        var posArray     = new Array<Vector3>(_posBuffer);
        var viewportSize = GetViewport().GetVisibleRect().Size;

        foreach (ShaderMaterial mat in _wallMaterials)
        {
            mat.SetShaderParameter("player_positions", posArray);
            mat.SetShaderParameter("player_count",     count);
            mat.SetShaderParameter("xray_radius",      XRayRadius);
            mat.SetShaderParameter("dither_band",      DitherBand);
            mat.SetShaderParameter("viewport_size",    viewportSize);
        }

        foreach (ShaderMaterial mat in _overlayMaterials)
        {
            mat.SetShaderParameter("player_positions", posArray);
            mat.SetShaderParameter("player_count",     count);
            mat.SetShaderParameter("xray_radius",      XRayRadius);
            mat.SetShaderParameter("dither_band",      DitherBand);
            mat.SetShaderParameter("viewport_size",    viewportSize);
            mat.SetShaderParameter("xray_tint",        XRayTint);
            mat.SetShaderParameter("pulse_speed",      PulseSpeed);
            mat.SetShaderParameter("pulse_amount",     PulseAmount);
        }
    }
}
using Godot;
using System.Collections.Generic;
using System;
public partial class XRayManager : Node
{
    // Shader Consts
    private const float defaultRadius = .35f;
    private const float defaultEdgeSoftness = 0.4f;
    private const int maxPlayers = 4;
    private readonly List<Node3D>_players = new();
    private readonly List<MeshInstance3D> walls = new();
    private static readonly string ShaderPath ="res://src//Shaders/xray.gdshader";
    private Shader _xrayShader;
	private bool wallsCollected = false;
	public static XRayManager Instance {get; private set;}


    
    public override void _Ready(){
        Instance = this;
        
        _xrayShader = GD.Load<Shader>(ShaderPath);
        //if (_xrayShader == null)
            //GD.PrintErr($"[XRayManager] Shader not found at: {ShaderPath}");
        //else
            //GD.Print($"[XRayManager] Shader loaded from: {ShaderPath}");
        
        // Wait for all nodes to be ready
        //ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame).OnCompleted(CollectWalls);

		GetTree().TreeChanged += OnTreeChange;
    }

    public override void _Process(double delta){
        if (walls.Count == 0 || _players.Count == 0) return;
            
        UpdateWalls();
    }

	private void OnTreeChange()
	{
		int currentWallCount = GetTree().GetNodesInGroup("xray").Count;
    
        // Only re-collect if wall count has changed
        if (currentWallCount != walls.Count){
            CallDeferred(MethodName.CollectWalls);
        }
	}

    public void RegisterPlayer(Node3D player)
    {
        if(!_players.Contains(player))
        {
            _players.Add(player);
            //GD.Print($"[XRayManager] Player registered: {player.Name} | Total players: {_players.Count}");
        }
        //else
        //{
            //GD.PrintErr($"[XRayManager] Player already registered: {player.Name}");
       //}
    }

    public void UnregisterPlayer(Node3D player)
    {
        _players.Remove(player);
        //GD.Print($"[XRayManager] Player unregistered: {player.Name} | Total players: {_players.Count}");
    }

    private void CollectWalls()
    {
        walls.Clear();

    	var wallNodes = GetTree().GetNodesInGroup("xray");
    	//GD.Print($"[XRayManager] Collecting walls — found {wallNodes.Count} nodes in group 'xray'");

    	if (wallNodes.Count == 0)
    	{
        	//GD.PrintErr("[XRayManager] No nodes in group 'xray' — walls not loaded yet or not tagged");
        	return;
    	}

    	foreach (Node node in wallNodes)
    	{
        	//GD.Print($"[XRayManager] Processing wall node: {node.Name} (path: {node.GetPath()})");
        	MeshInstance3D mesh = GetMeshInstance(node);
        	if (mesh != null)
        	{
            	//GD.Print($"[XRayManager] Found mesh: '{mesh.Name}' for wall: '{node.Name}'");
            	EnsureShaderMaterial(mesh);
            	walls.Add(mesh);
        	}
        	//else
        	//{
            	//GD.PrintErr($"[XRayManager] No MeshInstance3D found for: '{node.Name}'");
        	//}
		}
       // GD.Print($"[XRayManager]  Total walls tracked: {walls.Count}");
    }

    private void UpdateWalls()
    {
        // Build position array
        Vector3[] positions = new Vector3[maxPlayers];
        int playerCount = Mathf.Min(_players.Count, maxPlayers);
        for (int i = 0; i < playerCount; i++){
            positions[i] = _players[i].GlobalPosition;
        }
        // Pad Unused Slots so they don't affect the shader
        for (int i = playerCount; i < maxPlayers; i++){
            positions[i] = new Vector3(99999f, 99999f, 99999f);
        }

        foreach(MeshInstance3D mesh in walls)
        {
            if(mesh.GetSurfaceOverrideMaterial(0) is not ShaderMaterial mat)
            {
                //GD.PrintErr($"[XRayManager] Wall '{mesh.Name}' missing ShaderMaterial — EnsureShaderMaterial may have failed");
                continue;
            }
            
            mat.SetShaderParameter("player_positions", positions);
            mat.SetShaderParameter("player_count", playerCount);
            mat.SetShaderParameter("radius", defaultRadius);
            mat.SetShaderParameter("edge_softness", defaultEdgeSoftness);
        }
    }

    private static MeshInstance3D GetMeshInstance(Node node)
    {
        if (node is MeshInstance3D meshInstance)
            return meshInstance;
        // Recursively search for child node with MeshInstance3D
        foreach (Node child in node.GetChildren())
        {
            MeshInstance3D found = GetMeshInstance(child);
            if(found != null) return found;
        }
        return null;
    }

    private void EnsureShaderMaterial(MeshInstance3D mesh)
{
    if (mesh.GetSurfaceOverrideMaterial(0) is ShaderMaterial)
    {
        //GD.Print($"[XRayManager] '{mesh.Name}' already has ShaderMaterial, skipping");
        return;
    }

    if (_xrayShader == null)
    {
        //GD.PrintErr($"[XRayManager] Cannot apply shader to '{mesh.Name}' — shader is null");
        return;
    }

    // Get the existing material from either the override or the mesh itself
    Material existingMat = mesh.GetSurfaceOverrideMaterial(0) 
                        ?? mesh.Mesh?.SurfaceGetMaterial(0);

    var mat = new ShaderMaterial();
    mat.Shader = _xrayShader;

    if (existingMat is StandardMaterial3D std)
    {
        //GD.Print($"[XRayManager] '{mesh.Name}' copying from StandardMaterial3D");
        // Copy ALL texture slots, not just albedo
       mat.SetShaderParameter("albedo", std.AlbedoColor);
        mat.SetShaderParameter("texture_albedo", std.AlbedoTexture);
    }
    else if (existingMat is BaseMaterial3D baseMat)
    {
        //GD.Print($"[XRayManager] '{mesh.Name}' copying from BaseMaterial3D");
        mat.SetShaderParameter("albedo", baseMat.AlbedoColor);
        mat.SetShaderParameter("texture_albedo", baseMat.AlbedoTexture);
    }
    else if (existingMat is ShaderMaterial existingShader)
    {
        // Wall already has a custom shader — grab its texture parameter if it has one
        //GD.Print($"[XRayManager] '{mesh.Name}' already uses ShaderMaterial, copying texture_albedo");
        var existingTex = existingShader.GetShaderParameter("texture_albedo");
        if (existingTex.Obj != null)
            mat.SetShaderParameter("texture_albedo", existingTex);
    }
    else
    {
        //GD.PrintErr($"[XRayManager] '{mesh.Name}' has unhandled material type: {existingMat?.GetType().Name ?? "null"}");
    }

    mat.SetShaderParameter("radius", defaultRadius);
    mat.SetShaderParameter("edge_softness", defaultEdgeSoftness);
    mesh.SetSurfaceOverrideMaterial(0, mat);
    //GD.Print($"[XRayManager] ShaderMaterial applied to '{mesh.Name}'");
}
}
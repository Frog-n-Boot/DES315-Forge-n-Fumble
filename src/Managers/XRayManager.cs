using Godot;
using System.Collections.Generic;
using System;

public partial class XRayManager : Node
{
	// Shader Consts
	private const float defaultRadius = 1.5f;
	private const float defaultEdgeSoftness = 0.4f;
	private const int maxPlayers = 4;
	private readonly List<Node3D>_players = new();
	private readonly List<MeshInstance3D> _walls = new();


	private static readonly string ShaderPath ="res://src//Shaders/xray.gdshader";
	private Shader _xrayShader;

	public override void _Ready(){
		_xrayShader = GD.Load<Shader>(ShaderPath);
		// Wait for all nodes to be ready
		ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame).OnCompleted(CollectWalls);
	}

	public override void _Process(double delta){
		if (_walls.Count == 0 || _players.Count == 0) return;
			
		UpdateWalls();
	}

	public void RegisterPlayer(Node3D player)
	{
		if(!_players.Contains(player))
			_players.Add(player);
	}

	public void UnregisterPlayer(Node3D player)
	{
		_players.Remove(player);
	}

	private void CollectWalls()
	{
		foreach(Node3D node in GetTree().GetNodesInGroup("xray_wall"))
		{
			MeshInstance3D mesh = GetMeshInstance(node);
			if (mesh != null)
			{
				EnsureShaderMaterial(mesh);
				_walls.Add(mesh);
			}
		}
	}

	private void UpdateWalls()
	{
		// Build position array
		Vector3[] positions  = new Vector3[maxPlayers];
		int playerCount = Mathf.Min(_players.Count, maxPlayers);

		for (int i = 0; i <playerCount; i++){
			positions[i] = _players[i].GlobalPosition;
		}

		// Pad Unused Slots to they don't affect the shader
		for (int i = playerCount; i < maxPlayers; i++){
            positions[i] = new Vector3(99999f, 99999f, 99999f);
		}

		foreach(MeshInstance3D mesh in _walls)
		{
			if(mesh.GetSurfaceOverrideMaterial(0) is not ShaderMaterial mat) continue;
			
			mat.SetShaderParameter("player_position", positions);
			mat.SetShaderParameter("player_count", playerCount);
			mat.SetShaderParameter("radius", defaultRadius);
			mat.SetShaderParameter("edge_softness", defaultEdgeSoftness);
		}
	}

	private static MeshInstance3D GetMeshInstance(Node node)
    {
        if (node is MeshInstance3D meshInstance)
            return meshInstance;

        foreach (Node child in node.GetChildren())
        {
            if (child is MeshInstance3D childMesh)
                return childMesh;
        }

        if (node.GetParent() is MeshInstance3D parentMesh)
            return parentMesh;

        return null;
    }

    private void EnsureShaderMaterial(MeshInstance3D mesh)
    {
        // Already has our shader material, nothing to do
        if (mesh.GetSurfaceOverrideMaterial(0) is ShaderMaterial)
            return;

        var mat = new ShaderMaterial();
        mat.Shader = _xrayShader;

        // Copy over albedo from existing StandardMaterial3D if present
        if (mesh.GetSurfaceOverrideMaterial(0) is StandardMaterial3D existing)
        {
            mat.SetShaderParameter("albedo", existing.AlbedoColor);
            mat.SetShaderParameter("texture_albedo", existing.AlbedoTexture);
        }
        else if (mesh.Mesh?.SurfaceGetMaterial(0) is StandardMaterial3D meshMat)
        {
            mat.SetShaderParameter("albedo", meshMat.AlbedoColor);
            mat.SetShaderParameter("texture_albedo", meshMat.AlbedoTexture);
        }

        mat.SetShaderParameter("radius", defaultRadius);
        mat.SetShaderParameter("edge_softness", defaultEdgeSoftness);

        mesh.SetSurfaceOverrideMaterial(0, mat);
    }
}

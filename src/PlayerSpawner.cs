// PlayerSpawner.cs
using Godot;
using System.Collections.Generic;

public partial class PlayerSpawner : Node
{
    [Export] private PackedScene playerScene;
    [Export] private Node3D spawnParent;
    private Node3D[] spawnPoints;
    
    private InputManager inputManager;
    private Dictionary<int, Node> activePlayers = new Dictionary<int, Node>();
    
    public override void _Ready()
    {
        inputManager = GetNode<InputManager>("/root/InputManager");
        
        if (inputManager == null)
        {
            GD.PrintErr("InputManager not found!");
            return;
        }

        var points = new List<Node3D>();
        foreach (Node child in GetChildren())
        {
            if (child is Node3D point)
                points.Add(point);
        }
        spawnPoints = points.ToArray();
        
        // Validate spawn points array
        if (spawnPoints == null || spawnPoints.Length < InputManager.MAX_PLAYERS)
        {
            GD.PrintErr($"Not enough spawn points! Need {InputManager.MAX_PLAYERS}, have {spawnPoints?.Length ?? 0}");
        }
        
        // Connect to input manager signals
        Input.JoyConnectionChanged += OnControllerConnectionChanged;
        
        // Use CallDeferred to spawn player after _Ready completes
        if (inputManager.IsPlayerAssigned(0))
        {
            CallDeferred(MethodName.SpawnPlayer, 0);
        }
    }
    
    private void OnControllerConnectionChanged(long device, bool connected)
    {
        int playerIndex = inputManager.GetPlayerForDevice((int)device);
        
        if (connected && playerIndex != -1)
        {
            // Use CallDeferred to avoid adding child during physics callback
            CallDeferred(MethodName.SpawnPlayer, playerIndex);
        }
        else if (!connected && playerIndex != -1)
        {
            CallDeferred(MethodName.DespawnPlayer, playerIndex);
        }
    }
    
    public void SpawnPlayer(int playerIndex)
    {
        if (activePlayers.ContainsKey(playerIndex))
        {
            GD.Print($"Player {playerIndex} already spawned");
            return;
        }
        
        if (playerScene == null)
        {
            GD.PrintErr("Player scene not set!");
            return;
        }
        
        // Instantiate player
        Node player = playerScene.Instantiate();
        
        // Add to scene FIRST (so the node is in the tree)
        if (spawnParent != null)
            spawnParent.AddChild(player);
        else
            AddChild(player);
        
        // NOW set position and rotation (after it's in the tree)
        if (player is Node3D player3D)
        {
            if (spawnPoints != null && spawnPoints.Length > playerIndex && spawnPoints[playerIndex] != null)
            {
                player3D.GlobalPosition = spawnPoints[playerIndex].GlobalPosition;
                player3D.GlobalRotation = spawnPoints[playerIndex].GlobalRotation;
                
                GD.Print($"Spawned player {playerIndex} at position {player3D.GlobalPosition}");
            }
            else
            {
                GD.PrintErr($"Invalid spawn point for player {playerIndex}");
            }
        }
        else
        {
            GD.PrintErr("Player scene root must be a Node3D (CharacterBody3D, etc.)");
        }
        
        // Set player index AFTER adding to tree (so _Ready has run and inputManager exists)
        if (player.HasMethod("SetPlayerIndex"))
        {
            player.Call("SetPlayerIndex", playerIndex);
        }
        
        activePlayers[playerIndex] = player;
        GD.Print($"Successfully spawned player {playerIndex} - Total active: {activePlayers.Count}");
    }
    
    public void DespawnPlayer(int playerIndex)
    {
        if (!activePlayers.TryGetValue(playerIndex, out Node player))
        {
            return;
        }
        
        player.QueueFree();
        activePlayers.Remove(playerIndex);
        GD.Print($"Despawned player {playerIndex}");
    }
    
    public Node GetPlayer(int playerIndex)
    {
        return activePlayers.GetValueOrDefault(playerIndex);
    }
    
    public int GetActivePlayerCount()
    {
        return activePlayers.Count;
    }
}
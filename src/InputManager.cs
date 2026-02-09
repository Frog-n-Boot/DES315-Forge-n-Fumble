// InputManager.cs
using Godot;
using System.Collections.Generic;

public partial class InputManager : Node
{
    private Dictionary<int, int> deviceToPlayer = new Dictionary<int, int>();
    private Dictionary<int, int> playerToDevice = new Dictionary<int, int>();
    private const int MAX_PLAYERS = 4;
    private const int KEYBOARD_DEVICE = -1;
    
    public override void _Ready()
    {
        Input.JoyConnectionChanged += OnJoyConnectionChanged;
        
        // Assign keyboard to player 0 by default
        AssignDeviceToPlayer(KEYBOARD_DEVICE, 0);
    }

    private void OnJoyConnectionChanged(long device, bool connected)
    {
        if (connected)
        {
            GD.Print($"Controller {device} connected");
            TryAssignDeviceToNextPlayer((int)device);
        }
        else
        {
            GD.Print($"Controller {device} disconnected");
            UnassignDevice((int)device);
        }
    }

    public bool AssignDeviceToPlayer(int device, int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= MAX_PLAYERS)
            return false;

        // Remove any existing assignment for this device
        if (deviceToPlayer.ContainsKey(device))
        {
            int oldPlayer = deviceToPlayer[device];
            playerToDevice.Remove(oldPlayer);
        }

        // Remove any device currently assigned to this player
        if (playerToDevice.ContainsKey(playerIndex))
        {
            int oldDevice = playerToDevice[playerIndex];
            deviceToPlayer.Remove(oldDevice);
        }

        deviceToPlayer[device] = playerIndex;
        playerToDevice[playerIndex] = device;
        
        GD.Print($"Assigned device {device} to player {playerIndex}");
        return true;
    }

    public bool TryAssignDeviceToNextPlayer(int device)
    {
        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            if (!playerToDevice.ContainsKey(i))
            {
                return AssignDeviceToPlayer(device, i);
            }
        }
        return false;
    }

    public void UnassignDevice(int device)
    {
        if (deviceToPlayer.TryGetValue(device, out int playerIndex))
        {
            deviceToPlayer.Remove(device);
            playerToDevice.Remove(playerIndex);
            GD.Print($"Unassigned device {device} from player {playerIndex}");
        }
    }

    public int GetPlayerForDevice(int device)
    {
        return deviceToPlayer.GetValueOrDefault(device, -1);
    }

    public int GetDeviceForPlayer(int playerIndex)
    {
        return playerToDevice.GetValueOrDefault(playerIndex, -2);
    }

    public bool IsPlayerAssigned(int playerIndex)
    {
        return playerToDevice.ContainsKey(playerIndex);
    }

    public int GetAssignedPlayerCount()
    {
        return playerToDevice.Count;
    }
}
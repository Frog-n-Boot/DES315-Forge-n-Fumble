// InputManager.cs
using Godot;
using System.Collections.Generic;

public partial class InputManager : Node
{
    private Dictionary<int, int> deviceToPlayer = new Dictionary<int, int>();
    private Dictionary<int, int> playerToDevice = new Dictionary<int, int>();
    public const int MAX_PLAYERS = 4;
    private const int KEYBOARD_DEVICE = -1;

    public override void _Ready()
    {
        Input.JoyConnectionChanged += OnJoyConnectionChanged;
        RefreshAssignments();
    }

    // Called on startup and whenever a controller connects/disconnects
    private void RefreshAssignments()
    {
        deviceToPlayer.Clear();
        playerToDevice.Clear();

        var connectedJoys = Input.GetConnectedJoypads();

        if (connectedJoys.Count == 0)
        {
            // No controllers — keyboard gets Player 0
            AssignDeviceToPlayer(KEYBOARD_DEVICE, 0);
            GD.Print("No controllers found. Keyboard assigned to Player 0.");
        }
        else
        {
            // Assign connected controllers in order, starting at Player 0
            int playerIndex = 0;
            foreach (int joyDevice in connectedJoys)
            {
                if (playerIndex >= MAX_PLAYERS) break;
                AssignDeviceToPlayer(joyDevice, playerIndex);
                GD.Print($"Controller {joyDevice} assigned to Player {playerIndex}.");
                playerIndex++;
            }
            // Keyboard is NOT assigned when controllers are present
        }
    }

    private void OnJoyConnectionChanged(long device, bool connected)
    {
        if (connected)
        {
            GD.Print($"Controller {device} connected.");
        }
        else
        {
            GD.Print($"Controller {device} disconnected.");
        }

        // Rebuild all assignments cleanly whenever topology changes
        RefreshAssignments();
    }

    public bool AssignDeviceToPlayer(int device, int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= MAX_PLAYERS)
            return false;

        // Remove existing assignment for this device
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

        GD.Print($"Assigned device {device} to Player {playerIndex}.");
        return true;
    }

    public bool TryAssignDeviceToNextPlayer(int device)
    {
        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            if (!playerToDevice.ContainsKey(i))
                return AssignDeviceToPlayer(device, i);
        }
        return false;
    }

    public void UnassignDevice(int device)
    {
        if (deviceToPlayer.TryGetValue(device, out int playerIndex))
        {
            deviceToPlayer.Remove(device);
            playerToDevice.Remove(playerIndex);
            GD.Print($"Unassigned device {device} from Player {playerIndex}.");
        }
    }

    public int GetPlayerForDevice(int device) =>
        deviceToPlayer.GetValueOrDefault(device, -1);

    public int GetDeviceForPlayer(int playerIndex) =>
        playerToDevice.GetValueOrDefault(playerIndex, -2);

    public bool IsPlayerAssigned(int playerIndex) =>
        playerToDevice.ContainsKey(playerIndex);

    public int GetAssignedPlayerCount() =>
        playerToDevice.Count;

    // Useful for player scripts to check if they should read keyboard input
    public bool IsKeyboardPlayer(int playerIndex) =>
        playerToDevice.TryGetValue(playerIndex, out int device) && device == KEYBOARD_DEVICE;
}
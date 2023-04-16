using System.Collections.Generic;

namespace Server;

sealed class ServerSaveState
{
    // Each player has one Player ID (PID) equivalent to their creature ID and player number.
    // Disconnected players are present, but asleep until they connect or die.
    public readonly List<SavedPlayerData> playerData = new();

    public readonly Dictionary<string, int> nameToPid = new();
}

using Common;
using System.Collections.Generic;

namespace Server;

sealed class ServerSaveState
{
    // Each player has one Player ID (PID) equivalent to their creature ID and player number.
    // Disconnected players are present, but asleep until they connect or die.
    public readonly List<SavedPlayerData> playerData = new();

    public readonly Dictionary<string, int> nameToPid = new();

    public class SavedPlayerData
    {
        public SavedPlayerData(SharedPlayerData shared, string hash)
        {
            Shared = shared;
            Hash = hash;
        }

        public void Update(PlayerState state)
        {
            Food = state.foodInStomach;
            FoodQuarters = state.quarterFoodPoints;
            Room = state.creature.pos.room;
            X = state.creature.pos.x;
            Y = state.creature.pos.y;
            Died = state.dead;
        }

        public SharedPlayerData Shared;
        public string Hash;

        public string Stomach = "";
        public int Food;
        public int FoodQuarters;
        public int Room;
        public int X;
        public int Y;
        public bool Died; // generate new stats on save if true
        public WorldCoordinate? KarmaFlower;
    }
}

using Common;

namespace Server;

class SavedPlayerData
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
        Room = state.creature.Room.name;
        X = state.creature.pos.x;
        Y = state.creature.pos.y;
        Died = state.dead;
    }

    public SharedPlayerData Shared;
    public string Hash;

    public string Stomach = "";
    public int Food;
    public int FoodQuarters;
    public string Room = "";
    public int X;
    public int Y;
    public bool Died; // generate new stats on save if true
    public WorldCoordinate? KarmaFlower;
}

using UnityEngine;

namespace Common;

sealed class SharedPlayerData
{
    public Color32 SkinColor = Color.white;

    // SlugcatStats
    public byte FoodMax;
    public byte FoodSleep;
    public float RunSpeed;
    public float PoleClimbSpeed;
    public float CorridorClimbSpeed;
    public float Weight;
    public float VisBonus;
    public float SneakStealth;
    public float Loudness;
    public float LungWeakness;
    public bool Ill;

    public bool EatsMeat = false;
    public bool Glows = false;
    public bool HasMark = false;

    public SlugcatStats Stats()
    {
        if (Ill) {
            // If malnourished, only override properties that malnourishment doesn't override.
            return new(0, malnourished: true) {
                maxFood = FoodMax,
                foodToHibernate = FoodMax,
                generalVisibilityBonus = VisBonus,
                visualStealthInSneakMode = SneakStealth,
                loudnessFac = Loudness,
                lungsFac = LungWeakness,
            };
        }
        // Override basically everything.
        return new(0, false) {
            maxFood = FoodMax,
            foodToHibernate = FoodSleep,
            runspeedFac = RunSpeed,
            poleClimbSpeedFac = PoleClimbSpeed,
            corridorClimbSpeedFac = CorridorClimbSpeed,
            bodyWeightFac = Weight,
            generalVisibilityBonus = VisBonus,
            visualStealthInSneakMode = SneakStealth,
            loudnessFac = Loudness,
            lungsFac = LungWeakness,
        };
    }

    public IntroPlayer ToPacket(int id, int room) => new(id, room, SkinColor.r, SkinColor.g, SkinColor.b, 
        FoodMax, FoodSleep, RunSpeed, PoleClimbSpeed, CorridorClimbSpeed, Weight, VisBonus, SneakStealth, Loudness, LungWeakness, 
        IntroPlayer.ToBitmask(Ill, EatsMeat, Glows, HasMark)
        );

    public static SharedPlayerData FromPacket(IntroPlayer p) => new() {
        SkinColor = new(p.SkinR, p.SkinG, p.SkinB, 255),
        FoodMax = p.FoodMax, FoodSleep = p.FoodSleep, RunSpeed = p.RunSpeed, PoleClimbSpeed = p.PoleClimbSpeed, CorridorClimbSpeed = p.CorridorClimbSpeed,
        Weight = p.Weight, VisBonus = p.VisBonus, SneakStealth = p.SneakStealth, Loudness = p.Loudness, LungWeakness = p.LungWeakness, Ill = p.Ill,
        EatsMeat = p.EatsMeat, Glows = p.EatsMeat, HasMark = p.HasMark
    };
}

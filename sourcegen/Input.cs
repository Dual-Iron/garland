static class Input
{
    // 0x100–0x1FF: Packets sent FROM client TO server
    // 0x200–0x3FF: Packets sent FROM server TO client
    //   0x200: Creature/object introductions, misc packets
    //   0x300: Creature/object updates

    // TODO non-player objects lol

    public const string PACKETS = """
# Sent to the server any time the client's input changes.
Input = 0x100 {
    vec Dir
    u8  Bitmask { Jump = 0x1, Throw = 0x2, Pickup = 0x4, Point = 0x8 }
}

# Sent to clients joining a game session.
EnterSession = 0x200 {
    u8  SlugcatWorld
    u16 RainbowSeed
    i32 ClientPid
    str StartingRoom
}

# Sent every 15 seconds, after `GlobalRain.rainDirectionGetTo` changes, and after a client joins. Flood speed is a constant 0.2.
SyncRain = 0x201 {
    u16 RainTimer
    u16 RainTimerMax
    f32 RainDirection
    f32 RainDirectionGetTo
}

# Sent every two seconds, after `DeathRain.deathRainMode` changes, and after a client joins. Only sent after death rain begins.
SyncDeathRain = 0x202 {
    u8  DeathRainMode
    f32 TimeInThisMode
    f32 Progression
    f32 CalmBeforeSunlight
}

# Sent after each time AntiGravity toggles on or off. Progress is set to 0 each time the packet is received.
SyncAntiGrav = 0x203 {
    bool On
    u16  Counter
    f32  From
    f32  To
}

# Tells a client to realize a room if it hasn't already.
RealizeRoom = 0x204 {
    i32 Index
}

# Tells a client to abtractize a room if it hasn't already. TODO (low-priority)
AbstractizeRoom = 0x205 {
    i32 Index
}

# Tells a client to destroy an object if it exists. TODO
DestroyObject = 0x210 {
    i32 ID
}

# Tells a client that a creature is inside a shortcut. TODO (high-priority)
SyncShortcut = 0x211 {
    i32    CreatureID
    i32    RoomID
    i32    EntranceNode
    i32    Wait
    ivec[] Positions
}

# Introduces a player to the client. TODO (next)
IntroPlayer = 0x220 {
    i32  ID
    u8   SkinR
    u8   SkinG
    u8   SkinB
    f32  RunSpeed
    f32  PoleClimbSpeed
    f32  CorridorClimbSpeed
    f32  BodyWeight
    f32  Lungs
    f32  Loudness
    f32  VisBonus
    f32  Stealth
    i32  ThrowingSkill
    bool Ill
}

# Updates a player for a client.
UpdatePlayer = 0x300 {
    i32 Room
    vec HeadPos
    vec ButtPos
    vec InputDir
    u8  InputBitmask { Jump = 0x1, Throw = 0x2, Pickup = 0x4, Point = 0x8 }
}

""";
}

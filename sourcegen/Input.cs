﻿static class Input
{
    // 0x100–0x1FF: Packets sent FROM client TO server
    // 0x200–0x3FF: Packets sent FROM server TO client

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

# Sent every 15 seconds, after `GlobalRain.rainDirectionGetTo` changes, and after a client joins.
SyncRain = 0x201 {
    i32 RainTimer
    i32 RainTimerMax
    f32 RainDirection
    f32 RainDirectionGetTo
}

# Sent every two seconds, after `DeathRain.deathRainMode` changes, and after a client joins. Only sent after death rain begins.
SyncDeathRain = 0x202 {
    u8  DeathRainMode
    f32 TimeInThisMode
    f32 Progression
    f32 CalmBeforeSunlight
    f32 Flood
    f32 FloodSpeed
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

# Tells a client to destroy an object if it exists.
DestroyObject = 0x206 {
    i32 ID
}

# Kills a creature for a client.
KillCreature = 0x207 {
    i32 ID
}

# Tells a client that a creature is inside a shortcut. TODO (high-priority)
SyncShortcut = 0x208 {
    i32    CreatureID
    i32    Room
    i32    EntranceNode
    i32    Wait
    ivec[] Positions
}

# Makes a creature grab an object.
Grab = 0x209 {
    i32 GrabbedID
    i32 GrabbedChunk
    i32 GrabberID
    i32 GraspUsed
    f32 Dominance
    u8  Bitmask { NonExclusive = 0x1, ShareWithNonExclusive = 0x2, OverrideEquallyDominant = 0x4, Pacifying = 0x8 }
}

# Makes a creature throw an object. Object-specific code may be run as needed. For instance, spears thrown by players set spearDamageBonus, and weapons call ChangeMode.
ThrowObject = 0x20A {
    i32 ID
    i32 Thrower
    i32 ThrowerGrasp
    vec Pos
    vec Vel
}

# Makes a weapon hit an object, as in Weapon::HitSomething.
HitObject = 0x20B {
    i32 ProjectileID
    vec ProjectilePos
    vec ProjectileVel
    i32 ObjectID
    u8  Chunk
    u8  Appendage
    u8  AppendageSeg
    f32 AppendageSegDistance
    vec CollisionPos
}

# Makes a weapon hit a wall, as in Weapon::HitWall(). Makes spears stick in walls if Stick = true.
HitWall = 0x20C {
    i32  ProjectileID
    vec  ProjectilePos
    vec  ProjectileVel
    bool Stick
}

# Syncs an object's rotation. Used after Weapon::SetRandomSpin() is called.
SyncRotation = 0x20D {
    i32 ID
    f32 Rot
    f32 RotSpeed
}

# Syncs and object's position.
SyncPosition = 0x20E {
    i32 ID
    u8  Chunk
    vec Pos
    vec PosLast
}

# Introduces a player to a client.
IntroPlayer = 0x250 {
    i32  ID
    i32  Room
    u8   SkinR
    u8   SkinG
    u8   SkinB
    u8   EyeR
    u8   EyeG
    u8   EyeB
    f32  Fat
    f32  Speed
    f32  Charm
    u8   FoodMax
    u8   FoodSleep
    f32  RunSpeed
    f32  PoleClimbSpeed
    f32  CorridorClimbSpeed
    f32  Weight
    f32  VisBonus
    f32  SneakStealth
    f32  Loudness
    f32  LungWeakness
    u8   Bitmask { Ill = 0x1, EatsMeat = 0x2, Glows = 0x4, HasMark = 0x8 }
}

# Updates a plyaer for a client.
UpdatePlayer = 0x251 {
    i32 ID
    bool Standing
    u8  BodyMode
    u8  Animation
    u8  AnimationFrame
    i8  FlipDirection
    i8  FlipDirectionLast
    vec HeadPos
    vec HeadVel
    vec ButtPos
    vec ButtVel
    vec InputDir0
    vec InputDir1
    vec InputDir2
    vec InputDir3
    vec InputDir4
    vec InputDir5
    vec InputDir6
    vec InputDir7
    vec InputDir8
    vec InputDir9
    u8  InputBitmask0
    u8  InputBitmask1
    u8  InputBitmask2
    u8  InputBitmask3
    u8  InputBitmask4
    u8  InputBitmask5
    u8  InputBitmask6
    u8  InputBitmask7
    u8  InputBitmask8
    u8  InputBitmask9
}

""";
}

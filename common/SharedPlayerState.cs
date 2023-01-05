using UnityEngine;

namespace Common;

sealed class SharedPlayerData
{
    public SlugcatStats Stats = new(0, false);
    public Color32 SkinColor = Color.white;
    public bool Glows = false;
    public bool HasMark = false;
}

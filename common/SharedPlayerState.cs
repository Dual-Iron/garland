using UnityEngine;

sealed class SharedPlayerData
{
    public SlugcatStats stats = new(0, false);
    public Color skinColor = Color.white;
}
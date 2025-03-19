using System.Collections.Generic;
using UnityEngine;

public enum LevelThemeType
{
    DarkTunnel = 0,
    DarkArena
}

[System.Serializable]
public class LevelTheme : MonoBehaviour
{
    [SerializeField] private List<Sprite> seamlessBackgrounds;
    [SerializeField] private LevelThemeType theme;

    // Public getter to access in other scripts
    public List<Sprite> SeamlessBackgrounds => seamlessBackgrounds;
    public LevelThemeType Theme => theme;
}

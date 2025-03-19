using System.Collections.Generic;
using UnityEngine;

public enum LevelLightLevels
{
    PitchBlack = 0,
    VeryDark,
    Dark,
    Dim,
    Normal,
    Bright,
    VeryBright
}

public class Level : MonoBehaviour
{
    public List<Sprite> BackgroundImages = new();
    public List<Sprite> SeamlessBackgrounds = new();

    public LevelLightLevels LightLevel = 0;
    public LevelSoundscapeType LevelSoundscape = 0;

    public bool HeavyFog = false;
    public Color FogColorMultiplier = new(1, 1, 1, 1);

    public static readonly float ParallaxBackgroundSizeMultiplier = 1.1f;
    public static readonly float ParallaxEffectFactor = 0.25f;

    private Vector3 _levelBotLeft;
    private Vector3 _levelTopRight;

    protected virtual void Awake()
    {
        var bgBoundaries = transform.Find("BGBoundaries");

        if (bgBoundaries != null)
        {
            var colliderArea = bgBoundaries.GetComponent<BoxCollider2D>();

            _levelBotLeft = (Vector2)transform.position + colliderArea.offset - (colliderArea.size / 2);
            _levelTopRight = (Vector2)transform.position + colliderArea.offset + (colliderArea.size / 2);
        }
    }

    protected virtual void Start()
    {
        var bgobject = transform.Find("bg");

        if (bgobject != null)
        {
            bgobject.gameObject.SetActive(false);

            Debug.Log("Level background set inactive.");
        }
    }

    protected virtual void Update()
    {
        DebugUtil.DrawCircle(_levelBotLeft, 0.1f, new Color(0, 0, 1.0f));
        DebugUtil.DrawCircle(_levelTopRight, 0.1f, new Color(0, 0, 1.0f));
    }

    public (Vector3 botLeft, Vector3 topRight) GetLevelBoundaries()
    {
        Debug.Log($"Tilemap Bounds - Bottom Left: {_levelBotLeft}, Top Right: {_levelTopRight}");
        return (_levelBotLeft, _levelTopRight);
    }

    public Vector3 GetLevelEntrance()
    {
        Transform doorway = gameObject.transform.Find("DoorwayEnter");

        if (doorway != null)
        {
            return doorway.position;
        }
        else
        {
            Debug.LogError("DoorwayEnter not found!");
        }

        return new();
    }

    public Sprite GetLevelBackground()
    {
        if (0 < BackgroundImages.Count)
        {
            return BackgroundImages[0];
        }

        return null;
    }
}
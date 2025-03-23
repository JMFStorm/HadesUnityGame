using System;
using UnityEngine;

[Serializable]
public class CutsceneData
{
    public Sprite Image;
    public AudioClip SoundEffect;
    public float DisplayTime = 5f;
    public string DisplayText = "";
    public Vector2 ScaleStart = new(1f, 1f);
    public Vector2 ScaleEnd = new(1f, 1f);
}

using System;
using UnityEngine;

[Serializable]
public class CutsceneData
{
    public Sprite Image;
    public AudioClip SoundEffect;
    public float DisplayTime = 5f;
    public string DisplayText = "";
    public float ZoomStart = 1f;
    public float ZoomEnd = 1f;
}

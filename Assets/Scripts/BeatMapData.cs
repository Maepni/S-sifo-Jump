using System;
using UnityEngine;

[Serializable]
public class BeatEntry
{
    public float time;    // segundos en la canción
    public float energy;  // 0..1
}

[Serializable]
public class BeatMap
{
    public float bpm;
    public BeatEntry[] beats;
}

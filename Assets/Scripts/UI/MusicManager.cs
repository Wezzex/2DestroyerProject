using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{

    public static MusicManager Instance { get; private set; }
    private AudioSource musicSource;
    private float volume = 0.4f;

    private void Awake()
    {
        Instance = this;
        musicSource = GetComponent<AudioSource>();
    }

    public void ChangeVolume()
    {
        volume += 0.1f;
        if (volume > 1f )
        {
            volume = 0f;
        }
        musicSource.volume = volume;
    }

    public float GetVolume()
    {
        return volume;
    }
}

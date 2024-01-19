using Assets.Scripts.Music_script;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    [SerializeField] private List<MusicClip> musicClips;

    void Start()
    {
        //init
        for(int i  = 0; i < musicClips.Count; i++) 
        {
            var musicClip = musicClips[i];
            var component = gameObject.AddComponent<AudioSource>();
            component.clip = musicClip.clip;
            component.volume = musicClip.volume;
            component.pitch = musicClip.pitch;
            musicClip.source = component;
            musicClips[i] = musicClip;
        }
    }

    public void PlayAudio(SFXClip clip)
    {
        foreach(var musicClip in musicClips)
        {
            if(musicClip.sfx == clip)
            {
                musicClip.source.Play();
                return;
            }
        }
        print("no clips");
    }
}



using Assets.Scripts.Music_script;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this is where the player can play the sounds
public class SoundPlayer : MonoBehaviour
{
    //contain a bunch of custom music clip data structure
    //that the game can call into it so that it can play it
    [SerializeField] private List<MusicClip> musicClips;

    void Start()
    {
        //init
        for(int i  = 0; i < musicClips.Count; i++) 
        {
            // set up the music clip for each music clip registered in editor
            var musicClip = musicClips[i];

            var component = gameObject.AddComponent<AudioSource>(); //create individual audio source for each SFX

            component.clip = musicClip.clip; 
            component.volume = musicClip.volume;
            component.pitch = musicClip.pitch;
            //set up the audio source vol, pitch and clip so that it is ready to play it.

            musicClip.source = component; //put it back into the music clip so that it can be called
            musicClips[i] = musicClip;
        }
    }

    //this is where the game can call to play the audio source
    public void PlayAudio(SFXClip clip)
    {
        foreach(var musicClip in musicClips)
        {
            //find the sfx clip that is related to the clip that is require to play
            if(musicClip.sfx == clip)
            {
                //and play the clip
                musicClip.source.Play();
                return;
            }
        }
        //show an error if there is no clip to play
        Debug.LogError("no clips"); 
    }
}



using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Music_script
{
    [SerializeField]
    public struct MusicClip 
    {
        public AudioClip clip;
        public AudioSource source;
        
        public float volume;
        public float pitch;
    }
}
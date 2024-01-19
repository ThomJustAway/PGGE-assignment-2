﻿using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Music_script
{
    [Serializable]
    public struct MusicClip 
    {
        public SFXClip sfx;
        public AudioClip clip;
        [HideInInspector] public AudioSource source;
        [Range(0,1)]
        public float volume;
        [Range(-3,3)]
        public float pitch;
    }
}
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class ButtonHover : MonoBehaviour , IPointerEnterHandler
    {
        [SerializeField] private SFXClip clipToPlay;

        public void OnPointerEnter(PointerEventData eventData)
        {
            GameApp.Instance.soundPlayer.PlayAudio(clipToPlay);
        }            
        
        public void ClickSound()
        {
            GameApp.Instance.soundPlayer.PlayAudio(SFXClip.buttonClick);
        }
    }
}
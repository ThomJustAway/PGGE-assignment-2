using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts
{
    //this is when the player hover on a button, it will play a hover clip.
    public class ButtonHover : MonoBehaviour , IPointerEnterHandler
    {
        [SerializeField] private SFXClip clipToPlay;

        //when the player hover on the button, play the hover clip
        public void OnPointerEnter(PointerEventData eventData)
        {
            GameApp.Instance.soundPlayer.PlayAudio(clipToPlay);
        }            
        
        //this is special case for the button for the connecting button.
        public void ClickSound()
        {
            GameApp.Instance.soundPlayer.PlayAudio(SFXClip.buttonClick);
        }
    }
}
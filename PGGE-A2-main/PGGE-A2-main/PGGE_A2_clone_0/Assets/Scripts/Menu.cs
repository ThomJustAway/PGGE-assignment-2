using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

//This is where the button get their clicking sound effect as well shifting the players to different scene
public class Menu : MonoBehaviour
{
    //if player click the single player button, send them to the singleplayer scene
    public void OnClickSinglePlayer()
    {
        var clipToPlay = SFXClip.buttonClick; 
        //make sure that they are send to the singleplayer scene
        StartCoroutine(ButtonClick("SinglePlayer", clipToPlay));
    }

    //if player click the multiplayer player button, send them to the multiplayer scene
    public void OnClickMultiPlayer()
    {
        var clipToPlay = SFXClip.buttonClick;
        //make sure that they are send to the multiplayer scene
        StartCoroutine( ButtonClick("Multiplayer_Launcher", clipToPlay));
    }

    //if the player click the return button (at the multplayer lancher scene) then send them back to the menu scene
    public void OnClickReturn()
    {
        var clipToPlay = SFXClip.buttonClick2; //different button sound
        StartCoroutine(ButtonClick("Menu" , clipToPlay));
    }
    
    IEnumerator ButtonClick(string sceneName , SFXClip clip)
    {
        GameApp.Instance.soundPlayer.PlayAudio(clip); //play the audio clip
        yield return new WaitForSeconds(1.5f); //make sure that the clip has been played before loading the scene
        SceneManager.LoadScene(sceneName); //load the next scene
    }

}

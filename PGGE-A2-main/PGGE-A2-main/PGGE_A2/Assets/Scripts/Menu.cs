using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{

    public void OnClickSinglePlayer()
    {
        var clipToPlay = SFXClip.buttonClick;

        //Debug.Log("Loading singleplayer game");
        StartCoroutine(ButtonClick("SinglePlayer", clipToPlay));
    }

    public void OnClickMultiPlayer()
    {
        var clipToPlay = SFXClip.buttonClick;

        //Debug.Log("Loading multiplayer game");
        StartCoroutine( ButtonClick("Multiplayer_Launcher", clipToPlay));
    }

    public void OnClickReturn()
    {
        var clipToPlay = SFXClip.buttonClick2;
        StartCoroutine(ButtonClick("Menu" , clipToPlay));
    }

    IEnumerator ButtonClick(string sceneName , SFXClip clip)
    {
        print(sceneName);
        GameApp.Instance.soundPlayer.PlayAudio(clip);
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(sceneName);
    }

}

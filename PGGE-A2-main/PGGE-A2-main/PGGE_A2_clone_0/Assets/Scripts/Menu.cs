using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{

    public void OnClickSinglePlayer()
    {
        //Debug.Log("Loading singleplayer game");
        StartCoroutine(ButtonClick("SinglePlayer"));
    }

    public void OnClickMultiPlayer()
    {
        //Debug.Log("Loading multiplayer game");
        StartCoroutine( ButtonClick("Multiplayer_Launcher"));
    }

    public void OnClickReturn()
    {
        StartCoroutine(ButtonClick("Menu"));
    }

    IEnumerator ButtonClick(string sceneName)
    {
        print(sceneName);
        GameApp.Instance.soundPlayer.PlayAudio(SFXClip.buttonClick);
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(sceneName);
    }

}

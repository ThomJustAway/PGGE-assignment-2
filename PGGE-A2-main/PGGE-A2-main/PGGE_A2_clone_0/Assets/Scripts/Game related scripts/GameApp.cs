using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PGGE.Patterns;
using UnityEngine.SceneManagement;

public class GameApp : Singleton<GameApp>
{
    private bool mPause;
    public SoundPlayer soundPlayer { get; private set; }

    void Start()
    {
        soundPlayer = GetComponent<SoundPlayer>();
        mPause = false;
        SceneManager.LoadScene("Menu");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GamePaused = !GamePaused;
        }
    }

    public bool GamePaused
    {
        get { return mPause; }
        set
        {
            mPause = value;
            //mOnPause?.Invoke(GamePaused);
            if (GamePaused)
            {
                Time.timeScale = 0;
            }
            else
            {
                Time.timeScale = 1;
            }
        }
    }    
    
    void OnEnable()
    {
        Debug.Log("OnEnable called");
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded2;
    }

    // called when the game terminates
    void OnDisable()
    {
        Debug.Log("OnDisable");
        SceneManager.sceneLoaded -= OnSceneLoaded2;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoaded - Scene Index: " + scene.buildIndex + " Scene Name: " + scene.name);
    }

    void OnSceneLoaded2(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Hello. Welocome to my scene.");
    }

}

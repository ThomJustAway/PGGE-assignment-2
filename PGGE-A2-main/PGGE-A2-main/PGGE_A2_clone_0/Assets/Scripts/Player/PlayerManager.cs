using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviourPunCallbacks 
{
    public static PlayerManager instance; //make it into a singleton for use in the menu

    public string mPlayerPrefabName;
    public PlayerSpawnPoints mSpawnPoints;

    [HideInInspector]
    public GameObject mPlayerGameObject;
    [HideInInspector]
    private ThirdPersonCamera mThirdPersonCamera;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            print("error with player manager");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Transform randomSpawnTransform = mSpawnPoints.GetSpawnPoint();
        mPlayerGameObject = PhotonNetwork.Instantiate(mPlayerPrefabName,
            randomSpawnTransform.position,
            randomSpawnTransform.rotation,
            0);

        mThirdPersonCamera = Camera.main.gameObject.AddComponent<ThirdPersonCamera>();

        //mPlayerGameObject.GetComponent<PlayerMovement>().mFollowCameraForward = false;
        mThirdPersonCamera.mPlayer = mPlayerGameObject.transform;
        mThirdPersonCamera.mDamping = 20.0f;
        mThirdPersonCamera.mCameraType = CameraType.Follow_Track_Pos_Rot;
    }

    public void LeaveRoom()
    {
        StartCoroutine(OnLeave());
    }

    public void RestartLevel()
    {
        Transform randomSpawnTransform = mSpawnPoints.GetSpawnPoint();
        mPlayerGameObject.transform.position = randomSpawnTransform.position;
        mPlayerGameObject.transform.rotation = randomSpawnTransform.rotation;
        //make the player spawn in different areas
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Menu");
    }

    IEnumerator OnLeave()
    {
        GameApp.Instance.soundPlayer.PlayAudio(SFXClip.buttonClick2);
        yield return new WaitForSeconds(1.5f);
        PhotonNetwork.LeaveRoom();
    }
}

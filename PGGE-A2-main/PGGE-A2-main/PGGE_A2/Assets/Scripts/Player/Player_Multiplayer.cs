using System.Collections;
using UnityEngine;
using PGGE.Patterns;
using Photon.Pun;
using System.Collections.Generic;
using System;

namespace PGGE.Player
{
    public class Player_Multiplayer : MonoBehaviour , IDamageable , IPunObservable
    {
        //for networking
        private PhotonView mPhotonView;

        //multi purpose movement
        [HideInInspector]
        public FSM mFsm = new FSM();
        public Animator mAnimator;
        public PlayerMovement mPlayerMovement;

        #region shooting variables
        // This is the maximum number of bullets that the player 
        // needs to fire before reloading.
        // This is the total number of bullets that the 
        // player has.
        // This is the count of bullets in the magazine.
        [HideInInspector]
        public bool[] mAttackButtons = new bool[3];
        public float damageFromBullet = 10;
        public GameObject mBulletPrefab;
        public float mBulletSpeed = 10.0f;
        public int[] RoundsPerSecond = new int[3];

        [HideInInspector]
        public int mBulletsInMagazine = 40;

        [HideInInspector]
        public int mAmunitionCount = 100;
        public int mMaxAmunitionBeforeReload = 40;

        public Transform mGunTransform;
        public string bulletPrefabsName;
        bool[] mFiring = new bool[3];


        //for sync the firing of players
        private Queue<Action> firingCallback;
        private bool firedBullet;
        #endregion

        
        public LayerMask mPlayerMask;
        public Canvas mCanvas;
        public RectTransform mCrossHair;
        public RectTransform HealthUI;

        #region health
        private float maxHealthUIWidth;
        private const float maxHealth = 100;
        public float health = maxHealth;
        private  bool isDead;
        public GameObject DeathCanvas;
        #endregion

        // Start is called before the first frame update
        void Start()
        {
            maxHealthUIWidth = HealthUI.rect.width; //make sure that the UI shows that it is max health
            isDead = false; //have a boolen to keep track whether the player is dead or not.
            mPhotonView = GetComponent<PhotonView>();
            PopulatingFSM();

            //this is to make sure the crosshair dont intersect with the other players.
            if (!mPhotonView.IsMine)
            {
                mCanvas.gameObject.SetActive(false);
                //for networking side
                firingCallback = new Queue<Action>();
                firedBullet = false;
            }
        }

        //this is to initalise the FSM so that the player can swap between different statss
        private void PopulatingFSM()
        {
            mFsm.Add(new PlayerState_Multiplayer_MOVEMENT(this));
            mFsm.Add(new PlayerState_Multiplayer_ATTACK(this));
            mFsm.Add(new PlayerState_Multiplayer_RELOAD(this));
            mFsm.SetCurrentState((int)PlayerStateType.MOVEMENT);
        }

        void Update()
        {
            //if it is not the client one, then dont move it
            if (isDead) return;
            if (!mPhotonView.IsMine )
            {//do data reading here
                if(firingCallback.Count > 0)
                {
                    var callback = firingCallback.Dequeue();
                    callback.Invoke();
                }
                return;
            }

            mFsm.Update();
            Aim();

            FireLogic();
        }

        private void FireLogic()
        {
            if (Input.GetButton("Fire1"))
            {
                mAttackButtons[0] = true;
                mAttackButtons[1] = false;
                mAttackButtons[2] = false;
            }
            else
            {
                mAttackButtons[0] = false;
            }

            if (Input.GetButton("Fire2"))
            {
                mAttackButtons[0] = false;
                mAttackButtons[1] = true;
                mAttackButtons[2] = false;
            }
            else
            {
                mAttackButtons[1] = false;
            }

            if (Input.GetButton("Fire3"))
            {
                mAttackButtons[0] = false;
                mAttackButtons[1] = false;
                mAttackButtons[2] = true;
            }
            else
            {
                mAttackButtons[2] = false;
            }
        }

        #region player related movement
        //for the player when they are aiming the gun at an object
        public void Aim()
        {
            Vector3 dir = -mGunTransform.right.normalized;
            // Find gunpoint as mentioned in the worksheet.
            Vector3 gunpoint = mGunTransform.transform.position +
                               dir * 1.2f -
                               mGunTransform.forward * 0.1f;
            // Fine the layer mask for objects that you want to intersect with.
            LayerMask objectsMask = ~mPlayerMask;

            // Do the Raycast
            RaycastHit hit;
            bool flag = Physics.Raycast(gunpoint, dir,
                            out hit, 50.0f, objectsMask);
            if (flag)
            {
                // Draw a line as debug to show the aim of fire in scene view.
                Debug.DrawLine(gunpoint, gunpoint +
                    (dir * hit.distance), Color.red, 0.0f);

                // Find the transformed intersected point to screenspace
                // and then transform the crosshair position to this
                // new position.
                // first you need the RectTransform component of your mCanvas
                RectTransform CanvasRect = mCanvas.GetComponent<RectTransform>();

                // then you calculate the position of the UI element.
                // Remember that 0,0 for the mCanvas is at the centre of the screen. 
                // But WorldToViewPortPoint treats the lower left corner as 0,0. 
                // Because of this, you need to subtract the height / width 
                // of the mCanvas * 0.5 to get the correct position.

                Vector2 ViewportPosition = Camera.main.WorldToViewportPoint(hit.point);
                Vector2 WorldObject_ScreenPosition = new Vector2(
                ((ViewportPosition.x * CanvasRect.sizeDelta.x) - (CanvasRect.sizeDelta.x * 0.5f)),
                ((ViewportPosition.y * CanvasRect.sizeDelta.y) - (CanvasRect.sizeDelta.y * 0.5f)));

                //now you can set the position of the UI element
                mCrossHair.anchoredPosition = WorldObject_ScreenPosition;


                // Enable or set active the crosshair gameobject.
                mCrossHair.gameObject.SetActive(true);
            }
            else
            {
                // Hide or set inactive the crosshair gameobject.
                mCrossHair.gameObject.SetActive(false);
            }
        }

        //for the player to move based on the input given by the player
        public void Move()
        {
            if (!mPhotonView.IsMine) return;

            mPlayerMovement.HandleInputs();
            mPlayerMovement.Move();
        }

        //when players have no more ammo
        public void NoAmmo()
        {

        }

        public void Reload()
        {

        }

        public void Fire(int id)
        {
            if (mFiring[id] == false)
            {
                StartCoroutine(Coroutine_Firing(id));
            }
        }

        //over here is how we make the bullet get instantiated through the network.
        public void FireBullet()
        {
            if (mBulletPrefab == null) return;
            firedBullet = true;
            //find the direction of where the bullet should fire
            Vector3 dir = -mGunTransform.right.normalized;
            Vector3 firePoint = mGunTransform.transform.position + dir *
                1.2f - mGunTransform.forward * 0.1f;

            ////we then want to make a copy of the bullet in the game. This is so that the bullet can be seen in both the recieve and shooter client
            //var bullet = PhotonNetwork.Instantiate(bulletPrefabsName , 
            //    firePoint , 
            //    Quaternion.LookRotation(dir) 
            //    * Quaternion.AngleAxis(90.0f, Vector3.right));

            var bullet = Instantiate(mBulletPrefab , 
                firePoint,
                Quaternion.LookRotation(dir) * Quaternion.AngleAxis(90.0f , Vector3.right)
                );

            //we will then add the force to the bullet so that it can move across the network.
            bullet.GetComponent<Rigidbody>().AddForce(dir * mBulletSpeed, ForceMode.Impulse);
            //player will have a capsule collider that will recieve the bullet (the character controller capsule collider does not work for some reason)
            //after it hit the player, it will damage the player.
        }

        IEnumerator Coroutine_Firing(int id)
        {
            mFiring[id] = true;
            FireBullet();
            yield return new WaitForSeconds(1.0f / RoundsPerSecond[id]);
            mFiring[id] = false;
            mBulletsInMagazine -= 1;
        }

        //implement the Idamagable so that it can take damage
        public void TakeDamage()
        {
            health -= damageFromBullet; //substract of the health;
            if(health < 0)
            {
                StartDeath();

            }
            UpdateHealthUI();
        }

        //show dead UI here
        private void StartDeath()
        {
            mAnimator.SetTrigger("Die");
            isDead = true;
            DeathCanvas.SetActive(true);
        }
        #endregion

        //If player wants to restart
        public void RestartGame()
        {
            isDead = false;
            health = maxHealth;
            DeathCanvas.SetActive(false);
            mAnimator.SetTrigger("Restart");
            UpdateHealthUI();
            PlayerManager.instance.RestartLevel();
        }

        public void QuitGame()
        {
            PlayerManager.instance.LeaveRoom();
        }

        private void UpdateHealthUI()
        {
            var normaliseValue = health / maxHealth;
            var currentUIHealth = Mathf.Lerp(0, maxHealthUIWidth , normaliseValue);
            Vector2 sizeOfRect = HealthUI.sizeDelta;
            sizeOfRect.x = currentUIHealth;
            HealthUI.sizeDelta = sizeOfRect;
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(health);
                stream.SendNext(firedBullet);
                if (firedBullet)
                {
                    firedBullet = false;
                }
            }
            else
            {
                health = (float) stream.ReceiveNext();
                bool fired = (bool)stream.ReceiveNext();
                if(fired)
                {
                    firingCallback.Enqueue(FireBullet);
                }

            }
        }
    }
}

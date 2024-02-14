using System.Collections;
using UnityEngine;
using PGGE.Patterns;
using Photon.Pun;
using System.Collections.Generic;
using System;

namespace PGGE.Player
{
    //This script is used for the player in multiplayer mode. 
    //Implement the IDamageable so that the player can damage one another
    //Implement the IPunObservable to sync data between players in the other services
    public class Player_Multiplayer : MonoBehaviour , IDamageable , IPunObservable
    {
        //for networking
        private PhotonView mPhotonView;

        //multi purpose movement
        [HideInInspector]
        public FSM mFsm = new FSM();
        public Animator mAnimator;
        public PlayerMovement mPlayerMovement;

        #region shooting related
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

        #region UI
        public LayerMask mPlayerMask;
        public Canvas mCanvas;
        public RectTransform mCrossHair;
        #endregion

        #region health
        //this is for the Health and UI
        public RectTransform HealthUI; //The Health bar. It will decrease if the player lost HP
        private float maxHealthUIWidth; //This is to calculate the remaining health the player has left
        private const float maxHealth = 100; //the max HP the player has
        public float health = maxHealth; //this is the current hp of player
        private  bool isDead; // This is triggered if the player is dead
        public GameObject DeathCanvas; //this is the canvas that the player will see if they are dead.
        #endregion

        void Start()
        {
            maxHealthUIWidth = HealthUI.rect.width; //make sure that the UI shows that it is max health
            isDead = false; //have a boolen to keep track whether the player is dead or not.
            mPhotonView = GetComponent<PhotonView>();
            PopulatingFSM(); 

            if (!mPhotonView.IsMine)
            {//if this is the player from the other servers
                mCanvas.gameObject.SetActive(false); //this is to make sure the crosshair dont intersect with the other players.
                firingCallback = new Queue<Action>(); //Enable the callback queue so that it can give the illusion of the other player is firing the bullet
                firedBullet = false; //make sure it is false so as the other player dont fire the bullet.
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
            if (isDead) return; //if the actual player is dead, then dont do anything.
            
            if (!mPhotonView.IsMine) //if it is not the client one, then dont move it but do data reading.
            {//do data reading here
                if(firingCallback.Count > 0) //if there is bullets fired from the actual player then show that bullet in the other servers
                {
                    //since the shooting bullet function is in the queue, just dequeue it from the queue and invoke it.
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

            //activate the fired bullet boolen so that the OnPhotonSerializeView will know
            //that the actual player fired the bullet.
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

        //Once the player has not enough HP, it will trigger the death function to start it's death
        private void StartDeath()
        {
            //show the death animation here
            mAnimator.SetTrigger("Die");
            if (mPhotonView.IsMine)
            {
                //this is important as to not make sure that only the client is dead bool is true as
                //when the player restart the game, the bool of the client would be false but the 
                // other servers "Player" have the isDead bool true. This would cause very weird
                //behaviours that we dont want.
                isDead = true;
                DeathCanvas.SetActive(true);
            }
        }
        #endregion

        #region buttons for end canvas
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
        //if the player press the quit game button
        public void QuitGame()
        {
            //if Player wants to leave the game, they will be transported back.
            PlayerManager.instance.LeaveRoom();
        }
        #endregion

        //this is to reflect the amount of HP the player has before dying.
        private void UpdateHealthUI()
        {
            var normaliseValue = health / maxHealth; //finding the percentage of health the player has.
            var currentUIHealth = Mathf.Lerp(0, maxHealthUIWidth , normaliseValue); 
            //this is to find the percentage of health the UI as to show in order to see the changes.

            Vector2 sizeOfRect = HealthUI.sizeDelta;
            sizeOfRect.x = currentUIHealth;
            HealthUI.sizeDelta = sizeOfRect;
            //show and display the health bar

        }

        //this is to sync the player shooting so that the both players can see the bullets.
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {//if it is the client
                stream.SendNext(health); //sync the health and bullet with the other client
                stream.SendNext(firedBullet);

                if (firedBullet)
                {
                    //once the stream has registered the bullet, reset to register the next bullet.
                    firedBullet = false;
                }
            }
            else
            {//if it is on the other servers
                health = (float) stream.ReceiveNext(); //sync the health from the actual player to the players in other servers
                bool fired = (bool)stream.ReceiveNext();

                if(fired) //if the actual player has fired, make sure to queue up a bullet to show the bullet that is fired
                {
                    //this is so that the bullet would appear in the other servers once the actual player fired the bullet.
                    firingCallback.Enqueue(FireBullet);
                }

            }
        }
    }
}

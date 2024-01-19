using UnityEngine;

namespace PGGE.Player
{
    public class PlayerState_MOVEMENT : PlayerState
    {
        public PlayerState_MOVEMENT(Player player) : base(player)
        {
            mId = (int)(PlayerStateType.MOVEMENT);
        }

        public override void Enter()
        {
            base.Enter();
        }

        public override void Update()
        {
            base.Update();

            // For Student ---------------------------------------------------//
            // Implement the logic of player movement. 
            //----------------------------------------------------------------//
            // Hint:
            //----------------------------------------------------------------//
            // You should remember that the logic for movement
            // has already been implemented in PlayerMovement.cs.
            // So, how do we make use of that?
            // We certainly do not want to copy and paste the movement 
            // code from PlayerMovement to here.
            // Think of a way to call the Move method. 
            //
            // You should also
            // check if fire buttons are pressed so that 
            // you can transit to ATTACK state.

            mPlayer.Move();

            for (int i = 0; i < mPlayer.mAttackButtons.Length; ++i)
            {
                if (mPlayer.mAttackButtons[i])
                {
                    if (mPlayer.mBulletsInMagazine > 0)
                    {
                        PlayerState_ATTACK attack =
                      (PlayerState_ATTACK)mFsm.GetState(
                                (int)PlayerStateType.ATTACK);

                        attack.AttackID = i;
                        mPlayer.mFsm.SetCurrentState(
                            (int)PlayerStateType.ATTACK);
                    }
                    else
                    {
                        Debug.Log("No more ammo left");
                    }
                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
        }
    }

}

using UnityEngine;

namespace PGGE.Player
{
    public class PlayerState_Multiplayer_MOVEMENT : PlayerState_Multiplayer
    {
        public PlayerState_Multiplayer_MOVEMENT(Player_Multiplayer player) : base(player)
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

            mPlayer.Move();
            DecidedNextState();
        }

        private void DecidedNextState()
        {
            for (int i = 0; i < mPlayer.mAttackButtons.Length; ++i)
            {
                if (mPlayer.mAttackButtons[i])
                {
                    if (mPlayer.mBulletsInMagazine > 0)
                    {
                        PlayerState_Multiplayer_ATTACK attack =
                      (PlayerState_Multiplayer_ATTACK)mFsm.GetState(
                                (int)PlayerStateType.ATTACK);

                        attack.AttackID = i; //make sure the player is shooting based on the attack button
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

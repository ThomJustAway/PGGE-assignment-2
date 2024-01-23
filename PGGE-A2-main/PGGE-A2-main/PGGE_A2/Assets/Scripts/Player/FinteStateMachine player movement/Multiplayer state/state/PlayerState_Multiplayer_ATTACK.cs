using UnityEngine;

namespace PGGE.Player
{
    public class PlayerState_Multiplayer_ATTACK : PlayerState_Multiplayer
    {
        private int mAttackID = 0;
        private string mAttackName;

        public int AttackID
        {
            get
            {
                return mAttackID;
            }
            set
            {
                mAttackID = value;
                mAttackName = "Attack" + (mAttackID + 1).ToString();
            }
        }

        public PlayerState_Multiplayer_ATTACK(Player_Multiplayer player) : base(player)
        {
            mId = (int)(PlayerStateType.ATTACK);
        }

        public override void Enter()
        {
            mPlayer.mAnimator.SetBool(mAttackName, true);
        }
        public override void Exit()
        {
            mPlayer.mAnimator.SetBool(mAttackName, false);
        }
        public override void Update()
        {
            base.Update();

            DecideNextState();

        }
        void DecideNextState()
        {
            if (mPlayer.mBulletsInMagazine == 0 && mPlayer.mAmunitionCount > 0)
            {
                mPlayer.mFsm.SetCurrentState((int)PlayerStateType.RELOAD);
                return;
            }

            if (mPlayer.mAmunitionCount <= 0 && mPlayer.mBulletsInMagazine <= 0)
            {
                mPlayer.mFsm.SetCurrentState((int)PlayerStateType.MOVEMENT);
                mPlayer.NoAmmo();
                return;
            }

            if (mPlayer.mAttackButtons[mAttackID])
            {
                mPlayer.mAnimator.SetBool(mAttackName, true);
                mPlayer.Fire(AttackID);
            }
            else
            {
                mPlayer.mAnimator.SetBool(mAttackName, false);
                mPlayer.mFsm.SetCurrentState((int)PlayerStateType.MOVEMENT);
            }
        }
    }

}

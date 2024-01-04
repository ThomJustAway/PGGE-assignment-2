using UnityEngine;

public class PlayerState_ATTACK : PlayerState
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

    public PlayerState_ATTACK(Player player) : base(player)
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

        // For Student ---------------------------------------------------//
        // Implement the logic of attack, reload and revert to movement. 
        //----------------------------------------------------------------//
        // Hint:
        //----------------------------------------------------------------//
        // 1. Transition to RELOAD
        // Notice that we have three variables, viz., 
        // mAmunitionCount
        // mBulletsInMagazine
        // mMaxAmunitionBeforeReload
        // You will need to make use of these variables while
        // implementing the transition to RELOAD.
        //
        // 2. Staying in ATTACK state
        // You should stay in ATTACK state as long as the 
        // Fire buttons are pressed. During ATTACK state
        // you should trigger the correct ATTACK animation
        // based on which button is pressed and shoot bullets.
        // Every bullet shot should reduce the count of mAmunitionCount
        // and mBulletsInMagazine.
        // Once mBulletsInMagazine reaches to 0 you should 
        // transit to RELOAD state.
        //
        // 3. Transition to MOVEMENT state
        // You should transit to MOVEMENT state when any of the 
        // following two situations happen.
        // First you have exhausted all your bullets, that means your
        // mAmunitionCount is 0 or if you do not press any of the
        // Fire buttons.
        // Discuss with your tutor if you find any difficulties
        // in implementing this section.        
        
        // For tutor - start ---------------------------------------------//
        Debug.Log("Ammo count: " + mPlayer.mAmunitionCount + ", In Magazine: " + mPlayer.mBulletsInMagazine);
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
        // For tutor - end   ---------------------------------------------//
    }
}

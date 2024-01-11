using System.Collections;
using System.Collections.Generic;
using PGGE.Patterns;

namespace PGGE.Player
{
    public class PlayerState_Multiplayer : FSMState
    {
        protected Player_Multiplayer mPlayer = null;

        public PlayerState_Multiplayer(Player_Multiplayer player) 
            : base()
        {
            mPlayer = player;
            mFsm = mPlayer.mFsm;
        }

        public override void Enter()
        {
            base.Enter();
        }
        public override void Exit()
        {
            base.Exit();
        }
        public override void Update()
        {
            base.Update();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
        }
    }

}

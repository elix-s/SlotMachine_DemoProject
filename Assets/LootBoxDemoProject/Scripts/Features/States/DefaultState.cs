using AxGrid.FSM;
using UnityEngine;

namespace Features.States
{
    [State("DefaultState")]
    public class DefaultState : FSMState
    {
        [Enter]
        public void Enter()
        {
            Debug.Log("Enter default state");
        }

        [Exit]
        public void Exit()
        {
            Debug.Log("Exit state");
        }
    }
}


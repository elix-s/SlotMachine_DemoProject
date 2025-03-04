using AxGrid;
using AxGrid.FSM;
using UnityEngine;

namespace Features.States
{
    [State("SlotStopState")]
    public class SlotStopState : FSMState
    {
        [Enter]
        public void Enter()
        {
            Settings.Invoke("OnStopSlotMachine");
        }

        [Exit]
        public void Exit()
        {
            Debug.Log("Exit state");
        }
    }
}

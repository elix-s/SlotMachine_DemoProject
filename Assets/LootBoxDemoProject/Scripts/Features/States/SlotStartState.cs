using AxGrid;
using AxGrid.FSM;
using UnityEditor;
using UnityEngine;

namespace Features.States
{
    [State("SlotStartState")]
    public class SlotStartState : FSMState
    {
        [Enter]
        public void Enter()
        {
            Settings.Invoke("OnStartSlotMachine");
        }

        [Exit]
        public void Exit()
        {
            Debug.Log("Exit state");
        }
    }
}
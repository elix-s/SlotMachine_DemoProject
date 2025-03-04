using AxGrid;
using AxGrid.Base;
using AxGrid.FSM;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Features.States;

namespace Features.SlotMachine
{
    public class SlotMachineController : MonoBehaviourExt
    {
        [SerializeField] private SlotMachineScrollComponent _slotMachineScrollComponent;
        [SerializeField] private Button _button1;
        [SerializeField] private Button _button2;
        
        private bool _canBeStopped = false;

        [OnAwake]
        private void Init()
        {
            Settings.Model.EventManager.AddAction<string>("OnStartSlotMachine", OnStartSlotMachine);
            Settings.Model.EventManager.AddAction<string>("OnStopSlotMachine", OnStopSlotMachine);
            Settings.Model.EventManager.AddAction<string>("OnStartButtonClick", OnStartButtonClick);
            Settings.Model.EventManager.AddAction<string>("OnStopButtonClick", OnStopButtonClick);
            
            CreateFsm();
            
            _button2.interactable = false;
        }
        
        private void CreateFsm()
        {
            Settings.Fsm = new FSM();
            
            Settings.Fsm.Add(new DefaultState()); 
            Settings.Fsm.Add(new SlotStartState()); 
            Settings.Fsm.Add(new SlotStopState()); 
            
            Settings.Fsm.Start("DefaultState");
        }
        
        private void OnStartButtonClick(string arg1)
        {
            Settings.Fsm.Change("SlotStartState");
        }
        
        private void OnStopButtonClick(string arg1)
        {
            Settings.Fsm.Change("SlotStopState");
        }
        
        public async void OnStartSlotMachine(string arg1)
        {
            _slotMachineScrollComponent.StartScrolling(3.0f, 3000).Forget();
            _button1.interactable = false;
            _canBeStopped = false;
            await UniTask.Delay(3000);
            _canBeStopped = true;
            _button2.interactable = true;
        }

        private async void OnStopSlotMachine(string arg1)
        {
            if (_canBeStopped)
            {
                _slotMachineScrollComponent.StopScrolling(2.0f).Forget();
                _canBeStopped = false;
                _button2.interactable = false;
                await UniTask.Delay(3000);
                _button1.interactable = true;
            }
        }
    }
}

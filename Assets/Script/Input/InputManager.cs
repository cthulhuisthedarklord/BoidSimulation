using Unity6Demo.Input.Handler;
using Unity6Demo.Input.State;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Unity6Demo.Input.Core
{
    //->InputManager->InputHandler(InputData{InputValue})->Router->StateMachine->State
    //->InputManager Create all other components and control them
    //->InputHandler Subscribe to raw input and use InputRouter to send InputData
    //->InputRouter store all InputData inside and other can access it via Dictionary
    //->InputManager->Update StateMachine, StateMachine->Update State
    //->State subscribe to InputRouter and handle InputData on their own.
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private XRRayInteractor leftRay;
        [SerializeField] private XRRayInteractor rightRay;
        [SerializeField] private LayerMask layerMask;
        [SerializeField] private Button target;
        private IInputRouter router;
        private PlayerInputAction inputActions;
        private BaseInputHandler currentHandler;
        private StateMachine stateMachine;

        private void Awake()
        {
            router = new InputRouter();
            inputActions = new PlayerInputAction();
        }

        private void Start()
        {
            InitializeInputHandler();
            InitializeStateMachine();
        }

        private void InitializeInputHandler()
        {
            if (CameraManager.Instance.IsXREnable)
            {
                currentHandler = new VRInputHandler(router, inputActions, leftRay, rightRay);
            }
            else
            {
                currentHandler = new KeyboardMouseInputHandler(router, inputActions, layerMask);
            }

            currentHandler.Initialize();
            inputActions.Enable();
        }

        private void InitializeStateMachine()
        {
            stateMachine = new StateMachine(router);
            var state = new MovementState(router);
            state.SetButton(target);
            stateMachine.TransitionTo(state);
        }

        private void Update()
        {
            currentHandler?.Update();
            stateMachine?.Update();
        }

        private void OnDestroy()
        {
            currentHandler?.Dispose();
            inputActions?.Dispose();
        }
    }
}
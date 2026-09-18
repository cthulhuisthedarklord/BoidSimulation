using Unity6Demo.Input.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity6Demo.Input.Handler
{
    public class KeyboardMouseInputHandler : BaseInputHandler
    {
        private Vector2 currentMovement;
        private float currentRiseFall;
        private bool isMoving;
        private bool isRiseFalling;
        private bool isRightMouseDown;
        private bool isLeftMouseDown;
        private LayerMask layerMask;
        private Vector2 lastMousePosition;
        public KeyboardMouseInputHandler(IInputRouter router, PlayerInputAction inputActions, LayerMask layerMask) : base(router, inputActions)
        {
            this.layerMask = layerMask;
            currentMovement = Vector2.zero;
            currentRiseFall = 0f;
            isMoving = false;
            isRiseFalling = false;
        }

        public override void Initialize()
        {
            inputActions.KeyBoard.Move.started += HandleMoveStarted;
            inputActions.KeyBoard.Move.performed += HandleMovePerformed;
            inputActions.KeyBoard.Move.canceled += HandleMoveCanceled;

            inputActions.KeyBoard.RiseFallMove.started += HandleRiseFallStarted;
            inputActions.KeyBoard.RiseFallMove.performed += HandleRiseFallPerformed;
            inputActions.KeyBoard.RiseFallMove.canceled += HandleRiseFallCanceled;

            inputActions.Mouse.LeftClick.started += HandleLeftMouseDown;
            inputActions.Mouse.LeftClick.performed += HandleLeftMouseClick;
            inputActions.Mouse.LeftClick.canceled += HandleLeftMouseUp;
            inputActions.Mouse.RightClick.started += HandleRightMouseDown;
            inputActions.Mouse.RightClick.performed += HandleRightMouseClick;
            inputActions.Mouse.RightClick.canceled += HandleRightMouseUp;
            inputActions.Mouse.Move.performed += HandleMouseMove;
        }

        public override void Dispose()
        {
            inputActions.KeyBoard.Move.started -= HandleMoveStarted;
            inputActions.KeyBoard.Move.canceled -= HandleMoveCanceled;
            inputActions.KeyBoard.Move.performed -= HandleMovePerformed;

            inputActions.KeyBoard.RiseFallMove.started -= HandleRiseFallStarted;
            inputActions.KeyBoard.RiseFallMove.canceled -= HandleRiseFallCanceled;
            inputActions.KeyBoard.RiseFallMove.performed -= HandleRiseFallPerformed;

            inputActions.Mouse.LeftClick.started -= HandleLeftMouseDown;
            inputActions.Mouse.LeftClick.performed -= HandleLeftMouseClick;
            inputActions.Mouse.LeftClick.canceled -= HandleLeftMouseUp;
            inputActions.Mouse.RightClick.started -= HandleRightMouseDown;
            inputActions.Mouse.RightClick.performed -= HandleRightMouseClick;
            inputActions.Mouse.RightClick.canceled -= HandleRightMouseUp;
            inputActions.Mouse.Move.performed -= HandleMouseMove;
        }

        public override void Update()
        {
            if (isMoving)
            {
                SendInput(InputDataFactory.CreateMoveInput(currentMovement, InputSource.Keyboard));
            }
            if (isRiseFalling)
            {
                SendInput(InputDataFactory.CreateRiseFallInput(currentRiseFall, InputSource.Keyboard));
            }
        }

        private void HandleMoveStarted(InputAction.CallbackContext context)
        {
            isMoving = true;
            SendInput(InputDataFactory.CreateMoveInput(Vector2.zero, InputSource.Keyboard));
        }

        private void HandleMoveCanceled(InputAction.CallbackContext context)
        {
            isMoving = false;
            currentMovement = Vector2.zero;
            SendInput(InputDataFactory.CreateMoveInput(Vector2.zero, InputSource.Keyboard));
            //events.OnHorizontalInput?.Invoke(Vector2.zero);
        }

        private void HandleMovePerformed(InputAction.CallbackContext context)
        {
            isMoving = true;
            currentMovement = context.ReadValue<Vector2>();
            //SendInput(InputData.CreateMoveInput(currentMovement, InputSource.Keyboard));
        }

        private void HandleRiseFallStarted(InputAction.CallbackContext context)
        {
            isRiseFalling = true;
            SendInput(InputDataFactory.CreateRiseFallInput(0f, InputSource.Keyboard));
        }

        private void HandleRiseFallCanceled(InputAction.CallbackContext context)
        {
            isRiseFalling = false;
            currentRiseFall = 0f;
            SendInput(InputDataFactory.CreateRiseFallInput(0f, InputSource.Keyboard));
        }

        private void HandleRiseFallPerformed(InputAction.CallbackContext context)
        {
            currentRiseFall = context.ReadValue<float>();
        }

        private void HandleLeftMouseDown(InputAction.CallbackContext context)
        {
            isLeftMouseDown = true;
        }

        private void HandleLeftMouseClick(InputAction.CallbackContext context)
        {
            //SendInput(InputDataFactory.CreateButtonInput(true, InputSource.Mouse));
            //events.OnLeftMouseClick?.Invoke();
            SendInput(InputDataFactory.CreateMainButtonInput(true, InputSource.LeftMouse));
        }

        private void HandleLeftMouseUp(InputAction.CallbackContext context)
        {
            isLeftMouseDown = false;
        }

        private void HandleRightMouseClick(InputAction.CallbackContext context)
        {
            //SendInput(InputData.CreateButtonInput(true, InputSource.Mouse));
            //events.OnRightMouseClick?.Invoke();
        }

        private void HandleRightMouseDown(InputAction.CallbackContext context)
        {
            isRightMouseDown = true;
            //Debug.Log("Right Mouse Down");
        }

        private void HandleRightMouseUp(InputAction.CallbackContext context)
        {
            isRightMouseDown = false;
            //Debug.Log("Right Mouse Up");
        }

        private void HandleMouseMove(InputAction.CallbackContext context)
        {
            Vector2 currentMousePosition = context.ReadValue<Vector2>();
            Vector3 currentMouseRotation = new Vector3(currentMousePosition.x, currentMousePosition.y, 0);
            Ray ray = Camera.main.ScreenPointToRay(currentMousePosition);
            //Debug.Log("Mouse Move");
            if (Physics.Raycast(ray, out RaycastHit hit, layerMask))
            {
                //Debug.Log("RayCast Target" + hit.point);
                SendInput(InputDataFactory.CreateTargetInput(hit.point, hit.transform, InputSource.Mouse));
            }

            if (isRightMouseDown)
            {
                Vector2 mouseDelta = currentMousePosition - lastMousePosition;
                //Debug.Log("Right Mouse Hold and move");
                SendInput(InputDataFactory.CreateRotateInput(mouseDelta, InputSource.RightMouse));
            }
            if (isLeftMouseDown)
            {

            }
            lastMousePosition = currentMousePosition;
        }
    }
}
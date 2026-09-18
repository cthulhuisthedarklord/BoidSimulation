using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity6Demo.Input.Core;
namespace Unity6Demo.Input.Handler
{
    public class VRInputHandler : BaseInputHandler
    {
        private readonly XRRayInteractor leftRay;
        private readonly XRRayInteractor rightRay;
        private bool isLeftTriggerPressed;
        private bool isRightTriggerPressed;
        private bool isRisefall;
        private float currentRiseFall;
        private bool isMoving;
        private Vector2 currentMove;

        public VRInputHandler(IInputRouter router, PlayerInputAction inputActions, XRRayInteractor leftRay, XRRayInteractor rightRay) : base(router, inputActions)
        {
            this.leftRay = leftRay;
            this.rightRay = rightRay;
        }

        public override void Initialize()
        {
            var controller = inputActions.Quest3Controller;

            controller.LeftPrimaryButton.performed += OnLeftButtonPerformed;
            controller.RightPrimaryButton.performed += OnRightButtonPerformed;

            // Triggers
            controller.LeftTrigger.started += OnLeftTriggerStarted;
            controller.LeftTrigger.canceled += OnLeftTriggerCanceled;
            controller.RightTrigger.started += OnRightTriggerStarted;
            controller.RightTrigger.canceled += OnRightTriggerCanceled;

            // Grips
            controller.LeftGrip.started += OnLeftGripStarted;
            controller.LeftGrip.canceled += OnLeftGripCanceled;
            controller.RightGrip.started += OnRightGripStarted;
            controller.RightGrip.canceled += OnRightGripCanceled;

            // Joysticks
            controller.LeftJoyStick.started += OnLeftStickStarted;
            controller.LeftJoyStick.performed += OnLeftStickPerformed;
            controller.LeftJoyStick.canceled += OnLeftStickCanceled;
            controller.RightJoyStick.started += OnRightStickStarted;
            controller.RightJoyStick.performed += OnRightStickPerformed;
            controller.RightJoyStick.canceled += OnRightStickCanceled;

            controller.RiseAndFall.started += OnRiseAndFallStarted;
            controller.RiseAndFall.performed += OnRiseAndFallPerformed;
            controller.RiseAndFall.canceled += OnRiseAndFallCanceled;
        }

        public override void Update()
        {
            // If pressed, also send target input
            if (isLeftTriggerPressed && leftRay.TryGetCurrent3DRaycastHit(out RaycastHit leftHit))
            {
                SendInput(InputDataFactory.CreateTargetInput(
                    leftHit.point,
                    leftHit.transform,
                    InputSource.VRLeftController
                ));
            }

            if (isRightTriggerPressed && rightRay.TryGetCurrent3DRaycastHit(out RaycastHit rightHit))
            {
                SendInput(InputDataFactory.CreateTargetInput(
                    rightHit.point,
                    rightHit.transform,
                    InputSource.VRRightController
                ));
            }

            if (isRisefall)
            {
                SendInput(InputDataFactory.CreateRiseFallInput(currentRiseFall, InputSource.VRLeftController));
            }
            if (isMoving)
            {
                SendInput(InputDataFactory.CreateMoveInput(currentMove, InputSource.VRLeftController));
            }
        }

        public override void Dispose()
        {
            var controller = inputActions.Quest3Controller;

            controller.LeftPrimaryButton.performed -= OnLeftButtonPerformed;
            controller.RightPrimaryButton.performed -= OnRightButtonPerformed;
            // Triggers
            controller.LeftTrigger.started -= OnLeftTriggerStarted;
            controller.LeftTrigger.canceled -= OnLeftTriggerCanceled;
            controller.RightTrigger.started -= OnRightTriggerStarted;
            controller.RightTrigger.canceled -= OnRightTriggerCanceled;

            // Grips
            controller.LeftGrip.started -= OnLeftGripStarted;
            controller.LeftGrip.canceled -= OnLeftGripCanceled;
            controller.RightGrip.started -= OnRightGripStarted;
            controller.RightGrip.canceled -= OnRightGripCanceled;

            // Joysticks
            controller.LeftJoyStick.started -= OnLeftStickStarted;
            controller.LeftJoyStick.performed -= OnLeftStickPerformed;
            controller.LeftJoyStick.canceled -= OnLeftStickCanceled;
            controller.RightJoyStick.started -= OnRightStickStarted;
            controller.RightJoyStick.performed -= OnRightStickPerformed;
            controller.RightJoyStick.canceled -= OnRightStickCanceled;
        }

        private void OnLeftTriggerStarted(InputAction.CallbackContext ctx)
        {
            isLeftTriggerPressed = true;
            SendInput(InputDataFactory.CreateTriggerInput(ctx.ReadValue<float>(), InputSource.VRLeftController));
        }
        private void OnLeftTriggerCanceled(InputAction.CallbackContext ctx)
        {
            isLeftTriggerPressed = false;
            SendInput(InputDataFactory.CreateTriggerInput(0f, InputSource.VRLeftController));
        }
        private void OnRightTriggerStarted(InputAction.CallbackContext ctx)
        {
            isRightTriggerPressed = true;
            SendInput(InputDataFactory.CreateTriggerInput(ctx.ReadValue<float>(), InputSource.VRRightController));
        }
        private void OnRightTriggerCanceled(InputAction.CallbackContext ctx)
        {
            isRightTriggerPressed = false;
            SendInput(InputDataFactory.CreateTriggerInput(0f, InputSource.VRRightController));
        }

        private void OnLeftGripStarted(InputAction.CallbackContext ctx)
        {
            SendInput(InputDataFactory.CreateGripInput(ctx.ReadValue<float>(), InputSource.VRLeftController));
        }
        private void OnLeftGripCanceled(InputAction.CallbackContext ctx)
        {
            SendInput(InputDataFactory.CreateGripInput(0f, InputSource.VRLeftController));
        }
        private void OnRightGripStarted(InputAction.CallbackContext ctx)
        {
            SendInput(InputDataFactory.CreateTriggerInput(ctx.ReadValue<float>(), InputSource.VRRightController));
        }
        private void OnRightGripCanceled(InputAction.CallbackContext ctx)
        {
            SendInput(InputDataFactory.CreateGripInput(0f, InputSource.VRRightController));
        }
        private void OnLeftButtonPerformed(InputAction.CallbackContext ctx)
        {
            SendInput(InputDataFactory.CreateMainButtonInput(true, InputSource.VRLeftController));
        }
        private void OnRightButtonPerformed(InputAction.CallbackContext ctx)
        {
            SendInput(InputDataFactory.CreateMainButtonInput(true, InputSource.VRRightController));
            if (rightRay.TryGetCurrent3DRaycastHit(out RaycastHit rightHit))
            {
                SendInput(InputDataFactory.CreateTargetButtonInput(
                    rightHit.point,
                    rightHit.transform,
                    InputSource.VRRightController
                ));
            }
        }
        private void OnLeftStickStarted(InputAction.CallbackContext ctx)
        {
            isMoving = true;
        }
        private void OnLeftStickPerformed(InputAction.CallbackContext ctx)
        {
            currentMove = ctx.ReadValue<Vector2>();
            //SendInput(InputDataFactory.CreateMoveInput(currentMove, InputSource.VRLeftController));
        }
        private void OnLeftStickCanceled(InputAction.CallbackContext ctx)
        {
            isMoving = false;
            //SendInput(InputDataFactory.CreateMoveInput(Vector2.zero, InputSource.VRLeftController));
        }
        private void OnRightStickStarted(InputAction.CallbackContext ctx)
        {
            Debug.Log("Right Stick started");
            //SendInput(InputDataFactory.CreateMoveInput(Vector2.zero, InputSource.VRRightController));
        }
        private void OnRightStickPerformed(InputAction.CallbackContext ctx)
        {
            Debug.Log("Right Stick perform" + ctx.ReadValue<Vector2>());

        }
        private void OnRightStickCanceled(InputAction.CallbackContext ctx)
        {
            Debug.Log("Right Stick cancel");
            SendInput(InputDataFactory.CreateMoveInput(Vector2.zero, InputSource.VRRightController));
        }

        private void OnRiseAndFallStarted(InputAction.CallbackContext ctx)
        {
            isRisefall = true;
            Debug.Log("Rise Fal Start!");
            //SendInput(InputDataFactory.CreateRiseFallInput(0f, InputSource.VRLeftController));
        }

        private void OnRiseAndFallPerformed(InputAction.CallbackContext ctx)
        {

            currentRiseFall = ctx.ReadValue<float>();
            Debug.Log("Rise Fal Performed!" + currentRiseFall);
        }

        private void OnRiseAndFallCanceled(InputAction.CallbackContext ctx)
        {
            isRisefall = false;
            Debug.Log("Rise Fal Cancel!");
            //SendInput(InputDataFactory.CreateRiseFallInput(0f, InputSource.VRLeftController));
        }
    }
}
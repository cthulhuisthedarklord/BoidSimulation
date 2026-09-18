using System;
using UnityEngine;
namespace Unity6Demo.Input.Core
{
    public enum InputActionType
    {
        // Movement
        Move,
        Rotate,
        RiseFall,

        // Interaction
        Target,
        Spawn,
        Select,
        MainButton,
        SecondButton,
        TargetButton,


        // UI
        ToggleMenu,

        // VR Specific
        Grip,
        Trigger
    }

    [Flags]
    public enum InputSource
    {
        None = 0,

        // Base input types
        Keyboard = 1 << 0,
        Mouse = 1 << 1,
        VRController = 1 << 2,

        // Controller specifics
        LeftHand = 1 << 10,
        RightHand = 1 << 11,

        // Commonly used combinations
        LeftMouse = Mouse | LeftHand,
        RightMouse = Mouse | RightHand,
        KeyboardMouse = Keyboard | Mouse,
        VRLeftController = VRController | LeftHand,
        VRRightController = VRController | RightHand
    }

    public readonly struct InputData
    {
        private readonly InputActionType actionType;
        private readonly IInputValue value;
        private readonly InputSource source;
        private readonly float timestamp;

        public InputActionType ActionType => actionType;
        public InputSource Source => source;
        public float Timestamp => timestamp;

        public bool IsVRInput => (source & InputSource.VRController) != 0;
        public bool IsLeftHand => (source & InputSource.LeftHand) != 0;
        public bool IsRightHand => (source & InputSource.RightHand) != 0;
        public bool IsLeftMouse => (source & InputSource.LeftMouse) != 0;
        public bool IsRightMouse => (source & InputSource.RightMouse) != 0;

        public InputData(InputActionType actionType, IInputValue value, InputSource source)
        {
            this.actionType = actionType;
            this.value = value;
            this.source = source;
            this.timestamp = Time.time;
        }

        public bool TryGetValue<T>(out T result) where T : IInputValue
        {
            if (value is T typedValue)
            {
                result = typedValue;
                return true;
            }
            result = default;
            return false;
        }
    }
}
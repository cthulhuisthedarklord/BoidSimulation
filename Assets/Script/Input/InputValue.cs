using UnityEngine;
using UnityEngine.InputSystem;
namespace Unity6Demo.Input.Core
{
    public interface IInputValue { }

    public readonly struct ButtonInputValue : IInputValue
    {
        public readonly bool IsPressed { get; }
        public ButtonInputValue(bool isPressed) => IsPressed = isPressed;
    }

    public readonly struct SpawnInputValue : IInputValue
    {
        public readonly bool IsPressed { get; }
        public SpawnInputValue(bool isPressed) => IsPressed = isPressed;
    }

    public readonly struct RiseFallInputValue : IInputValue
    {
        public readonly float RiseFall { get; }
        public RiseFallInputValue(float riseFall) => RiseFall = riseFall;
    }

    public readonly struct GripInputValue : IInputValue
    {
        public readonly float Pressure { get; }
        public GripInputValue(float pressure) => Pressure = pressure;
    }


    public readonly struct MoveInputValue : IInputValue
    {
        public readonly Vector2 Movement { get; }
        public MoveInputValue(Vector2 movement) => Movement = movement;
    }

    public readonly struct RotateInputValue : IInputValue
    {
        public readonly Vector3 Rotation { get; }
        public RotateInputValue(Vector3 rotation) => Rotation = rotation;
    }

    public readonly struct TriggerInputValue : IInputValue
    {
        public readonly float Pressure { get; }
        public TriggerInputValue(float pressure) => Pressure = pressure;
    }

    public readonly struct TargetInputValue : IInputValue
    {
        public readonly Vector3 Position { get; }
        public readonly Transform HitTarget { get; }
        public TargetInputValue(Vector3 position, Transform hitTarget)
        {
            Position = position;
            HitTarget = hitTarget;
        }
    }

    public readonly struct TargetButtonInputValue : IInputValue
    {
        public readonly Vector3 Position { get; }
        public readonly Transform HitTarget { get; }
        public TargetButtonInputValue(Vector3 position, Transform hitTarget)
        {
            Position = position;
            HitTarget = hitTarget;
        }
    }

    public static class InputDataFactory
    {
        public static InputData CreateMoveInput(Vector2 movement, InputSource source) =>
            new InputData(InputActionType.Move, new MoveInputValue(movement), source);
        public static InputData CreateRotateInput(Vector3 rotation, InputSource source) =>
           new InputData(InputActionType.Rotate, new RotateInputValue(rotation), source);
        public static InputData CreateRiseFallInput(float riseFall, InputSource source) =>
            new InputData(InputActionType.RiseFall, new RiseFallInputValue(riseFall), source);
        public static InputData CreateTargetInput(Vector3 position, Transform hitTarget, InputSource source) =>
            new InputData(InputActionType.Target, new TargetInputValue(position, hitTarget), source);
        public static InputData CreateTargetButtonInput(Vector3 position, Transform hitTarget, InputSource source) =>
            new InputData(InputActionType.TargetButton, new TargetButtonInputValue(position, hitTarget), source);
        public static InputData CreateGripInput(float pressure, InputSource source) =>
           new InputData(InputActionType.Grip, new GripInputValue(pressure), source);
        public static InputData CreateTriggerInput(float pressure, InputSource source) =>
            new InputData(InputActionType.Trigger, new TriggerInputValue(pressure), source);
        public static InputData CreateMainButtonInput(bool isPressed, InputSource source) =>
            new InputData(InputActionType.MainButton, new ButtonInputValue(isPressed), source);
        public static InputData CreateSpawnInput(bool isPressed, InputSource source) =>
            new InputData(InputActionType.Spawn, new SpawnInputValue(isPressed), source);
    }
}
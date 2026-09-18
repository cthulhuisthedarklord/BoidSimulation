using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity6Demo.Input.Core
{
    public interface IInputRouter
    {
        void RouteInput(InputData data);
        void Subscribe(InputActionType action, Action<InputData> handler);
        void Unsubscribe(InputActionType action, Action<InputData> handler);
    }

    public class InputRouter : IInputRouter
    {
        private readonly Dictionary<InputActionType, List<Action<InputData>>> subscribers
        = new Dictionary<InputActionType, List<Action<InputData>>>();

        public void RouteInput(InputData data)
        {
            if (subscribers.TryGetValue(data.ActionType, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        handler(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error handling input {data.ActionType}: {e}");
                    }
                }
            }
        }

        public void Subscribe(InputActionType action, Action<InputData> handler)
        {
            if (!subscribers.ContainsKey(action))
            {
                subscribers[action] = new List<Action<InputData>>();
            }
            subscribers[action].Add(handler);
        }

        public void Unsubscribe(InputActionType action, Action<InputData> handler)
        {
            if (subscribers.TryGetValue(action, out var handlers))
            {
                handlers.Remove(handler);
            }
        }
    }
}
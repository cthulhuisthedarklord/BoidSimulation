using Unity6Demo.Input.Core;
namespace Unity6Demo.Input.State
{
    public class StateMachine
    {
        IInputRouter router;
        private MovementState movementState;
        private IInputState currentState;
        public StateMachine(IInputRouter router)
        {
            this.router = router;
        }

        public void TransitionTo(IInputState newState)
        {
            currentState?.Exit();
            currentState = newState;
            currentState.Enter();
        }

        public void Update()
        {
            currentState?.Update();
        }
    }
}
namespace Unity6Demo.Input.State
{
    public interface IInputState
    {
        public void Enter();
        public void Update();
        public void Exit();
    }
}
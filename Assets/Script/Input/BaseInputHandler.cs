using Unity6Demo.Input.Core;

namespace Unity6Demo.Input.Handler
{
    public abstract class BaseInputHandler : System.IDisposable
    {
        protected readonly IInputRouter router;
        protected readonly PlayerInputAction inputActions;

        public BaseInputHandler(IInputRouter router, PlayerInputAction inputActions)
        {
            this.router = router;
            this.inputActions = inputActions;
        }

        protected void SendInput(InputData data)
        {
            router.RouteInput(data);
        }

        public abstract void Initialize();
        public abstract void Update();
        public abstract void Dispose();
    }
}
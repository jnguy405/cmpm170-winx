namespace Utilities
{
    public abstract class IdleState
    {
        public virtual void Enter() { }

        public virtual void Update() { }

        public virtual void Exit() { }
    }
}

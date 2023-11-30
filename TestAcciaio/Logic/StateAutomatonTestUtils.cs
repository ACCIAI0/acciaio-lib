using Acciaio.Logic;

namespace Test.Acciaio.Logic;

public static class StateAutomatonTestUtils
{
    public sealed class TestState : State
    {
        private readonly Action _onEnter;
        private readonly Action _onExit;
        private readonly Action _onTick;
        
        public override bool IsActive { get; protected set; }

        public override bool FinishedExecution => true;

        public TestState(Action onEnter, Action onExit, Action onTick)
        {
            _onEnter = onEnter;
            _onExit = onExit;
            _onTick = onTick;
        }

        protected override void OnEnter() => _onEnter();

        protected override void OnTick() => _onTick();

        protected override void OnExit() => _onExit();
    }

    public sealed class StepsState : State
    {
        private readonly int _toReach;

        private bool _finished;
        
        public int Current { get; private set; }
        
        public override bool IsActive { get; protected set; }
        
        public override bool FinishedExecution => _finished;

        public StepsState(int toReach) => _toReach = Math.Max(toReach, 1);
        
        protected override void OnEnter() { }

        protected override void OnTick()
        {
            Current++;
            _finished = Current == _toReach;
        }

        protected override void OnExit() => Current = 0;
    }
}
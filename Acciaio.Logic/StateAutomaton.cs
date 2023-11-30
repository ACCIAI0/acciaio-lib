using System.Diagnostics;

namespace Acciaio.Logic;

public sealed class StateAutomaton
{
#region Internal

    private abstract class Transition
    {
        private readonly Func<bool> _predicate;

        public readonly int Priority;
        
        public abstract State TargetState { get; }
        
        protected Transition(Func<bool> predicate, int priority)
        {
            _predicate = predicate;

            Priority = priority;
        }
        
        public bool CheckCondition() => _predicate();
    }

    private sealed class BackwardsTransition : Transition
    {
        private readonly StateAutomaton _automaton;

        public override State TargetState
        {
            get
            {
                Debug.Assert(_automaton._previousState != null, "_automaton._previousState != null");
                return _automaton._previousState;
            }
        }

        public BackwardsTransition(StateAutomaton automaton, Func<bool> predicate, int priority) : base(predicate, priority) 
            => _automaton = automaton;
    }
    
    private sealed class StaticTransition : Transition
    {
        public override State TargetState { get; }
        
        public StaticTransition(State destination, Func<bool> predicate, int priority) : base(predicate, priority)
            => TargetState = destination;
    }
    
#endregion

    private static void AddInOrder(LinkedList<Transition> transitions, Transition element)
    {
        if (transitions.Count == 0) transitions.AddFirst(element);

        if (transitions.Last?.Value.Priority <= element.Priority)
        {
            transitions.AddLast(element);
            return;
        }

        var node = transitions.First;

        while (node != null)
        {
            if (node.Value.Priority >= element.Priority)
            {
                transitions.AddBefore(node, element);
                return;
            }

            node = node.Next;
        }
    }

    /// <summary>
    /// Raised whenever a state transition happens and its argument is the name of the state the automaton has transitioned into.
    /// This event is raised after the call to the specific state's OnEnter().
    /// </summary>
    public event Action<string>? StateChanged;
    
    private readonly Dictionary<State, State> _sequentialTransitions = new();
    private readonly LinkedList<Transition> _globalTransitions = new(); 
    private readonly Dictionary<State, LinkedList<Transition>> _conditionalTransitions = new();
    private readonly HashSet<State> _statesWithBackwardsTransitions = new();

    private State? _previousState;
    private State? _entryState;
    private AsyncAutomaton? _async;
    private bool _locked;
    
    public State? CurrentState { get; private set; }

    public State? EntryState
    {
        get => _entryState;
        set
        {
            if(value is not null && _statesWithBackwardsTransitions.Contains(value))
                throw new ArgumentException("State is already used in backwards transitions, it can't be used as entry point");
            _entryState = value;
        }
    }

    public AsyncAutomaton AsAsync => _async ??= new(this, locked => _locked = locked);

    public StateAutomaton(State entryState) => _entryState = entryState;
    
    private void InitializeState(State state) => state.IsActive = CurrentState?.Equals(state) ?? false;
    
    private void ChangeState(State state)
    {
        _previousState = CurrentState;
        if (_previousState != null)
        {
            _previousState.OnExit();
            _previousState.IsActive = false;
        }

        CurrentState = state;
        CurrentState.IsActive = true;
        CurrentState.OnEnter();

        StateChanged?.Invoke(CurrentState.Name);
    }
    
    private bool ExecuteFirstValidTransition(IEnumerable<Transition> transitions, bool excludeSelf)
    {
        foreach (var transition in transitions)
        {
            var canCheck = !excludeSelf || !transition.TargetState.Equals(CurrentState);
            if (!canCheck || !transition.CheckCondition()) continue;
            ChangeState(transition.TargetState);
            return true;
        }
        return false;
    }
    
    private bool TryStartAutomaton()
    {
        if (CurrentState is not null) return false;
        if (EntryState is null) throw new InvalidOperationException("can't start the automaton from a null entry state");
        
        ChangeState(EntryState);
        return true;
    }
    
    private bool TrySequentialTransition()
    {
        if (CurrentState is null) return false;
        if (!_sequentialTransitions.ContainsKey(CurrentState) || !CurrentState.FinishedExecution) return false;
        
        ChangeState(_sequentialTransitions[CurrentState]);
        return true;
    }

    private bool TryGlobalTransition() => ExecuteFirstValidTransition(_globalTransitions, true);

    private bool TryConditionalTransition()
    {
        return CurrentState is not null && 
               _conditionalTransitions.ContainsKey(CurrentState) && 
               ExecuteFirstValidTransition(_conditionalTransitions[CurrentState], false);
    }
    
    private void AddConditionalTransition(State from, Transition transition)
    {
        if (!_conditionalTransitions.ContainsKey(from))
            _conditionalTransitions.Add(from, new());
        if (_conditionalTransitions[from].Contains(transition))
            throw new ArgumentException("Transition already added");
        AddInOrder(_conditionalTransitions[from], transition);
            
        InitializeState(from);
        InitializeState(transition.TargetState);
    }
    
    public void SetSequentialTransition(State from, State to)
    {
        if (from is null)
            throw new ArgumentNullException(nameof(from), "Cannot register a transition from a null state");
        _sequentialTransitions[from] = to ?? throw new ArgumentNullException(nameof(to), "Cannot register a transition to a null state");

        InitializeState(from);
        InitializeState(to);
    }
    
    public void AddGlobalTransition(State to, Func<bool> predicate, int priority = 0)
    {
        if (to is null) 
            throw new ArgumentNullException(nameof(to), "Cannot register a transition to a null state");
        if (predicate is null) 
            throw new ArgumentNullException(nameof(predicate), "Cannot register a transition with a null predicate");

        var transition = new StaticTransition(to, predicate, priority);
        if (_globalTransitions.Any(transition.Equals)) return;
        
        AddInOrder(_globalTransitions, transition);
        InitializeState(to);
    }
    
    public void AddBackwardTransition(State from, Func<bool> predicate, int priority = 0)
    {
        if (from.Equals(EntryState))
            throw new ArgumentException($"{from.Name} cannot be used in a backward transition and as entry state");
        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate), "Cannot register a transition with a null predicate");
        AddConditionalTransition(from, new BackwardsTransition(this, predicate, priority));
        _statesWithBackwardsTransitions.Add(from);
    }
    
    public void AddConditionalTransition(State from, State to, Func<bool> predicate, int priority = 0)
    {
        if (to is null)
            throw new ArgumentNullException(nameof(to), "Cannot register a transition to a null state");
        AddConditionalTransition(from, new StaticTransition(to, predicate, priority));
    }
    
    public void Reset() {
        CurrentState = null;
        EntryState = null;

        _conditionalTransitions.Clear();
        _globalTransitions.Clear();
        _sequentialTransitions.Clear();
        _statesWithBackwardsTransitions.Clear();
    }
    
    public void Tick()
    {
        if (_locked)
            throw new InvalidOperationException("This state automaton is already being ticked on a separate thread");
        
        var stateChanged = TryStartAutomaton();
        if (!stateChanged)
            stateChanged = TryConditionalTransition();
        if (!stateChanged)
            stateChanged = TryGlobalTransition();
        if (!stateChanged) 
            stateChanged = TrySequentialTransition();
        
        CurrentState?.OnTick();
    }
}

public sealed class AsyncAutomaton
{
    private readonly StateAutomaton _automaton;
    private readonly Action<bool> _lockMechanism;

    private Action? _callback;
    private bool _queued;

    public State? CurrentState => _automaton.CurrentState;

    public bool IsTicking => _queued;

    internal AsyncAutomaton(StateAutomaton automaton, Action<bool> lockMechanism)
    {
        _automaton = automaton;
        _lockMechanism = lockMechanism;
        _callback = null;
        _queued = false;
    }

    private void AsyncTick(object? stateInfo)
    {
        _automaton.Tick();
        _lockMechanism(false);
        
        _callback?.Invoke();
        _queued = false;
    }

    public bool Tick(Action? callback)
    {
        if (_queued) return false;
        _queued = true;
        _callback = callback;
        _lockMechanism(true);
        return ThreadPool.QueueUserWorkItem(AsyncTick);
    }

    public void Tick() => Tick(null);
}
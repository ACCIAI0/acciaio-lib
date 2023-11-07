namespace Acciaio.Logic;

public abstract class State
{
    private StateAutomaton? _automaton;
    private string? _name;
        
    public abstract bool IsActive { get; protected internal set; }

    public abstract bool FinishedExecution { get; }
        
    public virtual string Name => _name ??= GetType().Name.Replace("State", string.Empty);

    internal void SetAutomaton(StateAutomaton automaton)
    {
        if (_automaton is not null) throw new MultiStateAutomatonsException(Name);
        _automaton = automaton;
    }
        
    protected internal abstract void OnEnter();

    protected internal abstract void OnTick();

    protected internal abstract void OnExit();

    public override bool Equals(object? obj) 
        => obj is State state && state.Name.Equals(Name, StringComparison.Ordinal);

    public override int GetHashCode() => Name.GetHashCode();
}
using Acciaio.Logic;

namespace Test.Acciaio.Logic;

public class StateAutomatonTests
{
    private const int Steps = 3;
    
    [Fact]
    public void CanCreateNew()
    {
        var state = new StateAutomatonTestUtils.StepsState(Steps);
        var automaton = new StateAutomaton(state);

        Assert.Null(automaton.CurrentState);
        Assert.NotNull(automaton.EntryState);
        
        automaton.Tick();
            
        Assert.NotNull(automaton.CurrentState);
        Assert.Equal(state, automaton.CurrentState);
        
        Assert.Equal(1, state.Current);
    }

    [Fact]
    public void CanCreateSequentialTransition()
    {
        var state1 = new StateAutomatonTestUtils.StepsState(Steps);
        var state2 = new StateAutomatonTestUtils.StepsState(Steps);
        var automaton = new StateAutomaton(state1);
        
        automaton.SetSequentialTransition(state1, state2);
        
        automaton.Tick();
        
        Assert.Equal(state1, automaton.CurrentState);
        
        automaton.Tick();
        automaton.Tick();
        automaton.Tick();
        
        Assert.Equal(state2, automaton.CurrentState);
        
        Assert.Equal(1, state2.Current);
    }

    [Fact]
    public void CanCreateConditionalTransition()
    {
        var state1IsOut = false;
        var state1 = new StateAutomatonTestUtils.TestState(() => { }, () => state1IsOut = true, () => {});
        var state2 = new StateAutomatonTestUtils.StepsState(Steps);
        var automaton = new StateAutomaton(state1);

        var change = false;
        
        automaton.AddConditionalTransition(state1, state2, () => change);
        
        automaton.Tick();
        
        Assert.Equal(state1, automaton.CurrentState);
        
        automaton.Tick();
        
        Assert.Equal(state1, automaton.CurrentState);

        change = true;
        
        automaton.Tick();
        
        Assert.Equal(state2, automaton.CurrentState);
        
        Assert.True(state1IsOut);
    }

    [Fact]
    public void CanCreateAGobalTransition()
    {
        var state1IsOut = false;
        var state1Ticks = 0;
        var state2IsOut = false;
        var state2Ticks = 0;
        var state1 = new StateAutomatonTestUtils.TestState(() => { }, () => state1IsOut = true, () => state1Ticks++);
        var state2 = new StateAutomatonTestUtils.TestState(() => { }, () => state2IsOut = true, () => state2Ticks++);
        var state3 = new StateAutomatonTestUtils.StepsState(Steps);
        
        var automaton = new StateAutomaton(state1);
        var change = false;
        
        automaton.AddGlobalTransition(state3, () => change);
        automaton.SetSequentialTransition(state3, state2);
        
        automaton.Tick();
        
        Assert.Equal(state1, automaton.CurrentState);

        change = true;
        
        automaton.Tick();
        
        Assert.Equal(state3, automaton.CurrentState);
        Assert.True(state1IsOut);
        
        automaton.Tick();
        automaton.Tick();
        automaton.Tick();
        
        Assert.Equal(state2, automaton.CurrentState);
        Assert.Equal(0, state3.Current);
        
        automaton.Tick();
        
        Assert.Equal(state3, automaton.CurrentState);
        Assert.True(state2IsOut);
        
        Assert.Equal(1, state1Ticks);
        Assert.Equal(1, state2Ticks);
        Assert.Equal(1, state3.Current);
    }
}
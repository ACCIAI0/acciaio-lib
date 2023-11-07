namespace Acciaio.Logic;

public class MultiStateAutomatonsException : Exception
{
    public MultiStateAutomatonsException(string stateName) : 
        base($"State {stateName} is already assigned to a StateAutomaton") { }
}
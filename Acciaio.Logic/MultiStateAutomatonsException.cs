namespace Acciaio.Logic;

public class MultiStateAutomatonsException(string stateName) : 
    Exception($"State {stateName} is already assigned to a StateAutomaton");
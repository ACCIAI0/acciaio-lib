namespace Acciaio.Types;

public sealed class Result<TValue, TError> where TError : Error
{
    public static Result<TValue, TError> Success(TValue value) => new(value, null);

    public static Result<TValue, TError> Failure(TError error) => new(default, error);
    
    private readonly TValue? _value;
    private readonly TError? _error;

    public bool IsSuccess => _error is null;
    
    public TError Error 
        => _error ?? throw new InvalidOperationException("Can't get error of a successful result");
    
    internal Result(TValue? value, TError? error)
    {
        _value = value;
        _error = error;
    }

    public Task<Result<TValueMap, TError>> MapAsync<TValueMap>(Func<TValue, TValueMap> map)
        => MapAsync(v => Task.FromResult(map(v)));

    public Result<TValueMap, TError> Map<TValueMap>(Func<TValue, TValueMap> map)
        => MapAsync(map).GetAwaiter().GetResult();

    public async Task<Result<TValueMap, TError>> MapAsync<TValueMap>(Func<TValue, Task<TValueMap>> mapAsync)
    {
        return IsSuccess
            ? Result<TValueMap, TError>.Success(await mapAsync(this))
            : Result<TValueMap, TError>.Failure(Error);
    }

    public Task<Result<TValue, TErrorMap>> MapErrorAsync<TErrorMap>(Func<TError, TErrorMap> map) 
        where TErrorMap : Error => MapErrorAsync(v => Task.FromResult(map(v)));
    
    public Result<TValue, TErrorMap> MapError<TErrorMap>(Func<TError, TErrorMap> map) where TErrorMap : Error
        => MapErrorAsync(map).GetAwaiter().GetResult();
    
    public async Task<Result<TValue, TErrorMap>> MapErrorAsync<TErrorMap>(Func<TError, Task<TErrorMap>> mapAsync) 
        where TErrorMap : Error
    {
        return IsSuccess 
            ? Result<TValue, TErrorMap>.Success(this) 
            : Result<TValue, TErrorMap>.Failure(await mapAsync(Error));
    }
    
    public Task<Result<TValueMap, TErrorMap>> MapAsync<TValueMap, TErrorMap>(
        Func<TValue, TValueMap> valueMap,
        Func<TError, TErrorMap> errorMap) where TErrorMap : Error
    {
        return MapAsync(v => Task.FromResult(valueMap(v)), e => Task.FromResult(errorMap(e)));
    }
    
    public Result<TValueMap, TErrorMap> Map<TValueMap, TErrorMap>(
        Func<TValue, TValueMap> valueMap,
        Func<TError, TErrorMap> errorMap) where TErrorMap : Error
    {
        return MapAsync(valueMap, errorMap).GetAwaiter().GetResult();
    }

    public async Task<Result<TValueMap, TErrorMap>> MapAsync<TValueMap, TErrorMap>(
        Func<TValue, Task<TValueMap>> valueMapAsync,
        Func<TError, Task<TErrorMap>> errorMapAsync) where TErrorMap : Error
    {
        return IsSuccess
            ? Result<TValueMap, TErrorMap>.Success(await valueMapAsync(this))
            : Result<TValueMap, TErrorMap>.Failure(await errorMapAsync(Error));
    }
    
    public async Task<T> MatchAsync<T>(Func<TValue, Task<T>> valueMatchAsync, Func<TError, Task<T>> errorMatchAsync)
    {
        return IsSuccess 
            ? await valueMatchAsync(this) 
            : await errorMatchAsync(Error);
    }
    
    public Task<T> MatchAsync<T>(Func<TValue, T> valueMatch, Func<TError, T> errorMatch) 
        => MatchAsync(v => Task.FromResult(valueMatch(v)), e => Task.FromResult(errorMatch(e)));

    public T Match<T>(Func<TValue, T> valueMatch, Func<TError, T> errorMatch)
        => MatchAsync(valueMatch, errorMatch).GetAwaiter().GetResult();

    public async Task DoAsync(Func<TValue, Task>? valueDoAsync, Func<TError, Task>? errorDoAsync)
    {
        if (IsSuccess)
        {
            if (valueDoAsync is not null)
                await valueDoAsync(this);
        }
        else if (errorDoAsync is not null) await errorDoAsync.Invoke(Error);
    }
    
    public Task DoAsync(Action<TValue> valueDo, Action<TError> errorDo)
        => DoAsync(v => Task.Run(() => valueDo(v)), e => Task.Run(() => errorDo(e)));

    public void Do(Action<TValue>? valueDo, Action<TError>? errorDo)
    {
        if (IsSuccess) valueDo?.Invoke(this);
        else errorDo?.Invoke(Error);
    }
    
    public async Task DoAsync(Func<TValue, Task> valueDoAsync) => await DoAsync(valueDoAsync, null);
    
    public async Task DoAsync(Func<TError, Task> errorDoAsync) => await DoAsync(null, errorDoAsync);

    public Task DoAsync(Action<TValue> valueDo) => DoAsync(v => Task.Run(() => valueDo(v)));
    
    public Task DoAsync(Action<TError> errorDo) => DoAsync(null, e => Task.Run(() => errorDo(e)));
    
    public void Do(Action<TValue> valueDo) => Do(valueDo, null);

    public static implicit operator TValue(Result<TValue, TError> result)
    {
        return result is { IsSuccess: true, _value: not null }
            ? result._value
            : throw new InvalidOperationException("Can't get value of a failed result");
    }
}
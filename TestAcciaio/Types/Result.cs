using Acciaio.Types;

namespace Test.Acciaio.Types;

public class Result
{
    [Fact]
    public void CanCreate()
    {
        var success = Result<int, Error>.Success(0);
        var error = Result<int, Error>.Failure(Error.Generic);
        
        Assert.True(success.IsSuccess);
        Assert.False(error.IsSuccess);
        
        Assert.Equal(0, success);
        Assert.Equal(Error.Generic, error.Error);
    }
}
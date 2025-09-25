namespace Acciaio.Types;

public class Error
{
    public static readonly Error Generic = new("An error occurred");
    
    public readonly string Message;
    
    public Error(string message)
    {
        if (string.IsNullOrEmpty(message))
            throw new ArgumentException("Can't use a null or empty message", nameof(message));
        Message = message;
    }
}
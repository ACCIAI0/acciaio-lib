using System.Text;

namespace Acciaio.Data;

public static class StringBuilderExtensions
{
    public static bool EndsWith(this StringBuilder builder, ReadOnlySpan<char> span)
    {
        if (builder.Length < span.Length) return false;

        for (var i = span.Length - 1; i >= 0; i--)
        {
            if (builder[builder.Length - span.Length + i] != span[i]) 
                return false;
        }
        return true;
    }
}
using System.Text;

namespace FlowSynx.Plugins.Compression.Zip.Extensions;

internal static class StringExtensions
{
    public static bool IsBase64String(this string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length % 4 != 0 
                                        || value.Contains(' ') || value.Contains('\t') 
                                        || value.Contains('\r') || value.Contains('\n'))
            return false;

        var index = value.Length - 1;

        if (value[index] == '=')
            index--;

        if (value[index] == '=')
            index--;

        for (var i = 0; i <= index; i++)
            if (IsInvalid(value[i]))
                return false;

        return true;
    }

    private static bool IsInvalid(char value)
    {
        var intValue = (int)value;
        switch (intValue)
        {
            case >= 48 and <= 57:
            case >= 65 and <= 90:
            case >= 97 and <= 122:
                return false;
            default:
                return intValue != 43 && intValue != 47;
        }
    }

    public static byte[] ToByteArray(this string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }

    public static byte[] Base64ToByteArray(this string value)
    {
        return Convert.FromBase64String(value);
    }
}
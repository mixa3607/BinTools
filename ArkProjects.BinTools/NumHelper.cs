namespace ArkProjects.BinTools;

public static class NumHelper
{
    public static long ParseI64(string src)
    {
        if (src.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            return Convert.ToInt64(src, 16);
        if (src.StartsWith("0b", StringComparison.InvariantCultureIgnoreCase))
            return Convert.ToInt64(src, 2);
        return Convert.ToInt64(src, 10);
    }

    public static byte ParseB(string src)
    {
        if (src.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            return Convert.ToByte(src, 16);
        if (src.StartsWith("0b", StringComparison.InvariantCultureIgnoreCase))
            return Convert.ToByte(src, 2);
        return Convert.ToByte(src, 10);
    }
}
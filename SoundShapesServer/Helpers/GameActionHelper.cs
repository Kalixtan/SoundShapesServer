namespace SoundShapesServer.Helpers;

public static class GameActionHelper
{
    public static bool TrySplit(string arguments, out string id, out string action)
    {
        int separator = arguments.LastIndexOf('.');
        if (separator <= 0 || separator == arguments.Length - 1)
        {
            id = action = string.Empty;
            return false;
        }
        id = arguments[..separator];
        action = arguments[(separator + 1)..];
        return true;
    }
}

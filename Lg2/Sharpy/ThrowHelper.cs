namespace Lg2.Sharpy;

static class ThrowHelper
{
    internal static void ThrowInvalidNullInstance<TThrower>()
    {
        var throwerName = typeof(TThrower).Name;
        throw new InvalidOperationException($"Invalid {throwerName}, instance is null");
    }
}

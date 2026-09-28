namespace ValetaxTest.Core;

public static class Timestamps
{
    /// <summary>
    /// Converts to UTC and cuts to microseconds (the precision PostgreSQL stores), so a time received
    /// again in a retried request equals the stored one.
    /// </summary>
    public static DateTimeOffset Normalize(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(utc.Ticks - utc.Ticks % 10, TimeSpan.Zero);
    }
}

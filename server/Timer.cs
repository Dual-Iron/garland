namespace Server;

struct Timer
{
    public int Length;
    public int Time;

    public static Timer WithPeriod(int lengthInTicks) => new() { Length = lengthInTicks };

    /// <summary>Ticks the timer once.</summary>
    /// <returns>True if the timer has completed a period.</returns>
    public bool Tick()
    {
        return Time++ >= Length;
    }

    /// <summary>Resets the timer.</summary>
    public void Reset()
    {
        Time = 0;
    }
}

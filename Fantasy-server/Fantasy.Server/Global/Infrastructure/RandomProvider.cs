namespace Fantasy.Server.Global.Infrastructure;

public class RandomProvider : IRandomProvider
{
    public int Next(int minValue, int maxValue) => Random.Shared.Next(minValue, maxValue);
}

namespace Fantasy.Server.Global.Infrastructure;

public interface IRandomProvider
{
    int Next(int minValue, int maxValue);
}

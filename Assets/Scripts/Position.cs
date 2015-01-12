using System.Collections.Generic;

public static class Position
{

    public static IEnumerable<long> MakeRectangle(int fromX, int fromY, int toX, int toY)
    {
        for (int x = fromX; x <= toX; x++)
        {
            for (int y = fromY; y <= toY; y++)
            {
                yield return Make(x, y);
            }
        }
    }

    public static long Make(int x, int y)
    {
        return ((long) x << 32) | y;
    }

    public static int GetX(this long p)
    {
        return (int) (p >> 32);
    }

    public static int GetY(this long p)
    {
        return (int) (p & 0xffffffff);
    }

}

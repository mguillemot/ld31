using System;
using System.Collections.Generic;
using System.Linq;

public static class Utils
{

    public static void Incr<T>(this Dictionary<T, int> dict, T key, int value)
    {
        int current;
        if (dict.TryGetValue(key, out current))
        {
            dict[key] = current + value;
        }
        else
        {
            dict[key] = value;
        }
    }

    public static T RandomElement<T>(this List<T> list)
    {
        if (list.Count == 1)
        {
            return list[0];
        }
        var idx = UnityEngine.Random.Range(0, list.Count);
        return list[idx];
    }

    public static T RandomElement<T>(this T[] list)
    {
        if (list.Length == 1)
        {
            return list[0];
        }
        var idx = UnityEngine.Random.Range(0, list.Length);
        return list[idx];
    }

    public static string ProperEnumeration(this List<string> items)
    {
        if (items.Count == 0)
        {
            return null;
        }
        if (items.Count == 1)
        {
            return items[0];
        }
        return string.Join(", ", items.Take(items.Count - 1).ToArray()) + " and " + items[items.Count - 1];
    }

    public static T TryParse<T>(string repr, T defaultValue) where T : struct, IConvertible
    {
        if (repr == null)
        {
            return defaultValue;
        }
        try
        {
            // dirty: cannot parse numbers as enums
            int i;
            if (int.TryParse(repr, out i))
            {
                return defaultValue;
            }

            return (T)Enum.Parse(typeof(T), repr);
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }

    public static string PathRepr(this List<long> path)
    {
        if (path == null)
        {
            return "NONE";
        }
        return string.Join(" => ", path.Select(p => p.GetX() + ":" + p.GetY()).ToArray());
    }

}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProbabilityTable<T>
{

    private readonly List<T> values = new List<T>();
    private readonly List<float> weights = new List<float>();

    public float totalWeight
    {
        get { return weights.Sum(); }
    }

    public int count
    {
        get { return values.Count; }
    }

    public ProbabilityTable<T> Add(T value, float weight)
    {
        // Try to merge with existing entry
        for (int i = 0, n = values.Count; i < n; i++)
        {
            var v = values[i];
            if (v.Equals(value))
            {
                weights[i] += weight;
                return this;
            }
        }

        // Or else, create a new entry
        values.Add(value);
        weights.Add(weight);
        return this;
    }

    public T ChooseOne()
    {
        if (weights.Count == 0)
        {
            return default(T);
        }

        var roll = Random.Range(0, totalWeight);
        var i = 0;
        while (roll > 0f && i < values.Count)
        {
            roll -= weights[i];
            if (roll < 0f)
            {
                return values[i];
            }
            i++;
        }
        return default(T);
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Decor
{
    public string name;
    public float frequency;
}

[System.Serializable]
public class BiomeData : MonoBehaviour
{
    public string biome_name;
    public Material material;
    // public float scale
    [HideInInspector] public PolygonCollider2D rainTempArea;
    public float dev;
    public Decor[] decorations;
    public float density;
}

public struct Biome
{
    public static Dictionary<string, int> s_indexes;
    public static GameObject[] s_decorations;

    public static void Init()
    {
        s_decorations = Resources.LoadAll<GameObject>("Decorations");

        s_indexes = new Dictionary<string, int>();
        for(int i=0; i<s_decorations.Length; i++)
        {
            var name = s_decorations[i].name;
            s_indexes.Add(name, i);
        }
    }

    public int[] decorationIndexes;
    public float[] decorationThreshholds;
    public Material material;

    public Biome(BiomeData biomeData)
    {
        decorationIndexes = new int[biomeData.decorations.Length];
        material = biomeData.material;
        decorationThreshholds = new float[biomeData.decorations.Length];

        for(int i=0; i<decorationIndexes.Length; i++)
        {
            var name = biomeData.decorations[i].name;
            decorationIndexes[i] = s_indexes.ContainsKey(name) ? s_indexes[name] : 0;
            decorationThreshholds[i] = biomeData.decorations[i].frequency;
            if(i > 0) decorationThreshholds[i] += decorationThreshholds[i-1];
        }
        for(int i=0; i<decorationThreshholds.Length; i++)
        {
            decorationThreshholds[i] /= decorationThreshholds[decorationThreshholds.Length-1];
        }
    }
}
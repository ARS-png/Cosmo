
using UnityEditorInternal;
using UnityEngine;

public static class RandomXT
{
    public static Vector3 RandomVector3(Vector3 min, Vector3 max)
    {
        return new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), Random.Range(min.z, max.z));
    }


    public static Color RandomColor(Color min, Color max)
    {
        return new Color(Random.Range(min.r, max.r), Random.Range(min.g, max.g),
            Random.Range(min.b, max.b), Random.Range(min.a, max.a));
    }

    public static bool RandomBool()
    {
        return Random.Range(0f, 1f) > 0.5f;
    }

    public static Gradient RandomGradient(Color[] colors)
    {
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[colors.Length];
        GradientColorKey[] colorKeys = new GradientColorKey[colors.Length];

        float time = 0f;
        float increment = 1f / (float)colors.Length;
        for (int i = 0; i < colors.Length; ++i)
        {
            alphaKeys[i] = new GradientAlphaKey(colors[i].a, time);
            colorKeys[i] = new GradientColorKey(colors[i], time);
            time += increment;
        }


        Gradient gradient = new Gradient();
        gradient.alphaKeys = alphaKeys;
        gradient.colorKeys = colorKeys;
        return gradient;
    }


    public static Color RandomColor()
    {
        Color new_color = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.5f, 1f, 0.2f, 0.9f);
        return new_color;
    }


    public static Vector3 RandomUnitVector3()
    {
        return new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
    }


}




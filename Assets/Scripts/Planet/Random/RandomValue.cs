using UnityEditor.SettingsManagement;
using UnityEngine;

[System.Serializable]
public class RandomValue<T>
{
    [SerializeField]
    protected T min;
    [SerializeField]
    protected T max;
    [HideInInspector]
    public T lastValue;

    public RandomValue(T min, T max)
    {
        this.min = min;   
        this.max = max;
    }

    public virtual T PickRandomValue()
    {
        return min;
    }
}

[System.Serializable]
public class RandomInt : RandomValue<int>
{
    public RandomInt(int min, int max) : base(min, max) { }

    public override int PickRandomValue()
    {
        return lastValue = Random.Range(min, max);
    }
}


[System.Serializable]
public class RandomFloat : RandomValue<float>
{
    public RandomFloat(float min, float max) : base(min, max) { }

    public override float PickRandomValue()
    {
        return lastValue = Random.Range(min, max);
    }
}


[System.Serializable]
public class RandomColor : RandomValue<Color>
{
    public RandomColor(Color min, Color max) : base(min, max) { }

    public override Color PickRandomValue()
    {     
        return lastValue = RandomXT.RandomColor(min, max);
    }
}


[System.Serializable]
public class RandomSimpleNoise : RandomValue<NoiseSettings.SimpleNoiseSettings>
{
    public RandomSimpleNoise(NoiseSettings.SimpleNoiseSettings min, NoiseSettings.SimpleNoiseSettings max) : base(min, max) { }
    public override NoiseSettings.SimpleNoiseSettings PickRandomValue()
    {
        NoiseSettings.SimpleNoiseSettings settings = new NoiseSettings.SimpleNoiseSettings();

        settings.baseRoughness = Random.Range(min.baseRoughness, max.baseRoughness);
        settings.minValue = Random.Range(min.minValue, max.minValue);
        settings.numLayers = Random.Range(min.numLayers, max.numLayers);
        settings.persistence = Random.Range(min.persistence, max.persistence);
        settings.roughness = Random.Range(min.roughness, max.roughness);
        settings.strength = Random.Range(min.strength, max.strength);
        settings.center = RandomXT.RandomVector3(min.center, max.center);

        return lastValue = settings;
    }
}


[System.Serializable]
public class RandomRigidNoise : RandomValue<NoiseSettings.RigidNoiseSettings>
{
    public RandomRigidNoise(NoiseSettings.RigidNoiseSettings min, NoiseSettings.RigidNoiseSettings max) : base(min, max) { }
    public override NoiseSettings.RigidNoiseSettings PickRandomValue()
    {
        NoiseSettings.RigidNoiseSettings settings = new NoiseSettings.RigidNoiseSettings();

        settings.baseRoughness = Random.Range(min.baseRoughness, max.baseRoughness);
        settings.minValue = Random.Range(min.minValue, max.minValue);
        settings.numLayers = Random.Range(min.numLayers, max.numLayers);
        settings.persistence = Random.Range(min.persistence, max.persistence);
        settings.roughness = Random.Range(min.roughness, max.roughness);
        settings.strength = Random.Range(min.strength, max.strength);
        settings.center = RandomXT.RandomVector3(min.center, max.center);
        settings.weightMultiplier = 100;


        return lastValue = settings;
    }
}


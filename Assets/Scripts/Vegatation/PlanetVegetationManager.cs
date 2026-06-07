using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Planet))]
public class PlanetVegetationManager : MonoBehaviour
{
    // Структура для настройки каждого отдельного вида растения в инспекторе Unity
    [System.Serializable]
    public struct PlantTypeSettings
    {
        public string name;            // Красивое имя (например: "Обычная Трава", "Красные Цветы", "Кусты")
        public Mesh lod0;              // Меш высокой детализации (близко)
        public Mesh lod1;              // Меш средней детализации (средняя дистанция)
        public Mesh lod2;              // Меш низкой детализации (далеко)
        public Material material;      // Уникальный материал/шейдер для этого растения
        public int maxInstances;       // Лимит на количество объектов (например, 100000 для травы, 5000 для цветов)

        [Header("Настройки разделения по шуму")]
        [Range(0f, 1f)] public float noiseMin; // С какого значения глобального шума растение начинает расти
        [Range(0f, 1f)] public float noiseMax; // На каком значении шума растение прекращает расти
    }

    [Header("Список всех видов растений на планете")]
    public List<PlantTypeSettings> plantTypes;

    // Внутренний список рабочих рендереров (по одному GrassRenderer на каждый тип растения)
    private List<VegetationRenderer> activeRenderers = new List<VegetationRenderer>();
    private Planet planet;
    private bool isInitialized = false;

    void Awake()
    {
        planet = GetComponent<Planet>();
    }

    /// <summary>
    /// Инициализирует систему растительности для всех типов растений сразу.
    /// Вызывается один раз при старте или генерации планеты.
    /// </summary>
    public void InitializeVegetation(Vector3 planetCenter, Vector3 localUp)
    {
        // 1. Очищаем старые буферы, если они были в памяти
        ReleaseAllVegetationBuffers();

        if (plantTypes == null || plantTypes.Count == 0) return;

        // 2. Создаем для каждого растения свой личный рабочий GrassRenderer
        foreach (var plant in plantTypes)
        {
            if (plant.material == null || plant.lod0 == null) continue;

            // Создаем стандартный GrassRenderer (твой класс остается без изменений!)
            VegetationRenderer renderer = new VegetationRenderer(planet, localUp);

            // Инициализируем его личными мешами и материалом
            renderer.Initialize(plant.lod0, plant.lod1, plant.lod2, plant.material, plant.maxInstances, planetCenter);

            // Передаем уникальные настройки порогов шума в Compute-шейдер этого конкретного растения
            var cs = planet.grassSettings.grassComputeShader;
            if (cs != null)
            {
                cs.SetFloat("_NoiseMin", plant.noiseMin);
                cs.SetFloat("_NoiseMax", plant.noiseMax);
            }

            // Добавляем настроенный рендерер в список управления
            activeRenderers.Add(renderer);
        }

        isInitialized = true;
    }

    /// <summary>
    /// Передает сгенерированные вершины меша чанка планеты во все рендереры растений.
    /// Вызывается из скрипта чанка, когда его геометрия обновилась.
    /// </summary>
    public void UpdateAllGeometry(List<TerrainFace.GrassVertexData> vertexData, Matrix4x4 localMatrix)
    {
        if (!isInitialized || activeRenderers == null) return;

        // Отдаем один и тот же набор вершин ВСЕМ рендерерам растений.
        // Каждый из них внутри своего Compute-шейдера выберет только свои точки по шуму.
        foreach (var renderer in activeRenderers)
        {
            renderer.UpdateGeometry(vertexData, localMatrix);
        }
    }

    /// <summary>
    /// Основной цикл отрисовки. Синхронно запускает RenderMeshIndirect для каждого вида травы/растений.
    /// </summary>
    void Update()
    {
        if (!isInitialized || activeRenderers == null || activeRenderers.Count == 0) return;

        // Каждый кадр заставляем каждое растение выполнить отрисовку.
        // Твой метод Render сам знает, как собрать данные камеры и вызвать Graphics.RenderMeshIndirect
        foreach (var renderer in activeRenderers)
        {
            renderer.Render(null, null, null, 0);
        }
    }

    // Защита от утечек памяти: очищаем видеобуферы видеокарты при удалении планеты или выходе из игры
    void OnDestroy()
    {
        ReleaseAllVegetationBuffers();
    }

    /// <summary>
    /// Полностью освобождает память всех буферов внутри контейнеров данных.
    /// </summary>
    private void ReleaseAllVegetationBuffers()
    {
        isInitialized = false;

        if (activeRenderers == null) return;

        foreach (var renderer in activeRenderers)
        {
            if (renderer != null)
            {
                // Вызываем метод очистки буферов (убедись, что в твоем GrassRenderer этот метод очищает GrassDataContainer)
                renderer.ReleaseGrassBuffers();
            }
        }

        activeRenderers.Clear();
    }
}

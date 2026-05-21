using UnityEngine;
using UnityEditor;

public class MeshPivotEditor : EditorWindow
{
    // Переменная для ручной регулировки смещения
    private static float customOffset = 0f;

    [MenuItem("Tools/Move Mesh Pivot Wizard")]
    public static void ShowWindow()
    {
        // Открывает удобное окошко для настройки
        GetWindow<MeshPivotEditor>("Pivot Editor");
    }

    private void OnGUI()
    {
        GUILayout.Label("Настройка смещения пивота", EditorStyles.boldLabel);

        // Поле ввода для офсета (можно вводить и отрицательные значения)
        customOffset = EditorGUILayout.FloatField("Дополнительный Офсет (Y):", customOffset);

        GUILayout.Space(10);

        if (GUILayout.Button("Применить и Сохранить Меш", GUILayout.Height(30)))
        {
            MovePivotAndSave(customOffset);
        }
    }

    private static void MovePivotAndSave(float offsetValue)
    {
        GameObject target = Selection.activeGameObject;
        if (target == null)
        {
            Debug.LogError("Сначала выберите объект травы на сцене!");
            return;
        }

        MeshFilter filter = target.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
        {
            Debug.LogError("На выбранном объекте нет компонента MeshFilter!");
            return;
        }

        Mesh meshCopy = Instantiate(filter.sharedMesh);
        Vector3[] vertices = meshCopy.vertices;

        float minY = float.MaxValue;
        foreach (Vector3 v in vertices) if (v.y < minY) minY = v.y;


        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].y -= (minY + offsetValue);
        }

        meshCopy.vertices = vertices;
        meshCopy.RecalculateBounds();

        string baseName = filter.sharedMesh.name.Replace("(Clone)", "");
        string savePath = $"Assets/{baseName}_CustomPivot.asset";
        savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);

        AssetDatabase.CreateAsset(meshCopy, savePath);
        AssetDatabase.SaveAssets();

        filter.mesh = meshCopy;

        Debug.Log($"Успех! Меш сохранен с офсетом {offsetValue}: {savePath}");
        EditorGUIUtility.PingObject(meshCopy);
    }
}

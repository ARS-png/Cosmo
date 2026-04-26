using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlanetGenerator))]
public class PlanetGeneratorEditor : Editor
{
    PlanetGenerator planetGenerator;
    Editor planetEditor;

    public override void OnInspectorGUI()
    {
        // 1. Рисуем стандартные поля (включая слоты для ColorSettings и ShapeSettings)
        using (var check = new EditorGUI.ChangeCheckScope())
        {
            base.OnInspectorGUI();
        }


        // 2. Рисуем вложенные редакторы
        DrawSettingsEditor(planetGenerator.settings, null, ref planetGenerator.settingsFoldout, ref planetEditor);
    }

    void DrawSettingsEditor(Object settings, System.Action onSettingsUpdated, ref bool foldout, ref Editor editor)
    {
        if (settings != null)
        {
            foldout = EditorGUILayout.InspectorTitlebar(foldout, settings);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                if (foldout)
                {
                    CreateCachedEditor(settings, null, ref editor);
                    editor.OnInspectorGUI();

                    if (check.changed)
                    {
                        onSettingsUpdated?.Invoke();
                    }
                }
            }
        }
    }

    private void OnEnable()
    {
        planetGenerator = (PlanetGenerator)target;
    }
}

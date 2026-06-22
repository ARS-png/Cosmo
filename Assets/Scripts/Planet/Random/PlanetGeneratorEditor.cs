//using UnityEngine;
//using UnityEditor;

//[CustomEditor(typeof(RandPlanetGenarator))]
//public class PlanetGeneratorEditor : Editor
//{
//    RandPlanetGenarator planetGenerator;
//    Editor planetEditor;

//    public override void OnInspectorGUI()
//    {
//        using (var check = new EditorGUI.ChangeCheckScope())
//        {
//            base.OnInspectorGUI();
//        }


     
//        DrawSettingsEditor(planetGenerator.rndSettings, null, ref planetGenerator.settingsFoldout, ref planetEditor);
//    }

//    void DrawSettingsEditor(Object settings, System.Action onSettingsUpdated, ref bool foldout, ref Editor editor)
//    {
//        if (settings != null)
//        {
//            foldout = EditorGUILayout.InspectorTitlebar(foldout, settings);

//            using (var check = new EditorGUI.ChangeCheckScope())
//            {
//                if (foldout)
//                {
//                    CreateCachedEditor(settings, null, ref editor);
//                    editor.OnInspectorGUI();

//                    if (check.changed)
//                    {
//                        onSettingsUpdated?.Invoke();
//                    }
//                }
//            }
//        }
//    }

//    private void OnEnable()
//    {
//        planetGenerator = (RandPlanetGenarator)target;
//    }
//}

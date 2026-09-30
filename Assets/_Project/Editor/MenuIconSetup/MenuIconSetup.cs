// Assegna il logo alle icone dell'app nelle impostazioni di Unity.
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace BeyondSky.EditorTools
{
    public static class MenuIconSetup
    {
        [MenuItem("Beyond Sky/Apply Menu and App Icon")]
        private static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Beyond Sky", "Esci da Play prima di applicare l'icona.", "OK");
                return;
            }

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/_Project/Art/Sprites/UI/AppIconBeyondSky.png");

            if (icon == null)
            {
                EditorUtility.DisplayDialog("Beyond Sky", "Importa anche AppIconBeyondSky.png dal pacchetto.", "OK");
                return;
            }

            PlatformIconKind[] kinds = PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android);
            if (kinds == null || kinds.Length == 0)
            {
                EditorUtility.DisplayDialog("Beyond Sky",
                    "Installa Android Build Support da Unity Hub e seleziona Android in Build Settings, poi ripeti il comando.", "OK");
                return;
            }

            int assignedSlots = 0;
            for (int i = 0; i < kinds.Length; i++)
            {
                PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kinds[i]);
                for (int j = 0; j < icons.Length; j++)
                {
                    int layerCount = Mathf.Max(1, icons[j].minLayerCount);
                    Texture2D[] textures = new Texture2D[layerCount];
                    for (int layer = 0; layer < textures.Length; layer++)
                    {
                        textures[layer] = icon;
                    }
                    icons[j].SetTextures(textures);
                    assignedSlots++;
                }
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kinds[i], icons);
            }

            if (assignedSlots == 0)
            {
                EditorUtility.DisplayDialog("Beyond Sky", "Nessuno slot Android disponibile. Seleziona Android in Build Settings e riprova.", "OK");
                return;
            }

            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new Texture2D[] { icon }, IconKind.Any);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Beyond Sky",
                "Icone assegnate. Il nuovo sfondo sostituisce quello precedente. Ora esegui Build And Run. Puoi eliminare la cartella Assets/_Project/Editor/MenuIconSetup.", "OK");
        }
    }
}

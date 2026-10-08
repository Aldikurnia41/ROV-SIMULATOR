using System.IO;
using Falah.RovSim.Core;
using UnityEditor;
using UnityEngine;

namespace Falah.RovSim.UI.Editor
{
    /// <summary>Inspector for <see cref="RovProfile"/>: the validator's findings above the fields, and menu commands.</summary>
    [CustomEditor(typeof(RovProfile))]
    public sealed class RovProfileEditor : UnityEditor.Editor
    {
        public const string Folder = "Assets/_Falah/Resources/RovProfiles";

        public override void OnInspectorGUI()
        {
            foreach (var issue in RovProfileValidator.Validate((RovProfile)target))
                EditorGUILayout.HelpBox(issue.Message, issue.Level == RovProfileValidator.Level.Error ? MessageType.Error : MessageType.Warning);
            DrawDefaultInspector();
        }

        [MenuItem("Falah/ROV/Create Default Profiles")]
        public static void CreateDefaults()
        {
            Directory.CreateDirectory(Folder);
            foreach (string id in new[] { "tortuga", "teledyne" })
            {
                string path = Folder + "/" + id + ".asset";
                if (AssetDatabase.LoadAssetAtPath<RovProfile>(path) != null) { Debug.Log("Profile exists, left unchanged: " + path); continue; }
                var p = RovProfile.CreateDefault(id);
                AssetDatabase.CreateAsset(p, path);
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Falah/ROV/Validate Profiles")]
        public static void ValidateAll()
        {
            int total = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:RovProfile"))
            {
                var p = AssetDatabase.LoadAssetAtPath<RovProfile>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var issue in RovProfileValidator.Validate(p))
                {
                    total++;
                    string line = "[" + p.Id + "] " + issue.Message;
                    if (issue.Level == RovProfileValidator.Level.Error) Debug.LogError(line, p); else Debug.LogWarning(line, p);
                }
            }
            Debug.Log("ROV profiles validated: " + total + " finding(s).");
        }
    }
}

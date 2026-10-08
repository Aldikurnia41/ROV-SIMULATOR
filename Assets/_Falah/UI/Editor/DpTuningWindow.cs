using Falah.RovSim.Core;
using UnityEditor;
using UnityEngine;

namespace Falah.RovSim.UI.Editor
{
    /// <summary>
    /// Falah/DP Tuning: edit the PID gains of one ROV in a <see cref="DpTuning"/> asset and preview the step response on
    /// a one-degree-of-freedom plant (mass, drag, constant current) before trying them in Play mode.
    /// </summary>
    public sealed class DpTuningWindow : EditorWindow
    {
        const string DefaultAssetPath = "Assets/_Falah/Rov/DpTuning.asset";

        DpTuning asset;
        int rovIndex;
        int loop;                 // 0 heading, 1 depth, 2 position
        DpPlant plant = DpPlant.SceneRov();
        float step = 5f;
        float seconds = 40f;

        static readonly string[] LoopNames = { "Heading (deg)", "Depth (m)", "Position (m)" };

        [MenuItem("Falah/DP Tuning")]
        public static void Open() => GetWindow<DpTuningWindow>("DP Tuning");

        void OnEnable()
        {
            if (asset == null) asset = AssetDatabase.LoadAssetAtPath<DpTuning>(DefaultAssetPath);
        }

        void OnGUI()
        {
            asset = (DpTuning)EditorGUILayout.ObjectField("Tuning asset", asset, typeof(DpTuning), false);
            if (asset == null || asset.Sets == null || asset.Sets.Length == 0)
            {
                EditorGUILayout.HelpBox("Pick or create a DpTuning asset (Create > Falah > DP Tuning).", MessageType.Info);
                return;
            }

            var names = new string[asset.Sets.Length];
            for (int i = 0; i < names.Length; i++) names[i] = asset.Sets[i].RovId;
            rovIndex = Mathf.Clamp(rovIndex, 0, names.Length - 1);
            rovIndex = EditorGUILayout.Popup("ROV", rovIndex, names);
            loop = GUILayout.Toolbar(loop, LoopNames);

            var set = asset.Sets[rovIndex];
            PidGains gains = loop == 0 ? set.Heading : loop == 1 ? set.Depth : set.Position;

            EditorGUI.BeginChangeCheck();
            gains.Kp = EditorGUILayout.FloatField("Kp", gains.Kp);
            gains.Ki = EditorGUILayout.FloatField("Ki", gains.Ki);
            gains.Kd = EditorGUILayout.FloatField("Kd", gains.Kd);
            gains.IntegralLimit = EditorGUILayout.FloatField("Integral limit", gains.IntegralLimit);
            gains.OutputLimit = EditorGUILayout.FloatField("Output limit", gains.OutputLimit);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(asset, "Edit DP gains");
                if (loop == 0) set.Heading = gains; else if (loop == 1) set.Depth = gains; else set.Position = gains;
                asset.Sets[rovIndex] = set;
                EditorUtility.SetDirty(asset);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview plant", EditorStyles.boldLabel);
            plant.Mass = EditorGUILayout.FloatField("Mass / inertia", plant.Mass);
            plant.LinearDrag = EditorGUILayout.FloatField("Linear drag", plant.LinearDrag);
            plant.QuadraticDrag = EditorGUILayout.FloatField("Quadratic drag", plant.QuadraticDrag);
            plant.Disturbance = EditorGUILayout.FloatField("Current force (N)", plant.Disturbance);
            step = EditorGUILayout.FloatField("Step size", step);
            seconds = Mathf.Max(5f, EditorGUILayout.FloatField("Seconds", seconds));

            var result = DpStepSimulator.Run(gains, plant, step, seconds);
            string settle = result.SettleTime < 0f ? "does not settle" : result.SettleTime.ToString("0.0") + " s";
            EditorGUILayout.LabelField("Overshoot " + (result.Overshoot * 100f).ToString("0.0") + " %   Settle (5%) " + settle +
                                       "   Final error " + result.FinalError.ToString("0.000"));
            DrawPlot(GUILayoutUtility.GetRect(position.width - 20f, 200f), result, step);
        }

        static void DrawPlot(Rect rect, StepResult r, float target)
        {
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.1f, 0.14f));
            float lo = Mathf.Min(0f, target), hi = Mathf.Max(target, 0f);
            foreach (float p in r.Positions) { lo = Mathf.Min(lo, p); hi = Mathf.Max(hi, p); }
            if (hi - lo < 1e-4f) hi = lo + 1f;

            System.Func<int, float, Vector3> point = (i, v) => new Vector3(
                rect.x + rect.width * i / (r.Positions.Length - 1f),
                rect.yMax - rect.height * (v - lo) / (hi - lo), 0f);

            Handles.color = new Color(0.95f, 0.69f, 0.24f, 0.8f); // target
            Handles.DrawLine(point(0, target), point(r.Positions.Length - 1, target));
            Handles.color = new Color(0.22f, 0.71f, 0.94f);       // response
            var pts = new Vector3[r.Positions.Length];
            for (int i = 0; i < pts.Length; i++) pts[i] = point(i, r.Positions[i]);
            Handles.DrawAAPolyLine(2f, pts);
        }
    }
}

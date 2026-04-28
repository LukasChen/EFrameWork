using EFrameWork.Runtime.Effect.Fly;
using UnityEditor;
using UnityEngine;

namespace EFrameWork.Editor.Effect.Fly
{
    [CustomEditor(typeof(FlyAnimationDebugRunner))]
    [CanEditMultipleObjects]
    public sealed class FlyAnimationDebugRunnerEditor : UnityEditor.Editor
    {
        private bool m_showConfigInspector = true;
        private UnityEditor.Editor m_cachedConfigEditor;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var runner = (FlyAnimationDebugRunner)target;
            EditorGUILayout.Space(8);

            if (targets != null && targets.Length > 1)
            {
                EditorGUILayout.HelpBox($"已多选 {targets.Length} 个 FlyAnimationDebugRunner。\nConfig 内嵌编辑仅在单选时可用。", MessageType.Info);
            }
            else
            {
                DrawInlineConfigInspector(runner);
            }

            EditorGUILayout.Space(8);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (targets != null && targets.Length > 1)
                {
                    if (GUILayout.Button("Play Selected (Runtime)"))
                    {
                        PlayAllSelected();
                    }
                }
                else
                {
                    if (GUILayout.Button("Play (Runtime)"))
                    {
                        runner.Play();
                    }
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 PlayMode 后可点击 Play 直接调试飞行效果。", MessageType.Info);
            }
        }

        private void DrawInlineConfigInspector(FlyAnimationDebugRunner runner)
        {
            if (runner == null) return;


            using (new EditorGUILayout.HorizontalScope())
            {
                m_showConfigInspector = EditorGUILayout.Foldout(m_showConfigInspector, "Config Editor", true);

                using (new EditorGUI.DisabledScope(runner.Config == null))
                {
                    if (GUILayout.Button(m_showConfigInspector ? "Hide" : "Show", GUILayout.Width(56)))
                    {
                        m_showConfigInspector = !m_showConfigInspector;
                    }
                }
            }

            if (!m_showConfigInspector) return;

            if (runner.Config == null)
            {
                EditorGUILayout.HelpBox("未指定 Config。请先在 DebugRunner 上设置 FlyAnimationConfig。", MessageType.Info);
                return;
            }

            UnityEditor.Editor.CreateCachedEditor(runner.Config, null, ref m_cachedConfigEditor);
            if (m_cachedConfigEditor != null)
            {
                m_cachedConfigEditor.OnInspectorGUI();
            }

        }

        private void PlayAllSelected()
        {
            if (targets == null) return;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is FlyAnimationDebugRunner r)
                {
                    r.Play();
                }
            }
        }

        private void OnSceneGUI()
        {
            var runner = (FlyAnimationDebugRunner)target;
            if (runner == null || runner.Config == null || runner.Start == null || runner.Target == null) return;

            var cfg = runner.Config;
            var start = runner.Start.position;
            var targetPos = runner.Target.position;

            // Base: start/target markers + optional start radius
            Handles.color = new Color(0.2f, 1f, 0.6f, 1f);
            Handles.SphereHandleCap(0, start, Quaternion.identity, HandleUtility.GetHandleSize(start) * 0.06f, EventType.Repaint);
            Handles.SphereHandleCap(0, targetPos, Quaternion.identity, HandleUtility.GetHandleSize(targetPos) * 0.06f, EventType.Repaint);

            if (cfg.StartRandomRadius > 0.001f)
            {
                var discColor = new Color(0.2f, 1f, 0.6f, 0.25f);
                Handles.color = discColor;
                Handles.DrawWireDisc(start, Vector3.forward, cfg.StartRandomRadius);
            }

            // Linear baseline
            Handles.color = new Color(0.2f, 1f, 0.6f, 0.8f);
            Handles.DrawLine(start, targetPos);

            if (cfg.PathType == FlyPathType.QuadraticBezier)
            {
                Vector3 mid = (start + targetPos) * 0.5f;
                Vector3 control = mid + cfg.QuadraticControlOffset;

                EditorGUI.BeginChangeCheck();
                var newControl = Handles.PositionHandle(control, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(cfg, "Move Quadratic Control");
                    cfg.QuadraticControlOffset = newControl - mid;
                    EditorUtility.SetDirty(cfg);
                }

                // Accurate quadratic preview: convert to equivalent cubic controls
                Vector3 c1 = start + (2f / 3f) * (control - start);
                Vector3 c2 = targetPos + (2f / 3f) * (control - targetPos);

                DrawControlLines(start, targetPos, c1, c2);
                DrawSampledCurve(start, targetPos, c1, c2);
            }
            else if (cfg.PathType == FlyPathType.CubicBezier)
            {
                Vector3 c1 = start + cfg.CubicControl1Offset;
                Vector3 c2 = targetPos + cfg.CubicControl2Offset;

                EditorGUI.BeginChangeCheck();
                var newC1 = Handles.PositionHandle(c1, Quaternion.identity);
                var newC2 = Handles.PositionHandle(c2, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(cfg, "Move Cubic Controls");
                    cfg.CubicControl1Offset = newC1 - start;
                    cfg.CubicControl2Offset = newC2 - targetPos;
                    EditorUtility.SetDirty(cfg);
                }

                DrawControlLines(start, targetPos, c1, c2);
                DrawSampledCurve(start, targetPos, c1, c2);
            }
            else if (cfg.PathType == FlyPathType.ScatterThenFly)
            {
                // 绘制散开范围圆盘
                float scatterRadius = cfg.StartRandomRadius > 0.001f ? cfg.StartRandomRadius : 50f;
                var discColor = new Color(0.6f, 0.8f, 1f, 0.35f);
                Handles.color = discColor;
                Handles.DrawWireDisc(start, Vector3.forward, scatterRadius);
                Handles.DrawSolidDisc(start, Vector3.forward, scatterRadius * 0.1f);

                // 示例散开点（用于可视化飞行路径预览）
                Vector3 scatterPoint = start + new Vector3(scatterRadius * 0.7f, scatterRadius * 0.5f, 0f);
                Handles.color = new Color(0.6f, 0.8f, 1f, 0.9f);
                Handles.SphereHandleCap(0, scatterPoint, Quaternion.identity, HandleUtility.GetHandleSize(scatterPoint) * 0.05f, EventType.Repaint);
                Handles.DrawDottedLine(start, scatterPoint, 3f);

                // 飞行阶段：从散开点到目标点的 CubicBezier 路径
                Vector3 c1 = scatterPoint + cfg.ScatterFlyControl1Offset;
                Vector3 c2 = targetPos + cfg.ScatterFlyControl2Offset;

                EditorGUI.BeginChangeCheck();
                var newC1 = Handles.PositionHandle(c1, Quaternion.identity);
                var newC2 = Handles.PositionHandle(c2, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(cfg, "Move ScatterFly Controls");
                    cfg.ScatterFlyControl1Offset = newC1 - scatterPoint;
                    cfg.ScatterFlyControl2Offset = newC2 - targetPos;
                    EditorUtility.SetDirty(cfg);
                }

                DrawControlLines(scatterPoint, targetPos, c1, c2);
                DrawSampledCurve(scatterPoint, targetPos, c1, c2);

                // 标注阶段
                Handles.Label(start + Vector3.up * HandleUtility.GetHandleSize(start) * 0.3f, "① Spawn", EditorStyles.whiteBoldLabel);
                Handles.Label(scatterPoint + Vector3.up * HandleUtility.GetHandleSize(scatterPoint) * 0.3f, "② Scatter & Hold", EditorStyles.whiteBoldLabel);
                Handles.Label(targetPos + Vector3.up * HandleUtility.GetHandleSize(targetPos) * 0.3f, "③ Target", EditorStyles.whiteBoldLabel);
            }
        }

        private static void DrawControlLines(Vector3 start, Vector3 target, Vector3 c1, Vector3 c2)
        {
            Handles.color = new Color(1f, 1f, 0.2f, 0.65f);
            Handles.DrawLine(start, c1);
            Handles.DrawLine(target, c2);

            Handles.color = new Color(1f, 0.75f, 0.1f, 0.9f);
            Handles.SphereHandleCap(0, c1, Quaternion.identity, HandleUtility.GetHandleSize(c1) * 0.05f, EventType.Repaint);
            Handles.SphereHandleCap(0, c2, Quaternion.identity, HandleUtility.GetHandleSize(c2) * 0.05f, EventType.Repaint);
        }

        private static void DrawSampledCurve(Vector3 p0, Vector3 p3, Vector3 p1, Vector3 p2)
        {
            const int kSegments = 32;
            var points = new Vector3[kSegments + 1];
            for (int i = 0; i <= kSegments; i++)
            {
                float t = i / (float)kSegments;
                points[i] = EvaluateCubic(p0, p1, p2, p3, t);
            }

            Handles.color = new Color(1f, 1f, 0.2f, 1f);
            Handles.DrawAAPolyLine(2f, points);
        }

        private static Vector3 EvaluateCubic(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return (uuu * p0)
                   + (3f * uu * t * p1)
                   + (3f * u * tt * p2)
                   + (ttt * p3);
        }
    }
}

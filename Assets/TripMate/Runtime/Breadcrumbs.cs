using System.Collections.Generic;
using UnityEngine;

namespace TripMate
{
    /// <summary>地上的發光引導點，一波一波往目標方向亮起來。</summary>
    public class Breadcrumbs : MonoBehaviour
    {
        readonly List<Transform> dots = new List<Transform>();

        public static Breadcrumbs Build(Transform parent, Vector3[] points, float spacing = 1.6f)
        {
            var root = TM.Empty("GuideDots", parent, Vector3.zero);
            var b = root.gameObject.AddComponent<Breadcrumbs>();
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 a = points[i], c = points[i + 1];
                float len = Vector3.Distance(a, c);
                int n = Mathf.Max(1, Mathf.RoundToInt(len / spacing));
                for (int k = 0; k < n; k++)
                {
                    var p = Vector3.Lerp(a, c, k / (float)n) + Vector3.up * 0.06f;
                    var d = TM.Prim(PrimitiveType.Sphere, "Dot", root, p, new Vector3(0.32f, 0.05f, 0.32f), TM.Hex("#FFC93C"), 1.6f);
                    b.dots.Add(d.transform);
                }
            }
            return b;
        }

        void Update()
        {
            for (int i = 0; i < dots.Count; i++)
            {
                float k = Mathf.Max(0f, Mathf.Sin(Time.time * 5f - i * 0.55f));
                float s = 0.26f + k * 0.2f;
                dots[i].localScale = new Vector3(s, 0.05f, s);
            }
        }
    }
}

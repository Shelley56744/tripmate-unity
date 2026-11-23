using System.Collections.Generic;
using UnityEngine;

namespace TripMate
{
    /// <summary>一個可以走動的站點場景。座標都是相對 origin 的本地座標。</summary>
    public class Area
    {
        public string key, name, questName;
        public Vector3 origin;
        public Transform root;   // 場景物件的根節點（不在這站時整個關掉，省效能）
        public Vector3 spawn, quest, guide;
        public Vector3[] homes = new Vector3[0];
        public float xMin = -30, xMax = 30, zMin = -30, zMax = 30;
        public List<TownBuilder.MapShape> shapes = new List<TownBuilder.MapShape>();

        public Vector3 W(Vector3 local) { return origin + local; }
        public Vector3 Local(Vector3 world) { return world - origin; }
        public bool Contains(Vector3 world)
        {
            var l = Local(world);
            return l.x >= xMin - 5 && l.x <= xMax + 5 && l.z >= zMin - 5 && l.z <= zMax + 5;
        }
    }

    /// <summary>所有站點、目前所在的站，以及早晨／白天／傍晚／夜晚的光線。</summary>
    public static class World
    {
        public static readonly Dictionary<string, Area> Areas = new Dictionary<string, Area>();
        public static Area Current;
        static Light sun;

        public static void Reset()
        {
            Areas.Clear();
            Current = null;
            sun = null;
        }

        public static Area Get(string key)
        {
            Area a;
            return Areas.TryGetValue(key, out a) ? a : null;
        }

        /// <summary>只顯示目前這站的場景（小鎮的兩站共用同一個根節點）。</summary>
        public static void ShowOnly(Area a)
        {
            var roots = new HashSet<Transform>();
            foreach (var x in Areas.Values) if (x.root) roots.Add(x.root);
            foreach (var r in roots) r.gameObject.SetActive(a != null && r == a.root);
        }

        public static void ApplyLight(string preset)
        {
            if (sun == null)
            {
                var go = new GameObject("Sun");
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.55f;
            }
            Color sunC, sky, eq, ground, fog, bg;
            float intensity; Vector3 rot;
            switch (preset)
            {
                case "morning":
                    sunC = TM.Hex("#FFE6C7"); intensity = 1.0f; rot = new Vector3(25, -60, 0);
                    sky = TM.Hex("#BFD4F2"); eq = TM.Hex("#F2D9C9"); ground = TM.Hex("#8A8090"); fog = TM.Hex("#C9D6EE"); bg = TM.Hex("#A9C2E8");
                    break;
                case "day":
                    sunC = TM.Hex("#FFF6E8"); intensity = 1.15f; rot = new Vector3(50, -30, 0);
                    sky = TM.Hex("#CFE2FF"); eq = TM.Hex("#EDE6F0"); ground = TM.Hex("#8E8AA0"); fog = TM.Hex("#D6E4F5"); bg = TM.Hex("#B2CEF0");
                    break;
                case "night":
                    sunC = TM.Hex("#9AA8FF"); intensity = 0.35f; rot = new Vector3(60, 20, 0);
                    sky = TM.Hex("#3A3F7A"); eq = TM.Hex("#4A4270"); ground = TM.Hex("#2A2840"); fog = TM.Hex("#1E2148"); bg = TM.Hex("#1A1D40");
                    break;
                default: // dusk
                    sunC = TM.Hex("#FFD2A0"); intensity = 0.85f; rot = new Vector3(18, -40, 0);
                    sky = TM.Hex("#9EA3E0"); eq = TM.Hex("#E3B3B8"); ground = TM.Hex("#6B6080"); fog = TM.Hex("#5E4F86"); bg = TM.Hex("#4A4A88");
                    break;
            }
            sun.color = sunC;
            sun.intensity = intensity;
            sun.transform.rotation = Quaternion.Euler(rot);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = eq;
            RenderSettings.ambientGroundColor = ground;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fog;
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 80f;
            var cam = Camera.main;
            if (cam) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = bg; }
        }
    }
}

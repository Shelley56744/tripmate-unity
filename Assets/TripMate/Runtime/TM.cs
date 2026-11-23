using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TripMate
{
    /// <summary>
    /// 共用工具：色票、材質、程式產生的網格（圓角方塊、圓錐、圓環）、3D 文字。
    /// 同時支援 Built-in 與 URP，不需要任何外部模型或貼圖。
    /// </summary>
    public static class TM
    {
        // ---------- 色票（與 2D 原型一致） ----------
        public static readonly Color Body = Hex("#FFB547");
        public static readonly Color Ink = Hex("#1F2350");
        public static readonly Color Page = Hex("#FFE8D1");
        public static readonly Color Blush = Hex("#FF7A6B");
        public static readonly Color Seam = Hex("#F29A2E");
        public static readonly Color Snow = Hex("#F4F1EC");
        public static readonly Color Stone = Hex("#D8CFC2");
        public static readonly Color Wood = Hex("#9A6B47");
        public static readonly Color Pine = Hex("#3F7A5A");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        // ---------- 材質 ----------
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static bool IsURP => GraphicsSettings.currentRenderPipeline != null;

        static Shader LitShader()
        {
            Shader s = null;
            if (IsURP) s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
            return s;
        }

        /// <summary>霧面卡通感的材質。emission &gt; 0 會自發光（霓虹、燈）。</summary>
        public static Material Mat(Color c, float emission = 0f, float smooth = 0.12f)
        {
            string key = ColorUtility.ToHtmlStringRGBA(c) + "_" + emission + "_" + smooth;
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(LitShader()) { name = "TM_" + key };
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * emission);
            }
            cache[key] = m;
            return m;
        }

        // ---------- 物件 ----------
        public static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Color c,
            float emission = 0f, Vector3? euler = null, bool keepCollider = false)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            if (euler.HasValue) g.transform.localEulerAngles = euler.Value;
            g.GetComponent<Renderer>().sharedMaterial = Mat(c, emission);
            if (!keepCollider)
            {
                var col = g.GetComponent<Collider>();
                if (col != null) Kill(col);
            }
            return g;
        }

        /// <summary>在播放中與編輯器中都能安全刪除物件。</summary>
        public static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        public static GameObject MeshObj(string name, Transform parent, Mesh mesh, Vector3 pos, Color c,
            float emission = 0f, Vector3? euler = null, Vector3? scale = null)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            if (euler.HasValue) g.transform.localEulerAngles = euler.Value;
            if (scale.HasValue) g.transform.localScale = scale.Value;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = Mat(c, emission);
            return g;
        }

        public static Transform Empty(string name, Transform parent, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        // ---------- 圓角方塊網格（趣趣的身體） ----------
        static readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();

        public static Mesh RoundedBox(Vector3 size, float radius, int seg = 10)
        {
            string key = "rb" + size + radius + seg;
            if (meshCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var half = size * 0.5f;
            radius = Mathf.Min(radius, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) - 0.001f);
            var inner = half - Vector3.one * radius;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var tris = new List<int>();

            // 六個面各自細分成格子，再把每個點推到圓角方塊表面
            Vector3[] faceN = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var n in faceN)
            {
                Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(n, u);
                int start = verts.Count;
                for (int i = 0; i <= seg; i++)
                {
                    for (int j = 0; j <= seg; j++)
                    {
                        float a = (i / (float)seg) * 2f - 1f;
                        float b = (j / (float)seg) * 2f - 1f;
                        Vector3 p = n + u * a + v * b;              // 單位方塊表面
                        p = Vector3.Scale(p, half);                   // 放大成實際尺寸
                        Vector3 q = new Vector3(
                            Mathf.Clamp(p.x, -inner.x, inner.x),
                            Mathf.Clamp(p.y, -inner.y, inner.y),
                            Mathf.Clamp(p.z, -inner.z, inner.z));
                        Vector3 dir = (p - q);
                        Vector3 nn = dir.sqrMagnitude > 1e-8f ? dir.normalized : n;
                        verts.Add(q + nn * radius);
                        norms.Add(nn);
                    }
                }
                for (int i = 0; i < seg; i++)
                {
                    for (int j = 0; j < seg; j++)
                    {
                        int a0 = start + i * (seg + 1) + j;
                        int a1 = a0 + 1;
                        int b0 = a0 + (seg + 1);
                        int b1 = b0 + 1;
                        tris.Add(a0); tris.Add(a1); tris.Add(b0);
                        tris.Add(a1); tris.Add(b1); tris.Add(b0);
                    }
                }
            }

            var mesh = new Mesh { name = "RoundedBox" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            FixWinding(mesh);
            mesh.RecalculateBounds();
            meshCache[key] = mesh;
            return mesh;
        }

        /// <summary>確保三角形朝外（法線與頂點方向一致），避免面翻轉看不到。</summary>
        static void FixWinding(Mesh mesh)
        {
            var v = mesh.vertices;
            var n = mesh.normals;
            var t = mesh.triangles;
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 face = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                Vector3 avg = n[t[i]] + n[t[i + 1]] + n[t[i + 2]];
                if (Vector3.Dot(face, avg) < 0f)
                {
                    int tmp = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = tmp;
                }
            }
            mesh.triangles = t;
        }

        // ---------- 圓錐（松樹、屋頂、霓虹燈罩） ----------
        public static Mesh Cone(float radius, float height, int seg = 16)
        {
            string key = "cone" + radius + height + seg;
            if (meshCache.TryGetValue(key, out var cached) && cached != null) return cached;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var tip = new Vector3(0, height, 0);
            for (int i = 0; i < seg; i++)
            {
                float a0 = i / (float)seg * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)seg * Mathf.PI * 2f;
                var p0 = new Vector3(Mathf.Cos(a0) * radius, 0, Mathf.Sin(a0) * radius);
                var p1 = new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius);
                int s = verts.Count;
                verts.Add(p0); verts.Add(tip); verts.Add(p1);
                tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
                s = verts.Count;
                verts.Add(p0); verts.Add(p1); verts.Add(Vector3.zero);
                tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
            }
            var mesh = new Mesh { name = "Cone" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            // 圓錐側面：法線要朝外，底面朝下
            var vv = mesh.vertices; var tt = mesh.triangles;
            for (int i = 0; i < tt.Length; i += 3)
            {
                Vector3 face = Vector3.Cross(vv[tt[i + 1]] - vv[tt[i]], vv[tt[i + 2]] - vv[tt[i]]);
                Vector3 center = (vv[tt[i]] + vv[tt[i + 1]] + vv[tt[i + 2]]) / 3f;
                Vector3 outward = new Vector3(center.x, center.y < 0.001f ? -1f : 0f, center.z);
                if (Vector3.Dot(face, outward) < 0f) { int tmp = tt[i + 1]; tt[i + 1] = tt[i + 2]; tt[i + 2] = tmp; }
            }
            mesh.triangles = tt;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshCache[key] = mesh;
            return mesh;
        }

        // ---------- 圓環（頸枕、眼鏡框） ----------
        public static Mesh Torus(float r, float tube, int seg = 24, int side = 10)
        {
            string key = "torus" + r + tube + seg + side;
            if (meshCache.TryGetValue(key, out var cached) && cached != null) return cached;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                var c = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                for (int j = 0; j <= side; j++)
                {
                    float b = j / (float)side * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(b), Mathf.Sin(a) * Mathf.Cos(b));
                    verts.Add(c + n * tube);
                    norms.Add(n);
                }
            }
            for (int i = 0; i < seg; i++)
            {
                for (int j = 0; j < side; j++)
                {
                    int a0 = i * (side + 1) + j, a1 = a0 + 1, b0 = a0 + side + 1, b1 = b0 + 1;
                    tris.Add(a0); tris.Add(b0); tris.Add(a1);
                    tris.Add(a1); tris.Add(b0); tris.Add(b1);
                }
            }
            var mesh = new Mesh { name = "Torus" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            FixWinding(mesh);
            mesh.RecalculateBounds();
            meshCache[key] = mesh;
            return mesh;
        }

        // ---------- 3D 文字（招牌） ----------
        static Font font;

        // ---------- 粒子材質（圓形柔邊，雪花、煙塵、彩帶共用） ----------
        static Material particleMat;
        static Texture2D softDot;

        public static Texture2D SoftDot
        {
            get
            {
                if (softDot != null) return softDot;
                const int n = 32;
                softDot = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "TM_SoftDot", wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01((1f - d) * 3f);
                        softDot.SetPixel(x, y, new Color(1, 1, 1, a));
                    }
                softDot.Apply();
                return softDot;
            }
        }

        public static Material ParticleMat()
        {
            if (particleMat != null) return particleMat;
            string[] names = { "Sprites/Default", "Legacy Shaders/Particles/Alpha Blended", "Particles/Standard Unlit", "Unlit/Transparent" };
            Shader s = null;
            foreach (var n in names) { s = Shader.Find(n); if (s != null) break; }
            particleMat = new Material(s) { name = "TM_Particle", color = Color.white };
            particleMat.mainTexture = SoftDot;
            if (particleMat.HasProperty("_BaseMap")) particleMat.SetTexture("_BaseMap", SoftDot);
            return particleMat;
        }

        /// <summary>想換字型時，在 TripMateBootstrap 的 Cjk Font 欄位指定，會優先使用。</summary>
        public static Font OverrideFont;

        static Font bundledFont;
        static bool bundledTried;

        /// <summary>
        /// 內嵌的中文字型（Noto Sans TC 子集，SIL OFL 1.1，見 Resources/Fonts）。
        /// WebGL 讀不到電腦內建字型，沒有這個中文會變成方塊；桌面版也會因此字型一致。
        /// </summary>
        public static Font BundledFont
        {
            get
            {
                if (!bundledTried)
                {
                    bundledTried = true;
                    try { bundledFont = Resources.Load<Font>("Fonts/NotoSansTC-TripMate"); } catch { }
                }
                return bundledFont;
            }
        }

        /// <summary>介面文字用的中文字型：有指定 OverrideFont 就用它，否則用內嵌字型。</summary>
        public static Font CjkFont { get { return OverrideFont != null ? OverrideFont : BundledFont; } }

        public static Font UIFont
        {
            get
            {
                if (OverrideFont != null) return OverrideFont;
                if (font != null) return font;
                font = BundledFont;
                if (font == null) { try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { } }
                if (font == null) { try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
                return font;
            }
        }

        /// <summary>立體招牌文字。中文、日文會自動使用電腦內建字型顯示。</summary>
        public static TextMesh Text3D(string text, Transform parent, Vector3 pos, float size, Color c, Vector3? euler = null)
        {
            var g = new GameObject("Text_" + text);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            if (euler.HasValue) g.transform.localEulerAngles = euler.Value;
            var tm = g.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = UIFont;
            tm.fontSize = 96;
            tm.characterSize = size / 10f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = c;
            var r = g.GetComponent<MeshRenderer>();
            if (tm.font != null)
            {
                tm.font.RequestCharactersInTexture(text, tm.fontSize);
                r.sharedMaterial = TextMat(tm.font);
            }
            return tm;
        }

        // 3D 文字材質：會被前面的物體擋住（內建字型材質會永遠畫在最上層，背面的字會透出來）
        static readonly Dictionary<Font, Material> textMats = new Dictionary<Font, Material>();
        static bool textHooked;

        static Material TextMat(Font f)
        {
            if (textMats.TryGetValue(f, out var m) && m != null) return m;
            var sh = Shader.Find("TripMate/Text3D");
            if (sh == null) return f.material; // 找不到 Shader 時退回原本的做法
            m = new Material(sh) { name = "TM_Text3D_" + f.name };
            m.mainTexture = f.material.mainTexture;
            textMats[f] = m;
            if (!textHooked)
            {
                textHooked = true;
                // 動態字型加入新字時會換一張貼圖，跟著更新
                Font.textureRebuilt += font =>
                {
                    if (textMats.TryGetValue(font, out var mm) && mm != null) mm.mainTexture = font.material.mainTexture;
                };
            }
            return m;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 所有畫面 UI（用 Unity 內建 OnGUI 畫，不需要任何 UI 套件）。
    /// 這個檔案是共用的部分：樣式、畫圖工具、探索時的 HUD（站點、共識、默契、印章、目標、小地圖、操作提示）、
    /// 對話框、提示、橫幅、頭上的氣泡。
    /// 各個畫面分在：DialogueUI.Menu.cs（主選單、房間、快測）、DialogueUI.Journey.cs（轉場、答題、網路）、
    /// DialogueUI.Wrap.cs（結算、排行程、結局）。
    /// </summary>
    public partial class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance;

        public PlayerMover player;
        public FollowCamera cam;
        public List<Teammate> teammates = new List<Teammate>();

        /// <summary>有畫面或對話在進行，玩家不能走、站點不會觸發。</summary>
        public bool Busy { get; set; }

        // ---- 共用狀態 ----
        string toastText = "";
        float toastBorn = -99f, toastUntil = -99f;

        bool dialogOn, showContinue;
        string speaker = "趣趣", body = "";
        Color speakerColor;

        class Bubble { public Transform t; public string text; public Color color; public float born; public bool small; }
        readonly List<Bubble> bubbles = new List<Bubble>();

        string bannerTitle = "", bannerSub = "";
        Color bannerColor;
        float bannerBorn = -99f, bannerUntil = -99f;

        bool hintsForced;
        float flashUntil = -1f;
        Vector3 navCached;
        float navNext;
        float hintAlpha = 1f;
        float stampPulse = -99f;

        // ---- 外觀 ----
        public static readonly Color Ink = TM.Hex("#1F2350");
        public static readonly Color Cream = TM.Hex("#FFF8EE");
        public static readonly Color Orange = TM.Hex("#FF8A3D");
        public static readonly Color Persimmon = TM.Hex("#FF6B35");
        public static readonly Color Gold = TM.Hex("#FFC93C");
        public static readonly Color Green = TM.Hex("#4DD9A0");
        public static readonly Color Grey = TM.Hex("#9A8F86");
        public static readonly Color Panel = new Color(0.12f, 0.14f, 0.31f, 0.8f);
        public static readonly Color[] Letter = { TM.Hex("#FF6B35"), TM.Hex("#3E8EDE"), TM.Hex("#2F9E62"), TM.Hex("#7C5CE0") };

        Texture2D white, tri;
        GUIStyle sSmall, sKey, sChip, sPill, sToast, sBody, sName, sTitle, sBig, sBanner, sBannerSub, sCardName, sCardText, sField;
        Font lastFont;
        float W;
        const float H = 1080f;

        void Awake()
        {
            Instance = this;
            white = Texture2D.whiteTexture;
            tri = MakeTriangle(64);
        }

        static Texture2D MakeTriangle(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "TM_Triangle", wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                    float half = 0.5f * (1f - v);
                    float edge = Mathf.Min(half - Mathf.Abs(u - 0.5f), v - 0.08f) * n;
                    t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(edge + 0.5f)));
                }
            t.Apply();
            return t;
        }

        static GUIStyle Make(int size, Color c, TextAnchor a, bool bold = false, bool wrap = false)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = size;
            s.alignment = a;
            s.wordWrap = wrap;
            s.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            s.richText = false;
            s.clipping = TextClipping.Overflow;
            s.normal.textColor = c;
            if (TM.CjkFont) s.font = TM.CjkFont;
            return s;
        }

        readonly Dictionary<int, GUIStyle> styleCache = new Dictionary<int, GUIStyle>();

        /// <summary>依基本樣式改字級、顏色、對齊（有快取，避免每幀產生大量物件）。</summary>
        protected GUIStyle St(GUIStyle baseStyle, int size, Color c, TextAnchor a, bool bold, bool wrap = false)
        {
            int key;
            unchecked
            {
                key = baseStyle.GetHashCode();
                key = key * 31 + size;
                key = key * 31 + c.GetHashCode();
                key = key * 31 + (int)a;
                key = key * 31 + (bold ? 1 : 0);
                key = key * 31 + (wrap ? 1 : 0);
            }
            GUIStyle cached;
            if (styleCache.TryGetValue(key, out cached)) return cached;
            var s = new GUIStyle(baseStyle);
            s.fontSize = size;
            s.normal.textColor = c;
            s.alignment = a;
            s.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            s.wordWrap = wrap;
            styleCache[key] = s;
            return s;
        }

        void BuildStyles()
        {
            styleCache.Clear();
            lastFont = TM.CjkFont;
            sSmall = Make(22, Color.white, TextAnchor.MiddleLeft);
            sKey = Make(20, Ink, TextAnchor.MiddleCenter, true);
            sChip = Make(26, Color.white, TextAnchor.MiddleLeft, true);
            sPill = Make(28, Color.white, TextAnchor.MiddleLeft, true);
            sToast = Make(28, Ink, TextAnchor.MiddleCenter, true);
            sBody = Make(31, Ink, TextAnchor.UpperLeft, false, true);
            sName = Make(27, Color.white, TextAnchor.MiddleCenter, true);
            sTitle = Make(44, Ink, TextAnchor.MiddleCenter, true, true);
            sBig = Make(38, Color.white, TextAnchor.MiddleCenter, true);
            sBanner = Make(60, Color.white, TextAnchor.MiddleCenter, true);
            sBannerSub = Make(27, Color.white, TextAnchor.MiddleCenter, true, true);
            sCardName = Make(29, Ink, TextAnchor.UpperLeft, true, true);
            sCardText = Make(22, TM.Hex("#5A5E7A"), TextAnchor.UpperLeft, false, true);
            sField = new GUIStyle(GUI.skin.textField) { fontSize = 30, alignment = TextAnchor.MiddleLeft };
            sField.normal.textColor = Ink;
            sField.focused.textColor = Ink;
            sField.padding = new RectOffset(18, 18, 8, 8);
            sField.normal.background = null;
            sField.focused.background = null;
            sField.hover.background = null;
            sField.active.background = null;
            sField.hover.textColor = Ink;
            sField.active.textColor = Ink;
            if (TM.CjkFont) sField.font = TM.CjkFont;
        }

        // ---------- 畫圖小工具 ----------
        protected void Box(Rect r, Color c, float radius = 16f)
        {
            radius = Mathf.Min(radius, Mathf.Min(r.width, r.height) * 0.5f);
            GUI.DrawTexture(r, white, ScaleMode.StretchToFill, true, 0, c, 0, radius);
        }

        protected void Shadowed(Rect r, Color fill, float radius = 18f)
        {
            Box(new Rect(r.x + 4, r.y + 6, r.width, r.height), new Color(0.12f, 0.14f, 0.31f, 0.5f), radius);
            Box(r, fill, radius);
        }

        protected void Circle(Vector2 c, float size, Color col)
        {
            Box(new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size), col, size * 0.5f);
        }

        protected void Rotated(Texture tex, Rect r, float angle, Color c)
        {
            var m = GUI.matrix;
            Vector3 piv = r.center;
            GUI.matrix = m * Matrix4x4.TRS(piv, Quaternion.Euler(0, 0, angle), Vector3.one) * Matrix4x4.TRS(-piv, Quaternion.identity, Vector3.one);
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true, 0, c, 0, 0);
            GUI.matrix = m;
        }

        protected static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }

        protected static Rect ScaleRect(Rect r, float k)
        {
            return new Rect(r.center.x - r.width * k * 0.5f, r.center.y - r.height * k * 0.5f, r.width * k, r.height * k);
        }

        protected bool Hover(Rect r) { return r.Contains(Event.current.mousePosition); }

        /// <summary>圓角按鈕：回傳這一幀有沒有被點。</summary>
        protected bool Button(Rect r, string label, Color fill, Color text, int size = 28, bool enabled = true)
        {
            bool hv = enabled && Hover(r);
            var rr = hv ? new Rect(r.x, r.y - 3, r.width, r.height) : r;
            Shadowed(rr, enabled ? (hv ? Color.Lerp(fill, Color.white, 0.15f) : fill) : TM.Hex("#D8D2CA"), rr.height * 0.5f);
            GUI.Label(rr, label, St(sName, size, enabled ? text : TM.Hex("#9A948C"), TextAnchor.MiddleCenter, true));
            return enabled && GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>可點的卡片（選項、預算⋯）：回傳這一幀有沒有被點。</summary>
        protected bool Card(Rect r, bool selected, Color accent, bool enabled = true)
        {
            bool hv = enabled && Hover(r);
            var rr = hv && !selected ? new Rect(r.x, r.y - 4, r.width, r.height) : r;
            if (selected) Box(new Rect(rr.x - 4, rr.y - 4, rr.width + 8, rr.height + 8), accent, 24);
            Shadowed(rr, !enabled ? TM.Hex("#EEE8E0") : hv || selected ? TM.Hex("#FFF6E6") : Color.white, 20);
            return enabled && GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        protected bool Typing { get { return GUIUtility.keyboardControl != 0; } }

        static string Composition()
        {
            try { return Input.compositionString ?? ""; } catch (Exception) { return ""; }
        }

        /// <summary>輸入框：白底圓角、聚焦時橘框；提示字只在空白且沒在打字（含注音選字中）時顯示。</summary>
        protected string InputBox(Rect r, string name, string value, int max, string placeholder)
        {
            bool focused = GUI.GetNameOfFocusedControl() == name;
            if (focused) Box(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), Orange, 18);
            Box(r, Color.white, 16);
            GUI.SetNextControlName(name);
            string v = GUI.TextField(r, value, max, sField);
            if (string.IsNullOrEmpty(v) && string.IsNullOrEmpty(Composition()) && !focused)
                GUI.Label(new Rect(r.x + 20, r.y, r.width - 30, r.height), placeholder, St(sSmall, 26, TM.Hex("#B8B0A6"), TextAnchor.MiddleLeft, false));
            return v;
        }

        /// <summary>按鍵（正在打字時不算）。</summary>
        protected bool Key(string k) { return !Typing && InputCompat.Down(k); }

        protected bool Advance() { return Key("space") || InputCompat.Down("click"); }

        // ---------- 每幀 ----------
        void Update()
        {
            if (Key("h")) hintsForced = !hintsForced;
            bool show = !Busy && screen == Page.None && (hintsForced || (player != null && player.movedSeconds < 5f));
            hintAlpha = Mathf.MoveTowards(hintAlpha, show ? 1f : 0f, Time.deltaTime * 2.5f);
            if (Typing) { try { Input.imeCompositionMode = IMECompositionMode.On; } catch (Exception) { } }
        }

        void OnGUI()
        {
            if (sBody == null || lastFont != TM.CjkFont) BuildStyles();
            float s = Screen.height / H;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1));
            W = Screen.width / s;

            if (screen == Page.None && !Busy)
            {
                DrawStationHud();
                DrawObjective();
                DrawMinimap();
                DrawInteractPrompt();
            }
            if (Time.time < flashUntil)
            {
                var oc = GUI.color;
                GUI.color = new Color(1, 1, 1, Mathf.Clamp01((flashUntil - Time.time) / 0.35f));
                Box(new Rect(-10, -10, W + 20, H + 20), Color.white, 0);
                GUI.color = oc;
            }
            DrawHints();
            DrawBubbles(s);

            switch (screen)
            {
                case Page.Title: DrawTitle(); break;
                case Page.Create: DrawCreate(); break;
                case Page.Join: DrawJoin(); break;
                case Page.Lobby: DrawLobby(); break;
                case Page.Quiz: DrawQuiz(); break;
                case Page.QuizReveal: DrawQuizReveal(); break;
                case Page.Transition: DrawTransition(); break;
                case Page.Choices: DrawChoices(); break;
                case Page.Custom: DrawCustom(); break;
                case Page.HintCard: DrawHintCard(); break;
                case Page.Network: DrawNetwork(); break;
                case Page.Lag: DrawLag(); break;
                case Page.Summary: DrawSummary(); break;
                case Page.Plan: DrawPlan(); break;
                case Page.Ending: DrawEnding(); break;
                case Page.EsimShop: DrawEsimShop(); break;
                case Page.Choose: DrawChoose(); break;
                case Page.Shooting: DrawShooting(); break;
                case Page.Soak: DrawSoak(); break;
                case Page.EsimPlan: DrawEsimPlan(); break;
                case Page.Shop: DrawShop(); break;
            }

            DrawBanner();
            if (dialogOn) DrawDialog();
            DrawToast();
        }

        public enum Page { None, Title, Create, Join, Lobby, Quiz, QuizReveal, Transition, Choices, Custom, HintCard, Network, Lag, Summary, Plan, Ending, EsimShop, Choose, Shooting, Soak, EsimPlan, Shop }
        public Page screen = Page.None;

        // ---------- 探索時的 HUD ----------
        void DrawStationHud()
        {
            var dir = LevelDirector.Instance;
            if (dir == null || dir.station < 0 || dir.station >= GameData.Rounds.Length) return;
            var R = GameData.Rounds[dir.station];
            var r = new Rect(24, 24, 430, 84);
            Box(r, Panel, 22);
            GUI.Label(new Rect(r.x + 22, r.y + 8, 300, 30), "第 " + (dir.station + 1) + " 站／" + GameData.Rounds.Length + "　" + R.clock, St(sSmall, 21, Gold, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(r.x + 22, r.y + 38, 400, 40), R.title, St(sChip, 30, Color.white, TextAnchor.MiddleLeft, true));

            float x = 24, y = r.yMax + 10;
            x = Chip(x, y, Green, "共識 " + Session.Consensus);
            int vibe = Scoring.LiveVibe();
            x = Chip(x, y, TM.Hex("#FF8AD8"), "默契 " + (vibe < 0 ? "—" : vibe.ToString()));
            float k = 1f + 0.25f * Mathf.Max(0f, 1f - (Time.time - stampPulse) / 0.35f);
            x = Chip(x, y, Gold, "印章 " + Collectible.Collected + "/" + Collectible.Total, k);

            DrawNetWidget(24, y + 60);
            if (dir.waking)
            {
                int found = dir.mates.Count(m => m.found);
                var wr = new Rect(24, y + 122, 300, 48);
                Box(wr, Panel, 24);
                GUI.Label(new Rect(wr.x + 20, wr.y, 280, wr.height), "叫醒隊友 " + found + " / " + dir.mates.Count, St(sChip, 24, Color.white, TextAnchor.MiddleLeft, true));
            }
        }

        float Chip(float x, float y, Color dot, string text, float pulse = 1f)
        {
            float tw = sChip.CalcSize(new GUIContent(text)).x;
            var r = new Rect(x, y, tw + 64, 50);
            Box(r, Panel, 25);
            Circle(new Vector2(r.x + 25, r.y + 25), 22 * pulse, dot);
            GUI.Label(new Rect(r.x + 44, r.y, tw + 20, r.height), text, St(sChip, 24, Color.white, TextAnchor.MiddleLeft, true));
            return r.xMax + 10;
        }

        /// <summary>網路小工具：訊號格、用的是哪種網路、這段用了多少流量；不是 eSIM 時有「開通去趣 eSIM」按鈕。</summary>
        void DrawNetWidget(float x, float y)
        {
            if (string.IsNullOrEmpty(Session.network)) return;
            bool hotel = World.Current != null && World.Current.key == "hotel";
            string usage;
            if (!Esim.Online) usage = "離線";
            else if (hotel) usage = "飯店 Wi-Fi";
            else if (Esim.Throttled) usage = "降速中";
            else usage = Esim.used.ToString("0.0") + "／" + (Session.network == "esim" ? "2" : "1") + " GB";
            string label = Esim.NetName + "　" + usage;
            float tw = sChip.CalcSize(new GUIContent(label)).x;
            var r = new Rect(x, y, tw + 110, 50);
            Color bg = !Esim.Online || Esim.Throttled ? new Color(0.55f, 0.18f, 0.16f, 0.88f) : Panel;
            Box(r, bg, 25);
            int bars = Esim.Bars;
            for (int i = 0; i < 4; i++)
            {
                float bh = 8 + i * 7;
                Box(new Rect(r.x + 18 + i * 10, r.y + 38 - bh, 7, bh), i < bars ? (Session.network == "esim" ? Green : Gold) : new Color(1, 1, 1, 0.25f), 2);
            }
            GUI.Label(new Rect(r.x + 66, r.y, tw + 30, r.height), label, St(sChip, 22, Color.white, TextAnchor.MiddleLeft, true));
            if (Session.network != "esim")
            {
                if (Button(new Rect(r.xMax + 10, y, 210, 50), "開通去趣 eSIM", Persimmon, Color.white, 22) && !Busy)
                    StartCoroutine(EsimShopCo(null));
            }
        }

        void DrawInteractPrompt()
        {
            var dir = LevelDirector.Instance;
            if (dir == null || player == null) return;
            var it = dir.NearbyInteractable;
            if (it == null) return;
            string txt = it.prompt;
            float tw = sPill.CalcSize(new GUIContent(txt)).x;
            var r = new Rect(W * 0.5f - (tw + 110) * 0.5f, H - 230, tw + 110, 70);
            float k = 1f + Mathf.Sin(Time.time * 4f) * 0.03f;
            r = ScaleRect(r, k);
            Shadowed(r, TM.Hex("#3E5FB8"), 35);
            Box(new Rect(r.x + 14, r.y + 12, 46, 46), Color.white, 12);
            GUI.Label(new Rect(r.x + 14, r.y + 12, 46, 46), "E", St(sKey, 26, Ink, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(r.x + 74, r.y, tw + 20, r.height), txt, sPill);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) dir.TriggerInteract();
        }

        /// <summary>拍照的閃光。</summary>
        public void Flash() { flashUntil = Time.time + 0.35f; }

        void DrawObjective()
        {
            if (!Objective.Target.HasValue || player == null || string.IsNullOrEmpty(Objective.Label)) return;
            if (!Esim.Online)
            {
                string off = "沒有網路，導航用不了，跟著地上的光點走";
                float ow = sPill.CalcSize(new GUIContent(off)).x;
                var orr = new Rect(W * 0.5f - (ow + 60) * 0.5f, 24, ow + 60, 66);
                Box(orr, new Color(0.55f, 0.18f, 0.16f, 0.88f), 33);
                GUI.Label(new Rect(orr.x + 30, orr.y, ow + 20, orr.height), off, sPill);
                return;
            }
            if (Esim.Throttled)
            {
                if (Time.time > navNext) { navCached = Objective.Target.Value; navNext = Time.time + 3f; }
            }
            else { navCached = Objective.Target.Value; navNext = 0; }
            Vector3 v = navCached - player.transform.position;
            v.y = 0;
            float dist = v.magnitude;
            Transform ct = Camera.main ? Camera.main.transform : null;
            Vector3 f = ct ? Vector3.ProjectOnPlane(ct.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 rgt = ct ? Vector3.ProjectOnPlane(ct.right, Vector3.up).normalized : Vector3.right;
            float ang = Mathf.Atan2(Vector3.Dot(v, rgt), Vector3.Dot(v, f)) * Mathf.Rad2Deg;
            string txt = Objective.Label + (dist < 4f ? "　就在這裡" : "　" + Mathf.RoundToInt(dist) + "m") + (Esim.Throttled ? "　（降速中，導航延遲）" : "");
            float tw = sPill.CalcSize(new GUIContent(txt)).x;
            var r = new Rect(W * 0.5f - (tw + 100) * 0.5f, 24, tw + 100, 66);
            Box(r, Panel, 33);
            var c = new Vector2(r.x + 36, r.y + 33);
            Circle(c, 52 * (1f + Mathf.Sin(Time.time * 5f) * 0.06f), Gold);
            Rotated(tri, new Rect(c.x - 15, c.y - 15, 30, 30), ang, Ink);
            GUI.Label(new Rect(r.x + 72, r.y, tw + 20, r.height), txt, sPill);
        }

        void DrawMinimap()
        {
            var area = World.Current;
            if (area == null) return;
            const float size = 260f, pad = 10f;
            var frame = new Rect(W - size - 24, 24, size, size);
            Box(frame, Panel, 22);
            var inner = new Rect(frame.x + pad, frame.y + pad, size - pad * 2, size - pad * 2);
            float ww = area.xMax - area.xMin, wd = area.zMax - area.zMin;
            float sc = Mathf.Min(inner.width / ww, inner.height / wd);
            float ox = inner.x + (inner.width - ww * sc) * 0.5f, oy = inner.y + (inner.height - wd * sc) * 0.5f;
            var mapR = new Rect(ox, oy, ww * sc, wd * sc);
            Func<Vector3, Vector2> P = world =>
            {
                var l = area.Local(world);
                return new Vector2(ox + (l.x - area.xMin) * sc, oy + (area.zMax - l.z) * sc);
            };
            foreach (var sh in area.shapes)
            {
                var r = Rect.MinMaxRect(ox + (sh.x0 - area.xMin) * sc, oy + (area.zMax - sh.z1) * sc, ox + (sh.x1 - area.xMin) * sc, oy + (area.zMax - sh.z0) * sc);
                var cl = Rect.MinMaxRect(Mathf.Max(r.xMin, mapR.xMin), Mathf.Max(r.yMin, mapR.yMin), Mathf.Min(r.xMax, mapR.xMax), Mathf.Min(r.yMax, mapR.yMax));
                if (cl.width <= 0 || cl.height <= 0) continue;
                Box(cl, sh.color, sh.radius * sc);
            }
            foreach (var c in Collectible.All)
                if (c != null && !c.Taken && string.IsNullOrEmpty(c.hiddenPoi) && area.Contains(c.transform.position)) { var p = P(c.transform.position); Circle(p, 9, TM.Hex("#B8860B")); Circle(p, 6, Gold); }
            if (Objective.Target.HasValue && area.Contains(Objective.Target.Value))
            {
                var p = P(Objective.Target.Value);
                float k = 1f + Mathf.Sin(Time.time * 6f) * 0.25f;
                Circle(p, 22 * k, new Color(1f, 0.79f, 0.24f, 0.45f));
                Circle(p, 12, Gold);
                Circle(p, 5, Ink);
            }
            foreach (var t in teammates)
            {
                if (t == null || !area.Contains(t.transform.position)) continue;
                var p = P(t.transform.position);
                var pl = Session.players.FirstOrDefault(x => x.mate == t);
                Circle(p, 12, Color.white);
                Circle(p, 9, pl != null ? pl.color : Grey);
            }
            if (player)
            {
                var p = P(player.transform.position);
                Circle(p, 20, Color.white);
                Rotated(tri, new Rect(p.x - 9, p.y - 10, 18, 18), player.transform.eulerAngles.y, Persimmon);
            }
            if (!Esim.Online)
            {
                Box(frame, new Color(0.1f, 0.1f, 0.15f, 0.72f), 22);
                GUI.Label(frame, "離線\n地圖無法更新", St(sTitle, 28, Color.white, TextAnchor.MiddleCenter, true));
            }
            GUI.Label(new Rect(frame.x, frame.yMax + 2, frame.width, 30), area.name, St(sSmall, 20, Color.white, TextAnchor.MiddleCenter, true));
        }

        void DrawHints()
        {
            if (Busy || screen != Page.None) return;
            string[,] items = { { "WASD", "移動" }, { "Shift", "跑" }, { "空白", "跳" }, { "E", "互動" }, { "滑鼠拖曳", "轉鏡頭" } };
            var old = GUI.color;
            if (hintAlpha > 0.02f)
            {
                GUI.color = new Color(1, 1, 1, hintAlpha);
                float x = 24, y = H - 24 - 50;
                for (int i = 0; i < items.GetLength(0); i++)
                {
                    float kw = Mathf.Max(48, sKey.CalcSize(new GUIContent(items[i, 0])).x + 20);
                    float lw = sSmall.CalcSize(new GUIContent(items[i, 1])).x;
                    var r = new Rect(x, y, kw + lw + 26, 50);
                    Box(r, Panel, 25);
                    Box(new Rect(x + 7, y + 7, kw, 36), Color.white, 12);
                    GUI.Label(new Rect(x + 7, y + 7, kw, 36), items[i, 0], sKey);
                    GUI.Label(new Rect(x + kw + 15, y, lw + 10, 50), items[i, 1], sSmall);
                    x += r.width + 10;
                }
            }
            else
            {
                GUI.color = new Color(1, 1, 1, 0.55f);
                var r = new Rect(24, H - 24 - 44, 132, 44);
                Box(r, Panel, 22);
                Box(new Rect(r.x + 6, r.y + 6, 32, 32), Color.white, 10);
                GUI.Label(new Rect(r.x + 6, r.y + 6, 32, 32), "H", sKey);
                GUI.Label(new Rect(r.x + 46, r.y, 90, r.height), "操作", sSmall);
            }
            GUI.color = old;
        }

        void DrawToast()
        {
            if (Time.time > toastUntil) return;
            float k = EaseOutBack((Time.time - toastBorn) / 0.25f);
            float a = Mathf.Clamp01((toastUntil - Time.time) / 0.3f);
            float tw = sToast.CalcSize(new GUIContent(toastText)).x + 70;
            var r = ScaleRect(new Rect(W * 0.5f - tw * 0.5f, 108, tw, 64), k);
            var old = GUI.color;
            GUI.color = new Color(1, 1, 1, a);
            Shadowed(r, Gold, 32);
            GUI.Label(r, toastText, sToast);
            GUI.color = old;
        }

        void DrawBubbles(float s)
        {
            var c = Camera.main;
            if (c == null) return;
            foreach (var b in bubbles)
            {
                if (b.t == null) continue;
                Vector3 sp = c.WorldToScreenPoint(b.t.position + Vector3.up * (Ququ.BodyTopY + 1.3f));
                if (sp.z < 0) continue;
                var p = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
                float k = EaseOutBack((Time.time - b.born) / 0.3f);
                if (b.small)
                {
                    var bs = St(sChip, 24, Color.white, TextAnchor.MiddleCenter, true, true);
                    var gc = new GUIContent(b.text);
                    float tw = Mathf.Clamp(bs.CalcSize(gc).x + 36, 70, 400);
                    float th = Mathf.Max(52, bs.CalcHeight(gc, tw - 36) + 20);
                    var r = ScaleRect(new Rect(p.x - tw * 0.5f, p.y - th + 26, tw, th), k);
                    Shadowed(r, b.color, 24);
                    Rotated(tri, new Rect(r.center.x - 10, r.yMax - 4, 20, 14), 180f, b.color);
                    GUI.Label(new Rect(r.x + 18, r.y + 8, r.width - 36, r.height - 16), b.text, bs);
                    continue;
                }
                Circle(p + new Vector2(3, 5), 84 * k, new Color(0.12f, 0.14f, 0.31f, 0.5f));
                Circle(p, 84 * k, Color.white);
                Circle(p, 70 * k, b.color);
                if (k > 0.5f) GUI.Label(new Rect(p.x - 40, p.y - 40, 80, 80), b.text, sBig);
            }
        }

        void DrawBanner()
        {
            if (Time.time > bannerUntil) return;
            float k = EaseOutBack((Time.time - bannerBorn) / 0.35f);
            var r = ScaleRect(new Rect(W * 0.5f - 360, 230, 720, 170), k);
            Shadowed(r, bannerColor, 32);
            if (k > 0.6f)
            {
                GUI.Label(new Rect(r.x, r.y + 16, r.width, 80), bannerTitle, sBanner);
                GUI.Label(new Rect(r.x + 20, r.y + 100, r.width - 40, 56), bannerSub, sBannerSub);
            }
        }

        void DrawDialog()
        {
            float dw = Mathf.Min(1180, W - 80);
            var d = new Rect((W - dw) * 0.5f, H - 36 - 170, dw, 170);
            Shadowed(d, Cream, 28);
            bool narr = speaker == "旁白";
            if (!narr)
            {
                float nw = sName.CalcSize(new GUIContent(speaker)).x + 50;
                var tag = new Rect(d.x + 36, d.y - 30, nw, 56);
                Box(tag, speakerColor, 28);
                GUI.Label(tag, speaker, sName);
            }
            var bs = narr ? new GUIStyle(St(sBody, 30, TM.Hex("#6A6E8C"), TextAnchor.UpperLeft, false, true)) : sBody;
            if (narr) bs.fontStyle = FontStyle.Italic;
            GUI.Label(new Rect(d.x + 44, d.y + 34, d.width - 110, d.height - 50), body, bs);
            if (showContinue)
            {
                float bob = Mathf.Abs(Mathf.Sin(Time.time * 5f)) * 6f;
                Rotated(tri, new Rect(d.xMax - 56, d.yMax - 44 + bob, 26, 22), 180f, Orange);
            }
        }

        // ---------- 對外 ----------
        public void Toast(string msg, float seconds = 2.2f)
        {
            toastText = msg;
            toastBorn = Time.time;
            toastUntil = Time.time + seconds;
        }

        public void OnStamp(string hiddenPoi = null)
        {
            stampPulse = Time.time;
            if (!string.IsNullOrEmpty(hiddenPoi))
            {
                var poi = Planner.Get(hiddenPoi);
                Banner("發現隱藏景點！", (poi != null ? poi.name : "") + "　·　印章 +3，排行程時可以放進去", TM.Hex("#8C5BE0"), 3f);
            }
            else if (Collectible.Collected == Collectible.Total) Toast("這一趟的印章全部收集完成！", 3f);
            else Toast("印章 +1（存起來可以換配件）", 1.2f);
        }

        public void Banner(string title, string sub, Color c, float seconds)
        {
            bannerTitle = title;
            bannerSub = sub;
            bannerColor = c;
            bannerBorn = Time.time;
            bannerUntil = Time.time + seconds;
        }

        protected void AddBubble(Transform t, string text, Color c, bool small = false)
        {
            bubbles.RemoveAll(b => b.t == t);
            bubbles.Add(new Bubble { t = t, text = text, color = c, born = Time.time, small = small });
        }

        protected void ClearBubbles() { bubbles.Clear(); }

        Color SpeakerColor(string who)
        {
            if (who == "趣趣") return Orange;
            var p = Session.players.FirstOrDefault(x => x.name == who);
            if (p != null) return p.isUser ? Persimmon : GameData.P(p.Persona).color;
            return Ink;
        }

        /// <summary>一句對話（鎖住玩家，說完解鎖）。</summary>
        public IEnumerator Line(string who, string text)
        {
            bool was = Busy;
            Busy = true;
            if (player) player.locked = true;
            yield return Say(who, text);
            dialogOn = false;
            if (!was)
            {
                if (player) player.locked = false;
                Busy = false;
            }
        }

        /// <summary>打字機效果＋說話嗶嗶聲；按空白鍵或點一下顯示全文，再按一次繼續。</summary>
        protected IEnumerator Say(string who, string text, bool waitForContinue = true)
        {
            dialogOn = true;
            speaker = who;
            speakerColor = SpeakerColor(who);
            showContinue = false;
            body = "";
            float pitch = who == "趣趣" ? 1.25f : who == "旁白" ? 0f : 1f;
            for (int i = 1; i <= text.Length; i++)
            {
                body = text.Substring(0, i);
                char ch = text[i - 1];
                if (pitch > 0 && i % 2 == 0 && !char.IsWhiteSpace(ch) && !char.IsPunctuation(ch)) Sfx.Blip(pitch);
                if (Advance()) { body = text; break; }
                yield return new WaitForSeconds(0.028f);
            }
            yield return null;
            if (!waitForContinue) yield break;
            showContinue = true;
            while (!Advance()) yield return null;
            showContinue = false;
            yield return null;
        }

        protected void HideDialog() { dialogOn = false; }
    }
}

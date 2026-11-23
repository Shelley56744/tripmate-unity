using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 整趟遊戲的流程：
    /// 主選單（建立／加入房間）→ 人格快測 → 10 站旅程（每站：轉場 → 自由逛 → 走到站點 → 答題揭曉）
    /// → 結算 → 微調行程 → 結局。第 1 站要先把散在廣場的隊友叫醒，第 1 站後選網路。
    /// </summary>
    public class LevelDirector : MonoBehaviour
    {
        public static LevelDirector Instance;

        public PlayerMover player;
        public Ququ playerQ;
        public FollowCamera cam;
        public DialogueUI ui;
        public QuestZone zone;
        public Ququ guide;
        public readonly List<Teammate> mates = new List<Teammate>();

        /// <summary>目前第幾站（0～9），-1 表示還沒出發。</summary>
        public int station = -1;
        public bool waking;
        bool exploring;

        /// <summary>玩家附近可以按 E 的地方（沒有就是 null）。</summary>
        public Interactable NearbyInteractable { get; private set; }
        /// <summary>這站要先做的事（例如溫泉站要先泡湯），做完站點才會亮。</summary>
        Interactable featured;
        string featuredLabel;
        readonly Dictionary<string, Interactable> keyed = new Dictionary<string, Interactable>();

        void Awake() { Instance = this; }

        void Start()
        {
            RegisterInteractables();
            StartCoroutine(Run());
        }

        // ---------------- 場景互動點 ----------------
        Interactable Add(string area, Vector3 local, string kind, string prompt, float radius = 2.4f, bool once = true)
        {
            var a = World.Get(area);
            if (a == null || a.root == null) return null;
            var it = Interactable.Create(a.root, local, kind, prompt, radius, once);
            keyed[area + ":" + kind] = it;
            return it;
        }

        void RegisterInteractables()
        {
            // 飯店前廣場：去趣 eSIM 櫃台（攤位在 TownBuilder 蓋好了）、公告欄
            Add("hotel", new Vector3(3.3f, 0, -4.9f), "esim", "去趣 eSIM 櫃台", 2.4f, false);
            Add("hotel", new Vector3(-6.0f, 0, 3.4f), "board", "看公告欄");
            // 道頓堀：運河夜景合照
            var dot = World.Get("dotonbori");
            if (dot != null) Props.PhotoSpot(dot.root, new Vector3(3.6f, 0, 21.5f), 0);
            Add("dotonbori", new Vector3(3.6f, 0, 21.5f), "photo_dotonbori", "拍照打卡", 2f);
            // 樂園：整理券機
            var usj = World.Get("usj");
            if (usj != null) Props.Kiosk(usj.root, new Vector3(6f, 0, -6f), -90, "整理券");
            Add("usj", new Vector3(4.6f, 0, -6f), "ticket", "用 App 搶整理券", 2.2f);
            // 心齋橋：TAX FREE 店
            Add("shinsaibashi", new Vector3(-4.8f, 0, -16f), "taxfree", "進藥妝店逛逛", 2.4f);
            // 大阪城：熱甘酒攤、拍照點
            Add("castle", new Vector3(6.4f, 0, -10f), "amazake", "喝杯熱甘酒", 2.2f);
            var castle = World.Get("castle");
            if (castle != null) Props.PhotoSpot(castle.root, new Vector3(-4f, 0, -8f), 0);
            Add("castle", new Vector3(-4f, 0, -8f), "photo_castle", "拍照打卡", 2f);
            // 黑門市場：試吃
            Add("kuromon", new Vector3(-4f, 0, -12f), "snack", "買一串邊走邊吃", 2.2f);
            // 新世界：射的
            Add("shinsekai", new Vector3(-5.5f, 0, -6f), "shooting", "玩射的", 2.4f);
            var ss = World.Get("shinsekai");
            if (ss != null) Props.PhotoSpot(ss.root, new Vector3(4f, 0, -12f), 0);
            Add("shinsekai", new Vector3(4f, 0, -12f), "photo_tower", "拍照打卡", 2f);
            // 梅田：地下街、拍照點
            Add("umeda", new Vector3(13f, 0, -4.8f), "underground", "走地下街", 2.4f);
            var um = World.Get("umeda");
            if (um != null) Props.PhotoSpot(um.root, new Vector3(5f, 0, -14f), 0);
            Add("umeda", new Vector3(5f, 0, -14f), "photo_umeda", "拍照打卡", 2f);
            // 溫泉：露天池（池子在 AreaBuilder 蓋好了）
            Add("onsen", new Vector3(-13f, 0, 11f), "soak", "泡溫泉", 5.2f);
        }

        public void TriggerInteract()
        {
            var it = NearbyInteractable;
            if (it == null || ui.Busy || ui.screen != DialogueUI.Page.None) return;
            NearbyInteractable = null;
            StartCoroutine(ui.Activity(it));
        }

        IEnumerator Run()
        {
            // 主選單時鏡頭繞著飯店前的趣趣轉
            var hotel = World.Get("hotel");
            World.Current = hotel;
            World.ShowOnly(hotel);
            World.ApplyLight("morning");
            player.locked = true;
            if (cam) { cam.SnapBehind(); cam.showcase = true; }
            guide.gameObject.SetActive(false);

            yield return ui.MenuCo();
            yield return ui.QuizCo();
            SpawnMates();
            guide.gameObject.SetActive(true);

            for (int r = 0; r < GameData.Rounds.Length; r++)
            {
                station = r;
                var R = GameData.Rounds[r];
                var area = World.Get(R.area);
                int rr = r;
                yield return ui.TransitionCo(r, () => EnterArea(rr, area, R));

                if (r == 0)
                {
                    yield return ui.Line("趣趣", "早安！大家還散在廣場各處做自己的事⋯⋯先去把 " + mates.Count + " 位隊友叫來集合吧！頭上有「！」的就是。");
                    waking = true;
                    while (mates.Any(m => !m.found)) yield return null;
                    waking = false;
                    zone.Activate(true);
                    ui.Toast("隊友到齊！到「" + area.questName + "」集合", 3f);
                }
                else if (R.area == "onsen" && keyed.TryGetValue("onsen:soak", out featured) && featured != null && !featured.used)
                {
                    // 溫泉站：先去泡湯，泡完（或決定不泡）站點才會亮
                    featuredLabel = "去露天溫泉泡湯";
                    zone.Activate(false);
                    ui.Toast("先去露天溫泉，走近按 E 泡個湯吧", 3f);
                }
                else
                {
                    featured = null;
                    zone.Activate(true);
                    ui.Toast("自由逛逛，按 E 可以互動；準備好就到「" + area.questName + "」", 3f);
                }

                exploring = true;
                while (!zone.Triggered) yield return null;
                exploring = false;
                Objective.Clear();
                yield return ui.RoundCo(r, guide);
                if (r == 0) yield return ui.NetworkCo();
            }

            station = GameData.Rounds.Length;
            Objective.Clear();
            var team = Scoring.Analyze();
            yield return ui.SummaryCo(team);
            yield return ui.PlanCo(team);
            yield return ui.EndingCo(team, x => { });
            Restart();
        }

        void SpawnMates()
        {
            int i = 0;
            foreach (var p in Session.Mates)
            {
                var q = Ququ.Create("Mate_" + p.name, transform, TownBuilder.SpawnPoint + new Vector3((i - 1) * 1.5f, 0, -2f), p.type);
                var m = q.gameObject.AddComponent<Teammate>();
                m.player = player.transform;
                m.index = i;
                m.displayName = p.name;
                m.ApplyPersonality(p.type);
                var tag = TM.Text3D(p.name, q.transform, new Vector3(0, Ququ.BodyTopY + 0.7f, 0), 0.22f, Color.white);
                tag.gameObject.AddComponent<FaceCamera>();
                p.mate = m;
                mates.Add(m);
                i++;
            }
            ui.teammates.Clear();
            ui.teammates.AddRange(mates);
        }

        /// <summary>傳送到新站點：光線、玩家位置、鏡頭、站點、嚮導趣趣、隊友。</summary>
        void EnterArea(int r, Area area, RoundDef R)
        {
            World.Current = area;
            World.ShowOnly(area);
            World.ApplyLight(R.light);
            if (r % 2 == 0) Esim.NewStation(); // 每兩站算一天，流量重新計算

            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            Vector3 spawn = area.W(area.spawn);
            Vector3 toQuest = area.W(area.quest) - spawn; toQuest.y = 0;
            player.transform.position = spawn;
            player.transform.rotation = Quaternion.LookRotation(toQuest.normalized);
            cc.enabled = true;
            if (cam) cam.SnapBehind();

            zone.Activate(false);
            zone.Place(area.W(area.quest), spawn);

            // 嚮導趣趣站在站點後面，面向出生點
            guide.transform.position = area.W(area.guide);
            Vector3 g = spawn - guide.transform.position; g.y = 0;
            guide.transform.rotation = Quaternion.LookRotation(g.normalized);

            var used = new HashSet<string>();
            for (int i = 0; i < mates.Count; i++)
            {
                var m = mates[i];
                var p = Session.players.First(x => x.mate == m);
                if (r == 0)
                {
                    // 晨間集合：每個人一早在廣場的不同角落做自己的事，要去叫醒
                    Vector3 home = TownBuilder.HotelHome(p.type);
                    while (used.Contains(home.ToString())) home += new Vector3(1.6f, 0, 1.2f);
                    used.Add(home.ToString());
                    m.Teleport(area.W(home));
                    m.found = false;
                    m.ShowMarker(true);
                }
                else
                {
                    // 一起抵達，然後各自散開去做自己的事
                    m.Teleport(spawn + new Vector3((i - 1) * 1.4f, 0, -1.8f - (i % 2) * 0.8f));
                    m.found = true;
                    m.ShowMarker(false);
                    var home = area.homes.Length > 0 ? area.homes[(i + r) % area.homes.Length] : area.quest;
                    m.TravelTo(area.W(home));
                }
            }
        }

        void Update()
        {
            bool free = (exploring || waking) && !ui.Busy && ui.screen == DialogueUI.Page.None;
            NearbyInteractable = free ? Interactable.Nearest(player.transform.position) : null;
            if (NearbyInteractable != null && InputCompat.Down("e") && GUIUtility.keyboardControl == 0) TriggerInteract();

            // 邊走邊開導航會吃流量（飯店有 Wi-Fi 不算）
            if (free && World.Current != null && World.Current.key != "hotel" && !string.IsNullOrEmpty(Session.network))
            {
                if (player.Velocity.sqrMagnitude > 0.5f) Esim.Drain(0.008f * Time.deltaTime);
                if (!Esim.Online) Esim.offlineSec += Time.deltaTime;
                else if (Esim.Throttled) Esim.slowSec += Time.deltaTime;
            }

            if (exploring && featured != null)
            {
                if (featured.used)
                {
                    featured = null;
                    zone.Activate(true);
                    ui.Toast("到「" + World.Current.questName + "」集合", 2.4f);
                }
                else
                {
                    Objective.Label = featuredLabel;
                    Objective.Target = featured.transform.position;
                    return;
                }
            }

            if (waking)
            {
                Teammate nearest = null;
                float bd = float.MaxValue;
                foreach (var m in mates)
                {
                    if (m.found) continue;
                    float d = (m.transform.position - player.transform.position).sqrMagnitude;
                    if (d < bd) { bd = d; nearest = m; }
                }
                if (nearest != null)
                {
                    Objective.Label = "叫醒 " + nearest.displayName;
                    Objective.Target = nearest.transform.position;
                    if (!ui.Busy && bd < 2.6f * 2.6f) StartCoroutine(Wake(nearest));
                }
            }
            else if (exploring && World.Current != null)
            {
                Objective.Label = "前往" + World.Current.questName;
                Objective.Target = zone.transform.position;
            }
        }

        static string WakeLine(Persona p)
        {
            switch (p)
            {
                case Persona.Soldier: return "終於來了！我五點半就在這等了，快點快點。";
                case Persona.Capybara: return "嗯⋯⋯再五分鐘⋯⋯好啦好啦，我起來了。";
                case Persona.Camera: return "等我拍完這張晨光就過去！";
                case Persona.Shopper: return "紀念品店八點才開⋯⋯好吧，先集合。";
                case Persona.Accountant: return "我在查今天的車票怎麼買最划算，馬上來。";
                case Persona.Explorer: return "我剛發現一條很有味道的小巷，等等帶你們去！";
                case Persona.Taxi: return "叫的車還沒到⋯⋯那我先跟你走過去。";
                case Persona.Nanny: return "大家都有穿暖嗎？我把暖暖包分一分就來！";
            }
            return "早安！";
        }

        IEnumerator Wake(Teammate m)
        {
            m.found = true;
            m.ShowMarker(false);
            var p = Session.players.First(x => x.mate == m);
            StartCoroutine(m.Hop(1));
            Sfx.Chime();
            yield return ui.Line(p.name, WakeLine(p.type));
            int idx = mates.Count(x => x.found) - 1;
            var area = World.Current;
            Vector3 meet = area.W(area.quest) + new Vector3(-3f + idx * 1.6f, 0, -2.2f);
            m.TravelTo(meet);
            if (mates.Any(x => !x.found)) ui.Toast("隊友 " + (idx + 1) + " / " + mates.Count, 1.8f);
        }

        public static void Restart()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#else
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
#endif
        }
    }
}

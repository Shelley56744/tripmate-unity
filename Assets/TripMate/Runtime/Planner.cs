using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TripMate
{
    public class Poi
    {
        public string id, name, area, price, blurb;
        public string[] tags;
        public float lat, lng, rating;
        public int minutes, cost, reviews;
        public bool AllDay { get { return minutes >= 360; } }
    }

    public class PlanItem { public int day; public string slot; public string poiId; public string by; } // by: guide / user / 隊友名字

    public class PlanIssue { public string text; public string slotKey; public string fixPoi; }

    /// <summary>
    /// 排行程：景點庫、時段、交通估算、依 10 站選擇預排，以及趣趣的替換建議。
    /// 規則沿用網頁版（SLOT_MINUTES、travelMinutes、seedFullPlan、scorePoi、budgetFit）。
    /// </summary>
    public static class Planner
    {
        public const string AllDaySlot = "全日";
        public static readonly string[] Slots = { "上午", "下午", "晚上" };
        public static readonly Dictionary<string, int> SlotMinutes = new Dictionary<string, int> { { "上午", 210 }, { "下午", 240 }, { "晚上", 210 }, { AllDaySlot, 660 } };
        static readonly Dictionary<string, string[]> SlotTags = new Dictionary<string, string[]>
        {
            { "上午", new[] { "文化", "探索", "親子" } },
            { "下午", new[] { "購物", "拍照", "放空", "溫泉", "親子" } },
            { "晚上", new[] { "夜生活", "美食", "購物" } },
        };

        static Poi P(string id, string name, string area, float lat, float lng, string tags, int minutes, string price, int cost, float rating, int reviews, string blurb)
        {
            return new Poi { id = id, name = name, area = area, lat = lat, lng = lng, tags = tags.Split(','), minutes = minutes, price = price, cost = cost, rating = rating, reviews = reviews, blurb = blurb };
        }

        public static readonly Poi[] Pois =
        {
            P("usj", "日本環球影城 USJ", "此花區", 34.6654f, 135.4323f, "拍照,親子,動漫", 420, "NT$2,600", 2600, 4.6f, 12840, "冬季限定雪景與城堡燈光，設施跟出片率都滿檔。"),
            P("kuromon", "黑門市場", "日本橋", 34.6656f, 135.5062f, "美食,探索", 90, "NT$600", 600, 4.2f, 5310, "現烤海膽、鮪魚生魚片，站著吃就是最道地的吃法。"),
            P("dotonbori", "道頓堀運河夜景", "難波", 34.6687f, 135.5013f, "拍照,夜生活,美食", 120, "免費", 0, 4.5f, 21470, "霓虹倒影加上運河散步，晚上 8 點人最多也最好拍。"),
            P("shinsaibashi", "心齋橋筋商店街", "心齋橋", 34.6723f, 135.5010f, "購物", 150, "看你自己", 1500, 4.3f, 9860, "藥妝、電器、服飾一條龍，退稅櫃檯 20:00 前記得排。"),
            P("umeda-sky", "梅田藍天大廈 空中庭園", "梅田", 34.7052f, 135.4901f, "拍照,夜生活", 90, "NT$350", 350, 4.6f, 7420, "360 度夜景，日落前 40 分鐘上去最美，冬天風很大。"),
            P("osaka-castle", "大阪城公園", "中央區", 34.6873f, 135.5262f, "文化,拍照,放空", 150, "NT$130", 130, 4.5f, 15230, "護城河與天守閣，冬天早上光線斜射時最好拍。"),
            P("shinsekai", "通天閣・新世界串炸", "新世界", 34.6525f, 135.5063f, "美食,文化", 120, "NT$500", 500, 4.1f, 6180, "昭和味十足的街區，串炸醬汁只能沾一次是鐵則。"),
            P("shitennoji", "四天王寺", "天王寺", 34.6542f, 135.5166f, "文化,放空", 75, "NT$70", 70, 4.3f, 2940, "日本最古老的官寺之一，冬天早上安靜到只聽得到鴿子。"),
            P("nakanoshima", "中之島公園・中央公會堂", "北區", 34.6937f, 135.5019f, "文化,放空,拍照", 90, "免費", 0, 4.4f, 1870, "紅磚建築配河岸散步，咖啡店坐一下午也不會被催。"),
            P("amemura", "美國村・堀江古著街", "西心齋橋", 34.6720f, 135.4976f, "購物,拍照,探索", 150, "NT$800", 800, 4.2f, 3360, "古著、選物店與塗鴉牆，三角公園旁的霓虹超好拍。"),
            P("solaniwa", "空庭溫泉", "港區", 34.6685f, 135.4602f, "溫泉,放空", 180, "NT$600", 600, 4.5f, 4120, "安土桃山風的大型溫泉設施，走到腳廢那天最該來。"),
            P("kaiyukan", "海遊館", "港區", 34.6547f, 135.4289f, "親子,拍照", 150, "NT$570", 570, 4.4f, 11350, "鯨鯊的巨型水槽，傍晚人潮較少，出口就是摩天輪。"),
            P("tenjinbashi", "天神橋筋商店街", "北區", 34.7060f, 135.5120f, "美食,購物,探索", 120, "NT$400", 400, 4.3f, 4580, "日本最長商店街，庶民價的章魚燒與喫茶店都在這。"),
            P("namba-yasaka", "難波八阪神社 獅子殿", "難波", 34.6606f, 135.4974f, "拍照,文化", 40, "免費", 0, 4.4f, 3210, "巨大獅子頭舞台，5 分鐘就能拍到全隊最有梗的合照。"),
            P("denden", "日本橋電電町", "日本橋", 34.6614f, 135.5063f, "動漫,購物", 120, "NT$800", 800, 4.2f, 2760, "扭蛋、模型與電玩店密集，宅度比秋葉原更好逛。"),
            P("uranamba", "裏難波居酒屋巡禮", "難波", 34.6640f, 135.5030f, "夜生活,美食,探索", 150, "NT$900", 900, 4.5f, 1930, "在地人的小巷居酒屋區，一間喝一杯再換下一間。"),
            P("sumiyoshi", "住吉大社", "住吉區", 34.6122f, 135.4930f, "文化,放空", 90, "免費", 0, 4.5f, 5640, "朱紅太鼓橋是招牌畫面，路面電車一日券可以順遊。"),
            P("banpaku", "萬博紀念公園・太陽之塔", "吹田市", 34.8073f, 135.5300f, "文化,探索,親子", 210, "NT$180", 180, 4.4f, 6890, "太陽之塔本人比照片更有壓迫感，旁邊就是大型購物中心。"),
            P("rinku", "臨空城 Outlet", "泉佐野", 34.4098f, 135.2960f, "購物", 180, "看你自己", 2000, 4.0f, 3450, "關西機場前一站，回程當天補貨的最後機會。"),
            P("kissaten", "昭和喫茶店巡禮", "本町・心齋橋", 34.6790f, 135.4990f, "放空,美食,拍照", 90, "NT$300", 300, 4.6f, 1520, "布丁、哈密瓜蘇打與絨布沙發，拍起來自帶底片感。"),
            P("sennichimae", "千日前 迴轉壽司＋宵夜", "千日前", 34.6650f, 135.5035f, "美食", 75, "NT$450", 450, 4.1f, 2380, "宵夜場也在營業，人均不到 500 就能吃得很飽。"),
            P("tsuruhashi", "鶴橋商店街", "生野區", 34.6650f, 135.5310f, "美食,探索", 120, "NT$700", 700, 4.4f, 2150, "韓國城的炭火燒肉香，從車站一路飄到巷底。"),
        };

        /// <summary>隱藏景點：旅途中撿到紫色隱藏印章才會解鎖，排行程時可以拖進格子。價格是大約值。</summary>
        public static readonly Poi[] HiddenPois =
        {
            P("h_nakazaki", "中崎町咖啡巷弄", "北區", 34.7069f, 135.5045f, "放空,拍照,探索", 120, "約 NT$250", 250, 4.4f, 0, "老長屋改成的小咖啡店和雜貨店，一條巷子可以逛一下午。"),
            P("h_hozenji", "法善寺橫丁", "難波", 34.6680f, 135.5025f, "文化,探索,美食", 60, "免費", 0, 4.4f, 0, "道頓堀旁邊的石板小巷，長滿青苔的不動明王像要潑水參拜。"),
            P("h_tempozan", "天保山大摩天輪", "港區", 34.6563f, 135.4319f, "拍照,夜生活", 45, "約 NT$200", 200, 4.3f, 0, "從 USJ 搭渡輪過去，晚上可以看整片大阪灣夜景。"),
            P("h_amemura", "美國村三角公園", "心齋橋", 34.6720f, 135.4980f, "購物,探索,拍照", 60, "看你自己", 300, 4.1f, 0, "古著、街頭塗鴉和小吃，年輕人的集合地。"),
            P("h_nishinomaru", "大阪城西之丸庭園", "中央區", 34.6862f, 135.5228f, "拍照,文化,放空", 60, "約 NT$50", 50, 4.4f, 0, "人比天守閣前少很多，草坪正對著天守閣，拍照角度最好。"),
            P("h_doguya", "千日前道具屋筋商店街", "難波", 34.6655f, 135.5040f, "購物,探索", 60, "看你自己", 400, 4.3f, 0, "整條街都是廚具和食物模型，可以買到章魚燒烤盤。"),
            P("h_janjan", "新世界 Jan Jan 橫丁", "浪速區", 34.6505f, 135.5055f, "美食,探索,夜生活", 75, "約 NT$400", 400, 4.2f, 0, "昭和風的窄巷，串炸和將棋會所擠在一起。"),
            P("h_hepfive", "HEP FIVE 紅色摩天輪", "梅田", 34.7038f, 135.5003f, "拍照,夜生活", 30, "約 NT$180", 180, 4.3f, 0, "蓋在百貨頂樓的紅色摩天輪，梅田夜景一次看完。"),
            P("h_kaiyukan", "海遊館", "港區", 34.6545f, 135.4290f, "親子,拍照", 150, "約 NT$600", 600, 4.5f, 0, "超大水槽裡有鯨鯊，下雪天也能整個下午待在室內。"),
        };

        public static Poi Get(string id) { return Pois.FirstOrDefault(p => p.id == id) ?? HiddenPois.FirstOrDefault(p => p.id == id); }

        // ---------------- 天數與時段 ----------------
        public class DayDef { public int day; public string label, note; public string[] slots; }

        public static List<DayDef> Days(int days)
        {
            var list = new List<DayDef>();
            for (int d = 1; d <= days; d++)
            {
                if (d == 1) list.Add(new DayDef { day = d, label = "Day 1", note = "抵達", slots = new[] { "下午", "晚上" } });
                else if (d == days) list.Add(new DayDef { day = d, label = "Day " + d, note = "賦歸", slots = new[] { "上午" } });
                else list.Add(new DayDef { day = d, label = "Day " + d, note = "全天", slots = Slots });
            }
            return list;
        }

        public static PlanItem At(List<PlanItem> board, int day, string slot) { return board.FirstOrDefault(i => i.day == day && i.slot == slot); }
        public static PlanItem AllDayOf(List<PlanItem> board, int day) { return board.FirstOrDefault(i => i.day == day && i.slot == AllDaySlot); }

        // ---------------- 交通估算 ----------------
        static float Haversine(Poi a, Poi b)
        {
            const float R = 6371f;
            float dLat = (b.lat - a.lat) * Mathf.Deg2Rad, dLng = (b.lng - a.lng) * Mathf.Deg2Rad;
            float s = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) + Mathf.Cos(a.lat * Mathf.Deg2Rad) * Mathf.Cos(b.lat * Mathf.Deg2Rad) * Mathf.Sin(dLng / 2) * Mathf.Sin(dLng / 2);
            return 2 * R * Mathf.Asin(Mathf.Sqrt(s));
        }

        /// <summary>大阪大眾運輸粗估：起步 8 分＋每公里 3.6 分，取 5 分為單位。</summary>
        public static int Travel(Poi a, Poi b)
        {
            if (a == null || b == null || a.id == b.id) return 0;
            return Mathf.Max(5, Mathf.RoundToInt((8 + Haversine(a, b) * 3.6f) / 5f) * 5);
        }

        public static string Dur(int m) { return m >= 60 ? (m / 60) + " 小時" + (m % 60 > 0 ? " " + (m % 60) + " 分" : "") : m + " 分鐘"; }

        // ---------------- 喜好與預算 ----------------
        public static Dictionary<string, float> TeamWeights(TeamAnalysis team)
        {
            var w = new Dictionary<string, float>();
            foreach (var t in new[] { "美食", "購物", "拍照", "放空", "探索", "文化", "夜生活", "親子", "溫泉", "動漫" }) w[t] = 1;
            if (team == null) return w;
            foreach (var m in team.members)
                for (int i = 0; i < Dims.N; i++)
                    if (Dims.Tag[i] != null) w[Dims.Tag[i]] += m.dims[i] * 0.3f / Mathf.Max(1, team.members.Count) * 2f;
            // 你自己的偏好多算一點
            var me = team.members.FirstOrDefault(m => m.player.isUser);
            if (me != null) for (int i = 0; i < Dims.N; i++) if (Dims.Tag[i] != null) w[Dims.Tag[i]] += me.dims[i] * 0.2f;
            return w;
        }

        public static bool BudgetOk(Poi p, out string text)
        {
            var b = Session.Budget;
            if (p.cost == 0) { text = "免費景點，預算無壓力"; return true; }
            if (b.key == "thrifty")
            {
                bool ok = p.cost <= 700;
                text = ok ? "一人約 NT$" + p.cost + "，符合小資節奏" : "一人約 NT$" + p.cost + "，對「" + b.name + "」來說偏貴";
                return ok;
            }
            if (b.key == "standard")
            {
                bool ok = p.cost <= 1600;
                text = ok ? "一人約 NT$" + p.cost + "，落在標準預算裡" : "一人約 NT$" + p.cost + "，會吃掉當天不少預算";
                return ok;
            }
            text = "一人約 NT$" + p.cost + "，預算完全沒問題";
            return true;
        }

        public static float Score(Poi p, Dictionary<string, float> w)
        {
            float tag = p.tags.Average(t => { float v; return w.TryGetValue(t, out v) ? v : 1f; });
            string _;
            float fit = BudgetOk(p, out _) ? 0.6f : -1.4f;
            return tag + (p.rating - 4f) * 1.5f + fit;
        }

        // ---------------- 依 10 站選擇預排 ----------------
        class Anchor { public string poi; public string slot; public Anchor(string p, string s) { poi = p; slot = s; } }

        static string Majority(int r)
        {
            if (r >= Session.history.Count) return "";
            var res = Session.history[r];
            if (res.topAnswer == null) return "";
            return res.topAnswer.choice;
        }

        /// <summary>每一站全隊多數的選擇 → 對應一個要排進去的地點（錨點）。</summary>
        static List<Anchor> Anchors()
        {
            var a = new List<Anchor>();
            a.Add(new Anchor("usj", AllDaySlot));                                                 // 第 2 站
            a.Add(new Anchor(Majority(0) == "B" ? "kissaten" : "kuromon", "上午"));               // 第 1 站
            a.Add(new Anchor("dotonbori", "晚上"));                                                // 第 3 站
            a.Add(new Anchor(Majority(3) == "C" ? "kissaten" : "shinsaibashi", "晚上"));          // 第 4 站
            a.Add(new Anchor("osaka-castle", "上午"));                                             // 第 5 站
            string m6 = Majority(5);
            a.Add(new Anchor(m6 == "B" ? "nakanoshima" : m6 == "C" ? "tsuruhashi" : "kuromon", "下午"));
            a.Add(new Anchor("shinsekai", "晚上"));                                                // 第 7 站
            string m8 = Majority(7);
            a.Add(new Anchor(m8 == "B" ? "tenjinbashi" : m8 == "C" ? "amemura" : "umeda-sky", "晚上"));
            string m9 = Majority(8);
            a.Add(new Anchor(m9 == "B" ? "kaiyukan" : m9 == "C" ? "tenjinbashi" : "solaniwa", "下午"));
            if (Majority(9) == "C") a.Add(new Anchor("rinku", "上午"));                            // 第 10 站：回程補貨
            return a;
        }

        public static List<PlanItem> Seed(int days, Dictionary<string, float> w)
        {
            var board = new List<PlanItem>();
            var dayDefs = Days(days);
            var used = new HashSet<string>();

            foreach (var an in Anchors())
            {
                if (used.Contains(an.poi)) continue;
                var poi = Get(an.poi);
                if (poi == null) continue;
                if (an.slot == AllDaySlot)
                {
                    var d = dayDefs.FirstOrDefault(x => x.slots.Length == 3 && !board.Any(i => i.day == x.day));
                    if (d == null) continue;
                    board.Add(new PlanItem { day = d.day, slot = AllDaySlot, poiId = poi.id, by = "guide" });
                    used.Add(poi.id);
                    continue;
                }
                // 最後一天上午留給「回程前」的地點，其他從前面的天數找空位
                IEnumerable<DayDef> order = an.poi == "rinku" ? dayDefs.AsEnumerable().Reverse() : dayDefs;
                foreach (var d in order)
                {
                    if (AllDayOf(board, d.day) != null) continue;
                    if (!d.slots.Contains(an.slot) || At(board, d.day, an.slot) != null) continue;
                    if (an.poi != "rinku" && d.day == days && days > 1) continue;
                    board.Add(new PlanItem { day = d.day, slot = an.slot, poiId = poi.id, by = "guide" });
                    used.Add(poi.id);
                    break;
                }
            }

            // 剩下的時段：離同一天已排的地點近、時段屬性對得上、符合大家喜好
            foreach (var d in dayDefs)
            {
                if (AllDayOf(board, d.day) != null) continue;
                foreach (var slot in d.slots)
                {
                    if (At(board, d.day, slot) != null) continue;
                    var best = BestFor(board, d.day, slot, w, used);
                    if (best == null) continue;
                    used.Add(best.id);
                    board.Add(new PlanItem { day = d.day, slot = slot, poiId = best.id, by = "guide" });
                }
            }
            return board;
        }

        static Poi BestFor(List<PlanItem> board, int day, string slot, Dictionary<string, float> w, HashSet<string> used)
        {
            return Candidates(board, day, slot, w, used).Select(c => c.Key).FirstOrDefault();
        }

        /// <summary>某個時段的候選地點（由好到差），value 是成本（越小越好）。</summary>
        public static List<KeyValuePair<Poi, float>> Candidates(List<PlanItem> board, int day, string slot, Dictionary<string, float> w, HashSet<string> exclude)
        {
            var sameDay = board.Where(i => i.day == day && i.slot != slot).Select(i => Get(i.poiId)).Where(p => p != null).ToList();
            int cap = SlotMinutes.ContainsKey(slot) ? SlotMinutes[slot] : 210;
            return Pois.Where(p => !exclude.Contains(p.id) && !p.AllDay)
                .Select(p =>
                {
                    float near = sameDay.Count > 0 ? sameDay.Min(q => Travel(q, p)) : 15;
                    float slotFit = SlotTags.ContainsKey(slot) && p.tags.Any(t => SlotTags[slot].Contains(t)) ? -15 : 0;
                    float nightMismatch = slot != "晚上" && p.tags.Contains("夜生活") ? 25 : 0;
                    float tooLong = p.minutes > cap ? 30 : 0;
                    return new KeyValuePair<Poi, float>(p, Mathf.Min(near, 70) + slotFit + nightMismatch + tooLong - Score(p, w) * 4);
                })
                .OrderBy(x => x.Value).ToList();
        }

        /// <summary>前一個時段 → 這個時段的交通時間（同一天）。</summary>
        public static int TravelInto(List<PlanItem> board, PlanItem item)
        {
            var prev = PrevItem(board, item);
            return prev == null ? 0 : Travel(Get(prev.poiId), Get(item.poiId));
        }

        public static PlanItem PrevItem(List<PlanItem> board, PlanItem item)
        {
            int idx = System.Array.IndexOf(Slots, item.slot);
            for (int k = idx - 1; k >= 0; k--)
            {
                var p = At(board, item.day, Slots[k]);
                if (p != null) return p;
            }
            return null;
        }

        /// <summary>趣趣幫忙檢查：交通太遠、預算超過、時段塞不下、夜生活排在白天。</summary>
        public static List<PlanIssue> Issues(List<PlanItem> board, Dictionary<string, float> w)
        {
            var list = new List<PlanIssue>();
            foreach (var it in board.OrderBy(i => i.day).ThenBy(i => System.Array.IndexOf(Slots, i.slot)))
            {
                var poi = Get(it.poiId);
                if (poi == null) continue;
                string key = it.day + "-" + it.slot;
                int t = TravelInto(board, it);
                if (t >= 35)
                {
                    var prev = Get(PrevItem(board, it).poiId);
                    list.Add(new PlanIssue { slotKey = key, text = "Day " + it.day + " 從「" + prev.name + "」到「" + poi.name + "」要坐 " + t + " 分鐘的車，" + it.slot + "會少掉一大段。", fixPoi = FixFor(board, it, w) });
                }
                string bt;
                if (!BudgetOk(poi, out bt))
                    list.Add(new PlanIssue { slotKey = key, text = "Day " + it.day + " " + it.slot + "的「" + poi.name + "」" + bt.Replace("一人約", "一人要") + "。", fixPoi = FixFor(board, it, w) });
                if (it.slot != AllDaySlot && SlotMinutes.ContainsKey(it.slot) && poi.minutes > SlotMinutes[it.slot])
                    list.Add(new PlanIssue { slotKey = key, text = "「" + poi.name + "」大約要 " + Dur(poi.minutes) + "，塞進 Day " + it.day + " " + it.slot + "會很趕。", fixPoi = FixFor(board, it, w) });
                if (it.slot != "晚上" && it.slot != AllDaySlot && poi.tags.Contains("夜生活"))
                    list.Add(new PlanIssue { slotKey = key, text = "「" + poi.name + "」晚上才熱鬧，排在 Day " + it.day + " " + it.slot + "有點可惜。", fixPoi = FixFor(board, it, w) });
            }
            return list;
        }

        static string FixFor(List<PlanItem> board, PlanItem it, Dictionary<string, float> w)
        {
            var used = new HashSet<string>(board.Select(i => i.poiId));
            var c = Candidates(board, it.day, it.slot, w, used).FirstOrDefault();
            return c.Key != null ? c.Key.id : null;
        }

        /// <summary>為什麼推這個：挑最有說服力的理由。</summary>
        public static string Reason(Poi p, List<PlanItem> board, int day, string slot, Dictionary<string, float> w)
        {
            var bits = new List<string>();
            var topTag = p.tags.OrderByDescending(t => { float v; return w.TryGetValue(t, out v) ? v : 0; }).First();
            bits.Add("大家這趟很愛「" + topTag + "」");
            var sameDay = board.Where(i => i.day == day && i.slot != slot).Select(i => Get(i.poiId)).Where(x => x != null).ToList();
            if (sameDay.Count > 0)
            {
                int near = sameDay.Min(q => Travel(q, p));
                bits.Add("跟同一天的行程只差 " + near + " 分鐘車程");
            }
            string bt; BudgetOk(p, out bt); bits.Add(bt);
            return string.Join("，", bits.ToArray()) + "。";
        }

        /// <summary>隊友想加的地點：依他的人格挑一個還沒排進去、最合他胃口的。</summary>
        public static Poi WishFor(Persona persona, List<PlanItem> board, HashSet<string> taken)
        {
            var d = GameData.P(persona).dims;
            var w = new Dictionary<string, float>();
            foreach (var t in new[] { "美食", "購物", "拍照", "放空", "探索", "文化", "夜生活", "親子", "溫泉", "動漫" }) w[t] = 0.5f;
            for (int i = 0; i < Dims.N; i++) if (Dims.Tag[i] != null) w[Dims.Tag[i]] += d[i];
            if (persona == Persona.Explorer) w["文化"] += 2;
            if (persona == Persona.Capybara || persona == Persona.Taxi) w["溫泉"] += 2;
            if (persona == Persona.Shopper) w["動漫"] += 1;
            if (persona == Persona.Nanny) w["親子"] += 2;
            var used = new HashSet<string>(board.Select(i => i.poiId));
            return Pois.Where(p => !used.Contains(p.id) && !taken.Contains(p.id) && !p.AllDay)
                .OrderByDescending(p => p.tags.Average(t => w.ContainsKey(t) ? w[t] : 0) + p.rating * 0.2f).FirstOrDefault();
        }

        /// <summary>把隊友想去的地點放進最適合的時段：換掉那天最不合的趣趣預排項目。</summary>
        public static PlanItem PlaceWish(List<PlanItem> board, Poi poi, string who, Dictionary<string, float> w, out string reason)
        {
            PlanItem best = null; float bestCost = float.MaxValue;
            foreach (var it in board)
            {
                if (it.slot == AllDaySlot || it.by != "guide") continue;
                if (poi.tags.Contains("夜生活") && it.slot != "晚上") continue;
                var others = board.Where(i => i.day == it.day && i != it).Select(i => Get(i.poiId)).Where(x => x != null).ToList();
                float near = others.Count > 0 ? others.Min(q => Travel(q, poi)) : 15;
                float cost = near - (Score(Get(it.poiId), w) * -2f);
                if (cost < bestCost) { bestCost = cost; best = it; }
            }
            if (best == null) { reason = "行程都排滿了，這次先記在候補名單。"; return null; }
            var old = Get(best.poiId);
            reason = "放在 Day " + best.day + " " + best.slot + "，換掉「" + old.name + "」，跟同一天的行程最順路。";
            best.poiId = poi.id;
            best.by = who;
            return best;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TripMate
{
    public class EsimPlan { public string key, name, cap, good, bad; public int tier, listPerDay; public float gb; }
    public class EsimCarrier { public string key, name, line, speed, fit, caveat; }
    public class NetNeeds { public float gbPerDay; public List<string> drivers = new List<string>(); public string recommended; public int days, spots; }

    /// <summary>
    /// 去趣 eSIM：遊戲中的網路狀態（流量、降速、離線），以及排完行程後的方案推薦與購買。
    /// 方案、價格（全館 85 折）、電信商、可購買天數、用量估算、購買條款都照網頁版 tripmatenew。
    /// </summary>
    public static class Esim
    {
        public const float SiteDiscount = 0.85f;
        public static readonly int[] DayOptions = { 1, 3, 5, 7, 10, 15, 20, 30 };

        public static readonly EsimPlan[] Plans =
        {
            new EsimPlan { key = "m500", tier = 0, name = "每日 500MB", gb = 0.5f, listPerDay = 26, cap = "用完降速，仍可文字通訊", good = "只用通訊軟體報平安", bad = "開地圖、傳照片很快見底" },
            new EsimPlan { key = "g1", tier = 1, name = "每日 1GB", gb = 1f, listPerDay = 35, cap = "用完降速，仍可文字通訊", good = "通訊、查資料、偶爾導航", bad = "整天邊走邊導航不太夠" },
            new EsimPlan { key = "g2", tier = 2, name = "每日 2GB", gb = 2f, listPerDay = 48, cap = "用完降速，仍可文字通訊", good = "整天導航、社群、傳照片都夠", bad = "看影片、長時間直播會超" },
            new EsimPlan { key = "unlimited", tier = 3, name = "吃到飽不降速", gb = 99f, listPerDay = 95, cap = "不限流量、全程不降速", good = "不想算流量", bad = "用量不大有點超規格" },
        };

        public static readonly EsimCarrier[] Carriers =
        {
            new EsimCarrier { key = "softbank", name = "SoftBank", line = "原生", speed = "最高 5G", fit = "都市基地台密度高，熱門商圈、樂園最穩", caveat = "Android 手機無法開熱點分享，只有 iPhone 可以" },
            new EsimCarrier { key = "unlimited", name = "不限電信商", line = "多電信漫遊", speed = "最高 5G", fit = "自動切換當地最強訊號，跨區域行程不用煩惱", caveat = "漫遊線路會繞道海外伺服器，可能慢 1～2 秒" },
            new EsimCarrier { key = "docomo", name = "Docomo(IIJ)", line = "原生", speed = "4G", fit = "郊區涵蓋四家最高，有近郊行程最保險", caveat = "市區熱門景點偶爾會卡頓，速度為 4G" },
        };

        public static readonly string[] Terms =
        {
            "售出後不接受個人因素退換貨。",
            "每日用完高速額度會降速，隔天恢復。",
            "QR Code 只能掃一次，建議出發當天再安裝。",
            "抵達當地連網開通當天就算第一天。",
            "下單後通常 20 分鐘內 Email 收到 QR Code，有 24 小時 LINE 中文真人客服。",
            "不含當地門號與通話簡訊，原門號的漫遊通話仍可使用。",
        };

        /// <summary>隊友在自己手機上會選的方案（依人格，同網頁版 MATE_PLAN_BIAS）。</summary>
        public static string MateBias(Persona p)
        {
            switch (p)
            {
                case Persona.Soldier: case Persona.Camera: return "unlimited";
                case Persona.Capybara: return "g1";
                case Persona.Accountant: return "m500";
                default: return "g2";
            }
        }

        public static EsimPlan Plan(string key) { return Plans.FirstOrDefault(p => p.key == key) ?? Plans[2]; }
        public static EsimCarrier Carrier(string key) { return Carriers.FirstOrDefault(c => c.key == key) ?? Carriers[1]; }
        public static int SalePerDay(EsimPlan p) { return Mathf.RoundToInt(p.listPerDay * SiteDiscount); }
        public static int BillableDays(int days) { foreach (var d in DayOptions) if (d >= days) return d; return DayOptions[DayOptions.Length - 1]; }
        public static int Price(EsimPlan p, int days) { return SalePerDay(p) * BillableDays(days); }

        /// <summary>依「實際排出來的行程」估算每天要多少流量（網頁版 tripNetworkNeeds）。</summary>
        public static NetNeeds Needs(List<PlanItem> board, int days, int groups)
        {
            var pois = board.Select(i => Planner.Get(i.poiId)).Where(p => p != null).ToList();
            System.Func<string, int> count = t => pois.Count(p => p.tags.Contains(t));
            int photo = count("拍照"), night = count("夜生活"), explore = count("探索"), shop = count("購物");
            System.Func<int, float, float> perDay = (n, w) => n / (float)days * w;
            float gb = 0.6f + perDay(photo, 0.3f) + perDay(explore, 0.15f) + perDay(night, 0.12f) + perDay(shop, 0.1f) + (groups > 1 ? 0.4f : 0f);
            gb = Mathf.Max(0.4f, Mathf.Round(gb * 10f) / 10f);
            var n2 = new NetNeeds { gbPerDay = gb, days = days, spots = pois.Count };
            if (photo > 0) n2.drivers.Add(photo + " 個拍照點，照片和限動會一直上傳");
            if (groups > 1) n2.drivers.Add("會分流成 " + groups + " 組，每個人都要能各自定位");
            if (explore > 0) n2.drivers.Add(explore + " 個要靠導航鑽巷弄的行程");
            if (night > 0) n2.drivers.Add(night + " 個夜間行程，地下街和人潮處訊號最不穩");
            if (shop > 0) n2.drivers.Add(shop + " 個購物點，比價和退稅查詢都吃網路");
            if (n2.drivers.Count > 4) n2.drivers = n2.drivers.Take(4).ToList();
            n2.recommended = gb >= 2.2f ? "unlimited" : gb >= 1.2f ? "g2" : gb >= 0.7f ? "g1" : "m500";
            return n2;
        }

        /// <summary>依市區／郊區分布推薦電信商（網頁版 suggestCarrier）。</summary>
        public static string SuggestCarrier(List<PlanItem> board, out string why)
        {
            var center = new Poi { lat = 34.6687f, lng = 135.5013f, id = "_center" };
            var pois = board.Select(i => Planner.Get(i.poiId)).Where(p => p != null).ToList();
            if (pois.Count == 0) { why = "行程還沒排，先選會自動切換訊號的多電信漫遊。"; return "unlimited"; }
            var outs = pois.Where(p => Planner.Travel(center, p) >= 45).ToList(); // 約 10 公里以外
            float ratio = outs.Count / (float)pois.Count;
            if (outs.Count >= 2 && ratio >= 0.25f) { why = "行程有 " + outs.Count + " 個郊區景點，Docomo 的郊區涵蓋最高。"; return "docomo"; }
            if (outs.Count > 0) { why = "「" + string.Join("、", outs.Select(p => p.name).ToArray()) + "」在郊區，多電信漫遊會自動切最強訊號。"; return "unlimited"; }
            why = "行程都在市區，SoftBank 的都市基地台密度最高。";
            return "softbank";
        }

        // ---------------- 遊戲中的網路狀態 ----------------
        /// <summary>這一站已經用掉的行動流量（GB）。</summary>
        public static float used;

        public static string NetName
        {
            get
            {
                switch (Session.network)
                {
                    case "esim": return "去趣 eSIM";
                    case "roaming": return "電信漫遊";
                    case "wifi": return "免費 Wi-Fi";
                }
                return "飯店 Wi-Fi";
            }
        }

        public static float Cap
        {
            get
            {
                switch (Session.network)
                {
                    case "esim": return 2f;
                    case "roaming": return 1f;
                    default: return 0f;
                }
            }
        }

        /// <summary>在飯店有 Wi-Fi；只有免費 Wi-Fi 的人一出門就斷線。</summary>
        public static bool Online
        {
            get
            {
                if (string.IsNullOrEmpty(Session.network)) return true;
                if (World.Current != null && World.Current.key == "hotel") return true;
                return Session.network != "wifi";
            }
        }

        public static bool Throttled { get { return Online && Session.network == "roaming" && used >= Cap && !(World.Current != null && World.Current.key == "hotel"); } }

        /// <summary>訊號格數 0～4。</summary>
        public static int Bars
        {
            get
            {
                if (!Online) return 0;
                if (World.Current != null && World.Current.key == "hotel") return 4;
                if (Session.network == "esim") return 4;
                if (Throttled) return 1;
                return 2 + (Mathf.FloorToInt(Time.time * 0.7f) % 2); // 漫遊在人潮裡忽強忽弱
            }
        }

        /// <summary>用掉一點流量；回傳這件事能不能順利完成（離線或降速時不行，會記一次「沒辦成」）。</summary>
        public static bool Spend(float gb)
        {
            bool ok = Online && !Throttled;
            Drain(gb);
            if (!ok) fails++;
            return ok;
        }

        /// <summary>背景用量（邊走邊導航），不算成功失敗。</summary>
        public static void Drain(float gb)
        {
            if (!Online) return;
            if (Session.network != "esim" || used < Cap) used += gb;
        }

        public static void NewStation() { used = 0f; }

        // ---- 這趟的網路紀錄（定版後給大家看：網路到底卡了你幾次）----
        public static int fails;
        public static float offlineSec, slowSec;

        public static void ResetStats() { used = 0f; fails = 0; offlineSec = 0f; slowSec = 0f; }

        public static string StatsLine()
        {
            if (fails == 0 && offlineSec < 1f && slowSec < 1f) return "這趟網路全程順暢，0 次卡住";
            var parts = new List<string>();
            if (offlineSec >= 1f) parts.Add("離線 " + Mathf.CeilToInt(offlineSec / 60f) + " 分鐘");
            if (slowSec >= 1f) parts.Add("降速 " + Mathf.CeilToInt(slowSec / 60f) + " 分鐘");
            if (fails > 0) parts.Add(fails + " 件事因為網路沒辦成");
            return "這趟：" + string.Join("・", parts.ToArray());
        }
    }
}

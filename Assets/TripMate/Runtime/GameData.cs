using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TripMate
{
    /// <summary>六個偏好維度（和網頁版 DIMS 相同）。</summary>
    public static class Dims
    {
        public const int Food = 0, Shop = 1, Photo = 2, Chill = 3, Explore = 4, Hustle = 5, N = 6;
        public static readonly string[] Key = { "food", "shop", "photo", "chill", "explore", "hustle" };
        public static readonly string[] Label = { "美食", "購物", "拍照", "放空", "探索", "衝刺" };
        public static readonly string[] Verb = { "吃美食", "買到底", "拍好拍滿", "躺平放空", "鑽巷弄", "衝行程" };
        public static readonly string[] Tag = { "美食", "購物", "拍照", "放空", "探索", null };

        public static int Index(string key)
        {
            for (int i = 0; i < N; i++) if (Key[i] == key) return i;
            return -1;
        }
    }

    public class PersonaDef
    {
        public Persona p;
        public string key, name, shortName, title, trait, quote, agree, conflict;
        public string[] gear;
        public Color color, soft, accent;
        public float[] dims = new float[Dims.N];
        public Regex keyword;
        public string[] chatter = new string[10];
    }

    public class QuizOption { public string key, label, sub; }
    public class QuizQuestion { public string id, ask; public QuizOption a, b; }

    public class BudgetDef { public string key, name, range, desc; public int mealCap; public Color color; }
    public class NetworkDef { public string key, name, desc, perk, tag; public int price; }
    public class DestDef { public string key, name, note; public bool ready; }

    public class MockDef
    {
        public string name;
        public Color color;
        public Persona type, alt;
        public Dictionary<int, string> dLines = new Dictionary<int, string>();
    }

    public class OptionDef
    {
        public string letter, shortName, text, hint;
        public Dictionary<Persona, float> w = new Dictionary<Persona, float>();
        public float[] d = new float[Dims.N];
        public int price = -1;
        public bool needNet;
    }

    public class RoundDef
    {
        public int id;
        public string area, title, subtitle, clock, scene, line, ask, advice, light;
        public OptionDef[] options;
    }

    /// <summary>
    /// 全部遊戲資料。前 4 站、人格、快測、預算、網路、隊友都沿用網頁版 tripmatenew 的內容，
    /// 第 5～10 站是照同樣格式新增的。改題目只要改這個檔案。
    /// </summary>
    public static class GameData
    {
        // ---------------- 人格 ----------------
        public static readonly Persona[] Order =
        {
            Persona.Soldier, Persona.Capybara, Persona.Camera, Persona.Shopper,
            Persona.Accountant, Persona.Explorer, Persona.Taxi, Persona.Nanny,
        };

        static readonly Dictionary<Persona, PersonaDef> personas = new Dictionary<Persona, PersonaDef>();

        public static PersonaDef P(Persona p)
        {
            Init();
            PersonaDef d;
            return personas.TryGetValue(p, out d) ? d : personas[Persona.Capybara];
        }

        public static Persona FromKey(string key)
        {
            switch (key)
            {
                case "soldier": return Persona.Soldier;
                case "capybara": return Persona.Capybara;
                case "camera": return Persona.Camera;
                case "shopper": return Persona.Shopper;
                case "accountant": return Persona.Accountant;
                case "explorer": return Persona.Explorer;
                case "taxi": return Persona.Taxi;
                case "nanny": return Persona.Nanny;
            }
            return Persona.Base;
        }

        static void Def(Persona p, string key, string name, string shortName, string color, string soft, string accent,
                        string title, string trait, string quote, string[] gear, string agree, string conflict, string dims, string keyword)
        {
            var d = new PersonaDef
            {
                p = p, key = key, name = name, shortName = shortName, title = title, trait = trait, quote = quote,
                gear = gear, agree = agree, conflict = conflict,
                color = TM.Hex(color), soft = TM.Hex(soft), accent = TM.Hex(accent),
                dims = ParseDims(dims),
                keyword = new Regex(keyword, RegexOptions.IgnoreCase),
            };
            personas[p] = d;
        }

        static bool inited;

        static void Init()
        {
            if (inited) return;
            inited = true;
            Def(Persona.Soldier, "soldier", "行程特種兵", "特種兵", "#D9481F", "#FFE1D3", "#FF4D4F",
                "分秒必爭的晨型衝刺兵", "Excel 精準到分鐘、早起吃早餐、跑滿所有景點。", "離下一班車還有 4 分鐘，大家加快！",
                new[] { "運動頭帶", "碼錶運動手錶" }, "超響亮擊掌", "急速滑步離場", "hustle:3", "早起|準時|衝|趕|搶|排隊|刷|設施|效率|跑");
            Def(Persona.Capybara, "capybara", "隨性慢活水豚", "慢活水豚", "#2A83AD", "#DDF1FA", "#87D068",
                "睡飽才有靈魂的鬆弛大師", "佛系鬆弛感、睡到自然醒，是全隊情緒最穩定的安全氣囊。", "都可以啊，你們決定就好～",
                new[] { "充氣頸枕", "冰咖啡" }, "一起吐放空泡泡", "化成一灘黃水", "chill:3", "睡|躺|賴床|放空|休息|都可以|隨便|佛系|慢慢");
            Def(Persona.Camera, "camera", "移動打卡機", "打卡機", "#C93C7E", "#FFE0EE", "#FF6B6B",
                "隨時開機的行走攝影棚", "行程可以少走，但出片率不能低，專攻 IG 爆紅機位與網美光影。", "先別動！這道菜／這個景讓我拍兩張！",
                new[] { "拍立得相機", "頭頂墨鏡" }, "取景框框比心", "鏡頭戳人", "photo:3", "拍|照|打卡|IG|網美|出片|自拍|相機|布丁");
            Def(Persona.Shopper, "shopper", "暴走購物狂", "購物狂", "#B8440E", "#FFE7CC", "#EB2F96",
                "行李箱留半箱的掃貨獵人", "行李箱留半箱出發、退稅滿額達人，最容易引發分流的關鍵人物。", "這台灣買不到！退稅算下來等於打六折！",
                new[] { "滿載免稅購物袋", "保暖毛帽" }, "提袋開箱大歡呼", "購物紙袋無情衝撞", "shop:3", "買|購|逛|掃貨|退稅|藥妝|爆買|戰利品|shopping");
            Def(Persona.Accountant, "accountant", "旅費精算師", "精算師", "#7A5A12", "#F6ECCB", "#13C2C2",
                "每一塊錢都有去處的財務大臣", "匯率與信用卡回饋權威，主動去零頭分帳的財務大臣。", "這間有 10% 券，刷這張折抵最划算。",
                new[] { "復古計算機", "折價券票夾" }, "蓋章批准報銷", "甩出厚帳單打臉", "food:2,shop:1", "省|便宜|划算|券|折|匯率|分帳|預算|比價|回饋");
            Def(Persona.Explorer, "explorer", "靈魂探險家", "探險家", "#6245C4", "#ECE5FF", "#722ED1",
                "轉個彎就有故事的巷弄偵探", "City Walk 流浪者，熱愛下町巷弄、美術館與隱密小酒館。", "前面巷子很有味道，我們走過去看看！",
                new[] { "私房地圖手冊", "漁夫帽" }, "悄悄遞出私房地圖", "拉下鐵捲門自閉", "explore:3", "巷|散步|漫步|探險|走走|美術館|小酒館|在地|迷路|city ?walk");
            Def(Persona.Taxi, "taxi", "尊榮計程車星人", "計程車星人", "#9A7400", "#FFF3C4", "#F5A800",
                "能坐絕不站的移動貴族", "能坐絕不站，走路超過 12 分鐘立刻叫車的體力儲值型。", "走路要 15 分鐘？叫車平攤一人幾十塊，上車！",
                new[] { "懸浮計程車車頂燈", "VIP 徽章" }, "招手全員上車", "狂按喇叭催上車", "chill:2,food:1", "車|uber|計程|taxi");
            Def(Persona.Nanny, "nanny", "全能旅行保母", "旅行保母", "#1F7A55", "#DDF5EA", "#FADB14",
                "四次元百寶袋的全隊靠山", "四次元百寶袋（藥品、行動電源、備份護照），隨時照看全場。", "有人口渴嗎？有人需要行動電源嗎？",
                new[] { "救急醫療包", "和平徽章" }, "強行牽手大合照", "原地猛烈揮白旗", "hustle:1,chill:1,food:1", "照顧|藥品|胃藥|成藥|行動電源|暖暖包|喝水|保暖|大家|幫忙|備用");

            // 隊友台詞：每站依人格各一句（順序同 Order）
            for (int r = 0; r < Chatter.Length; r++)
                for (int i = 0; i < Order.Length; i++)
                    personas[Order[i]].chatter[r] = Chatter[r][i];
        }

        public static float[] ParseDims(string s)
        {
            var d = new float[Dims.N];
            if (string.IsNullOrEmpty(s)) return d;
            foreach (var part in s.Split(','))
            {
                var kv = part.Split(':');
                int i = Dims.Index(kv[0].Trim());
                if (i >= 0) d[i] += float.Parse(kv[1]);
            }
            return d;
        }

        static Dictionary<Persona, float> ParseW(string s)
        {
            var w = new Dictionary<Persona, float>();
            foreach (var part in s.Split(','))
            {
                var kv = part.Split(':');
                w[FromKey(kv[0].Trim())] = float.Parse(kv[1]);
            }
            return w;
        }

        // ---------------- 人格快測（3 題二選一 → 8 種人格） ----------------
        public static readonly QuizQuestion[] Quiz =
        {
            new QuizQuestion { id = "q1", ask = "在超好看的街景前，你的第一個動作是？",
                a = new QuizOption { key = "photo", label = "先拍！出片率最重要", sub = "機位、光線、角度我來喬" },
                b = new QuizOption { key = "deep", label = "先走進去，體驗最重要", sub = "照片之後再說，先聞聞味道" } },
            new QuizQuestion { id = "q2", ask = "旅途中的早晨，你通常是？",
                a = new QuizOption { key = "early", label = "07:00 起床衝第一攤", sub = "早餐是行程的一部分" },
                b = new QuizOption { key = "late", label = "睡到自然醒才有靈魂", sub = "中午出門也是一種節奏" } },
            new QuizQuestion { id = "q3", ask = "出發前的行程表，你的版本是？",
                a = new QuizOption { key = "plan", label = "Excel 排到分鐘，附備案", sub = "雨天備案、交通轉乘都寫好" },
                b = new QuizOption { key = "free", label = "到了再說，隨興隨緣", sub = "只訂機票飯店，其他看心情" } },
        };

        public static readonly Dictionary<string, Persona> QuizMap = new Dictionary<string, Persona>
        {
            { "deep-early-plan", Persona.Soldier }, { "deep-early-free", Persona.Explorer },
            { "deep-late-plan", Persona.Accountant }, { "deep-late-free", Persona.Capybara },
            { "photo-early-plan", Persona.Camera }, { "photo-early-free", Persona.Shopper },
            { "photo-late-plan", Persona.Nanny }, { "photo-late-free", Persona.Taxi },
        };

        // ---------------- 房間設定 ----------------
        public static readonly DestDef[] Destinations =
        {
            new DestDef { key = "osaka", name = "日本大阪", note = "2 月冬季劇本", ready = true },
            new DestDef { key = "tokyo", name = "日本東京", note = "劇本製作中", ready = false },
            new DestDef { key = "seoul", name = "韓國首爾", note = "劇本製作中", ready = false },
        };

        public static readonly BudgetDef[] Budgets =
        {
            new BudgetDef { key = "thrifty", name = "特級小資", range = "2 萬內", desc = "住青旅、吃巷弄、購物克制", mealCap = 800, color = TM.Hex("#2F9E62") },
            new BudgetDef { key = "standard", name = "標準玩家", range = "2～4 萬", desc = "商務旅館，該吃吃該買買", mealCap = 1500, color = TM.Hex("#3E8EDE") },
            new BudgetDef { key = "luxury", name = "奢華富豪", range = "4 萬以上", desc = "好飯店、名店預約，放心掃貨", mealCap = 99999, color = TM.Hex("#B8440E") },
        };

        public static readonly NetworkDef[] Networks =
        {
            new NetworkDef { key = "esim", name = "去趣 eSIM 每日 2GB", price = 41, desc = "導航、地圖、上傳都順，訊號滿格", perk = "網路一切順暢", tag = "推薦" },
            new NetworkDef { key = "roaming", name = "電信漫遊 1GB／日", price = 30, desc = "每天 1GB，用完降速，導航會延遲" },
            new NetworkDef { key = "wifi", name = "到當地找免費 Wi-Fi", price = 0, desc = "出了飯店就離線：沒地圖、沒導航" },
        };

        // ---------------- 隊友（電腦） ----------------
        public static readonly MockDef[] MockPool =
        {
            new MockDef { name = "小明", color = TM.Hex("#3E8EDE"), type = Persona.Soldier, alt = Persona.Nanny,
                dLines = new Dictionary<int, string> { { 1, "單人通道狂刷設施" }, { 3, "我先衝 Bic 再回來接人" }, { 6, "塔上看完馬上去串炸" } } },
            new MockDef { name = "阿美", color = TM.Hex("#E0508F"), type = Persona.Shopper, alt = Persona.Camera,
                dLines = new Dictionary<int, string> { { 0, "先拍窗外雪景再出門" }, { 3, "直奔藥妝店掃貨" }, { 7, "百貨特賣先買再說" } } },
            new MockDef { name = "阿強", color = TM.Hex("#2F9E62"), type = Persona.Capybara, alt = Persona.Taxi,
                dLines = new Dictionary<int, string> { { 0, "躺著叫外送到飯店" }, { 2, "直接叫車回飯店睡" }, { 8, "泡湯泡到天黑" } } },
            new MockDef { name = "小芸", color = TM.Hex("#7C5CE0"), type = Persona.Camera, alt = Persona.Explorer,
                dLines = new Dictionary<int, string> { { 2, "去喫茶店拍網美布丁" }, { 4, "繞到護城河拍倒影" } } },
            new MockDef { name = "阿凱", color = TM.Hex("#C98A00"), type = Persona.Accountant, alt = Persona.Taxi,
                dLines = new Dictionary<int, string> { { 0, "先查市場有沒有折價券" }, { 1, "比價完再決定買哪個" }, { 5, "比完三攤再買最便宜的" } } },
        };

        // ---------------- 10 站題目 ----------------
        static OptionDef O(string letter, string shortName, string hint, string text, string w, string d, int price = -1, bool needNet = false)
        {
            return new OptionDef { letter = letter, shortName = shortName, hint = hint, text = text, w = ParseW(w), d = ParseDims(d), price = price, needNet = needNet };
        }

        public static readonly RoundDef[] Rounds =
        {
            new RoundDef { id = 1, ask = "5 度的早上，要早起衝市場嗎？", area = "hotel", light = "morning", title = "晨間集合", subtitle = "大阪冬晨的起床考驗", clock = "07:00 清晨",
                scene = "2 月初的大阪清晨只有 5 度，窗外冷風颼颼，但今天原定要一早衝去木津卸賣市場吃排隊海鮮丼與白草莓⋯⋯",
                line = "早安⋯⋯窗外只有 5 度耶。今天本來要一早衝木津市場，吃排隊海鮮丼跟白草莓的，你還起得來嗎？",
                advice = "這半天直接分頭走，友情比較保險。",
                options = new[] {
                    O("A", "07:00 衝市場", "頂著寒風搶第一輪海膽", "07:00 準時掀被子出發！頂著寒風也要搶第一輪入座吃海膽！", "soldier:3,nanny:2", "food:3,hustle:3"),
                    O("B", "賴床到 10 點", "超商熱包子解決", "鑽回被窩賴床到 10 點，樓下超商買個熱包子和熱咖啡解決。", "capybara:3", "chill:3,food:1"),
                    O("C", "叫 Uber 直達", "不吹風，直達門口", "拒絕在寒風中走路吹風，立刻開 App 叫 Uber 直達市場門口。", "taxi:3,accountant:2", "food:2,chill:1"),
                } },
            new RoundDef { id = 2, ask = "整理券快沒了，你要怎麼辦？", area = "usj", light = "day", title = "USJ 的分歧考驗", subtitle = "整理券只剩下午極少時段", clock = "10:30 上午",
                scene = "全隊抵達 USJ，入園才發現「超級任天堂世界」整理券只剩下午極少時段，且園區人潮滿患——所有人同時掏出手機⋯⋯",
                line = "糟了！超級任天堂世界的整理券只剩下午幾個時段，園區又爆滿⋯⋯大家都掏出手機了，你要怎麼辦？",
                advice = "USJ 這 3 小時建議分流，晚點再約集合時間。",
                options = new[] {
                    O("A", "狂刷搶整理券", "狂刷 App 搶（要網路）", "狂刷 App 搶整理券與 Fast Pass，接著直衝城堡拍冬季雪景！", "soldier:3,camera:2", "hustle:3,photo:2", -1, true),
                    O("B", "商店掃貨", "買限定爆米花桶", "既然設施要排 120 分鐘，直接殺進商店把限定爆米花桶買齊！", "shopper:3", "shop:3"),
                    O("C", "漫步看巡演", "喝熱奶油啤酒看巡演", "放棄排隊，買杯熱奶油啤酒在園區街道漫步、看巡演。", "explorer:3,capybara:1", "explore:2,chill:1,food:1"),
                } },
            new RoundDef { id = 3, ask = "今晚吃哪邊？", area = "dotonbori", light = "dusk", title = "道頓堀晚餐攻防", subtitle = "預算與食慾的正面對決", clock = "18:00 傍晚",
                scene = "細雪中的道頓堀，霓虹全開、香味四溢。名店門口排著長龍，隔壁巷子也飄出醬香，大家的肚子同時叫了⋯⋯",
                line = "聞到了嗎？名店門口排了長長一條，可是隔壁巷子也飄出醬香⋯⋯大家肚子同時叫了，今晚吃哪邊？",
                advice = "晚餐可以各吃各的，甜點再會合。",
                options = new[] {
                    O("A", "名店螃蟹全席", "一人約 NT$2,500", "衝螃蟹名店吃全席，配黑門和牛串——來都來了，一人約 NT$2,500。", "camera:2,shopper:2", "food:3,photo:1", 2500),
                    O("B", "巷弄大阪燒", "3.6 分隱藏版，約 NT$600", "打開 Tabelog 找隔壁巷子 3.6 分的隱藏版大阪燒＋章魚燒，一人約 NT$600。", "accountant:3,explorer:2", "food:3,explore:1", 600),
                    O("C", "超商回飯店", "回飯店泡腳吃，約 NT$300", "腳快斷了，超商買熱食和罐裝啤酒，回飯店邊泡腳邊吃，一人約 NT$300。", "capybara:3,taxi:2", "chill:3,food:1", 300),
                } },
            new RoundDef { id = 4, ask = "只剩兩小時，怎麼逛？", area = "shinsaibashi", light = "night", title = "心齋橋分流行動", subtitle = "20:30 打烊前的最後衝刺", clock = "18:30 夜晚",
                scene = "夜幕降臨心齋橋筋商店街，藥妝店、電器行與古著店分散在不同街區，而店鋪即將在 20:30 打烊⋯⋯",
                line = "藥妝店、電器行、古著店散在不同街區，而且 20:30 就打烊⋯⋯只剩兩小時，你想怎麼分？",
                advice = "今晚分頭逛，回飯店再開戰利品發表會。",
                options = new[] {
                    O("A", "原地分流", "分頭逛，靠網路互傳（要網路）", "時間不夠了！原地解散分頭逛 2 小時，靠網路傳比價與照片，晚上飯店集合。", "shopper:2,explorer:2,camera:1,soldier:1", "shop:2,explore:2", -1, true),
                    O("B", "全員抱團", "大家一起一間間逛", "天冷迷路很麻煩！大家緊緊跟在一起，一間一間陪著逛到底。", "nanny:3,soldier:1", "shop:1,hustle:1"),
                    O("C", "星巴克駐紮", "找咖啡店等大家", "我完全走不動了，你們去逛，我找一間咖啡店坐著等你們來領我。", "capybara:2,taxi:2", "chill:3"),
                } },
            new RoundDef { id = 5, ask = "天守閣在排隊，先去哪？", area = "castle", light = "morning", title = "大阪城的雪中晨光", subtitle = "天守閣開門前的抉擇", clock = "08:40 早晨",
                scene = "清晨的大阪城公園飄著細雪，護城河結了一層薄冰。天守閣 9 點開門，售票口前已經開始排隊，而西之丸庭園的雪景正美⋯⋯",
                line = "天守閣 9 點開門，售票口已經在排隊了；可是庭園的雪景現在最美⋯⋯你想先去哪？",
                advice = "上午先分兩路，11 點在天守閣下面會合。",
                options = new[] {
                    O("A", "衝天守閣", "8 樓俯瞰雪中大阪", "開門就衝天守閣！8 樓展望台俯瞰整座雪中大阪，一層一層把歷史展看完。", "soldier:3,nanny:1,explorer:1", "hustle:2,explore:1", 130),
                    O("B", "庭園雪景散步", "熱甘酒＋雪景拍照", "不排隊了，買杯熱甘酒在庭園慢慢散步，拍天守閣配雪景。", "capybara:2,camera:2,explorer:1", "chill:2,photo:2", 60),
                    O("C", "坐遊船繞城", "坐著把石牆看完", "腳力留給晚上！搭遊船繞護城河一圈，坐著就把石牆看完。", "taxi:3,accountant:1", "chill:2,explore:1", 350),
                } },
            new RoundDef { id = 6, ask = "午餐怎麼吃？", area = "kuromon", light = "day", title = "黑門市場午餐戰", subtitle = "邊走邊吃還是坐下來吃", clock = "12:00 中午",
                scene = "黑門市場的走道擠滿了人，現烤海膽、和牛串和大顆草莓擺滿攤位，每一攤的價格都讓精算師皺起眉頭⋯⋯",
                line = "黑門市場好香！可是一串和牛就要一千多日圓⋯⋯午餐你想怎麼吃？",
                advice = "預算不同就各買各的，吃完在市場出口集合。",
                options = new[] {
                    O("A", "邊走邊吃豪華版", "海膽和牛一路吃，約 NT$1,800", "現烤海膽、和牛串、草莓大福一路吃過去，一人約 NT$1,800。", "shopper:2,camera:2", "food:3,photo:1", 1800),
                    O("B", "超市便當公園吃", "打折便當，約 NT$300", "比完價去旁邊超市買打折便當，到附近公園曬太陽吃，一人約 NT$300。", "accountant:3,capybara:2", "food:1,chill:2", 300),
                    O("C", "後巷老店定食", "在地人的店，約 NT$700", "鑽進市場後巷，找一間只有在地人的老店吃海鮮定食，一人約 NT$700。", "explorer:3,nanny:1", "food:2,explore:2", 700),
                } },
            new RoundDef { id = 7, ask = "黃昏了，去哪？", area = "shinsekai", light = "dusk", title = "新世界的昭和黃昏", subtitle = "高塔、串炸、還是復古遊樂場", clock = "17:00 黃昏",
                scene = "通天閣的霓虹在黃昏亮起，滿街的串炸招牌和昭和風遊樂場，空氣裡都是炸物和醬汁的味道⋯⋯",
                line = "通天閣的燈亮了！要上去看夕陽、去吃串炸，還是去那間昭和遊樂場？",
                advice = "高塔和串炸可以接著排，遊樂場的人晚點再來會合。",
                options = new[] {
                    O("A", "登塔看夕陽", "趁夕陽拍整片新世界", "排隊登上通天閣展望台，趁夕陽還沒下山拍整片新世界。", "camera:3,soldier:2", "photo:3,hustle:1", 300),
                    O("B", "串炸吃到撐", "醬汁只能沾一次", "直接坐進串炸老店，醬汁只能沾一次，一人約 NT$500 吃到撐。", "accountant:2,taxi:2,capybara:1", "food:3,chill:1", 500),
                    O("C", "昭和射擊遊樂場", "贏一堆奇怪獎品", "去昭和風射擊遊戲和復古電玩，贏一堆奇怪的獎品。", "explorer:2,shopper:2,nanny:1", "explore:2,shop:1", 200),
                } },
            new RoundDef { id = 8, ask = "日落、美食、特賣，選哪個？", area = "umeda", light = "dusk", title = "梅田高樓的日落時刻", subtitle = "空中庭園、地下街、百貨公司", clock = "16:30 傍晚",
                scene = "梅田的高樓之間吹著冷風，空中庭園的日落時段開始排隊，地下街飄著甜點香，百貨公司正在冬季特賣⋯⋯",
                line = "空中庭園的日落 40 分鐘後開始，可是地下街跟百貨特賣也在召喚⋯⋯你選哪邊？",
                advice = "日落時段想上樓的先去排，逛街的人 19:00 在大樓下會合。",
                options = new[] {
                    O("A", "空中庭園看日落", "360 度夕陽與夜景", "提早 40 分鐘排隊上空中庭園，等 360 度夕陽和城市夜景亮起來。", "camera:3,soldier:1,nanny:1", "photo:3,hustle:1", 350),
                    O("B", "地下街美食巡禮", "章魚燒、蛋包飯一路吃", "鑽進梅田地下街，一間一間吃章魚燒、蛋包飯和現烤起司塔。", "accountant:2,capybara:2,explorer:1", "food:3,explore:1", 500),
                    O("C", "百貨特賣掃貨", "最後一天，退稅一次辦好", "冬季特賣最後一天！直衝百貨和電器行，退稅一次辦好。", "shopper:3,taxi:1", "shop:3", 1500),
                } },
            new RoundDef { id = 9, ask = "腳快斷了，下午怎麼過？", area = "onsen", light = "day", title = "腳快走斷的那一天", subtitle = "泡湯、衝景點、還是自由行", clock = "14:00 下午",
                scene = "連走了好幾天，大家的步數都破兩萬。空庭溫泉就在附近，但海遊館和天神橋筋商店街也還沒去⋯⋯",
                line = "大家的腳都快走斷了⋯⋯下午要一起去泡溫泉休息，還是把握時間再衝？",
                advice = "想休息的去泡湯，想衝的去海遊館，晚餐再一起吃。",
                options = new[] {
                    O("A", "全員泡湯", "泡湯、穿浴衣、躺著", "全員去空庭溫泉泡湯、穿浴衣、躺休息區，一人約 NT$600。", "capybara:3,nanny:2,taxi:2", "chill:3", 600),
                    O("B", "海遊館＋摩天輪", "看鯨鯊、坐摩天輪", "不休息！衝海遊館看鯨鯊，出來直接坐摩天輪看港口夕陽。", "soldier:3,camera:2", "hustle:2,photo:2", 570),
                    O("C", "天神橋筋亂逛", "日本最長商店街", "一個人去日本最長的商店街亂逛，吃章魚燒、找喫茶店。", "explorer:3,shopper:1,accountant:1", "explore:2,food:1,shop:1", 400),
                } },
            new RoundDef { id = 10, ask = "最後一晚，你想怎麼過？", area = "hotel", light = "night", title = "回程前的最後一晚", subtitle = "行李箱已經關不起來了", clock = "21:30 夜晚",
                scene = "回到飯店，大家的行李箱都塞得關不起來。明天中午就要去機場，可是唐吉訶德營業到凌晨，臨空城 Outlet 也在機場前一站⋯⋯",
                line = "行李箱都關不起來了⋯⋯明天就要回家，最後這一晚你想怎麼過？",
                advice = "想補貨的結伴去，想休息的先整理行李，明早大廳集合。",
                options = new[] {
                    O("A", "深夜唐吉訶德", "補一輪零食藥妝", "趁唐吉訶德營業到凌晨，最後補一輪零食藥妝，行李再壓一壓。", "shopper:3,soldier:1,camera:1", "shop:3,hustle:1", 1200),
                    O("B", "早睡整理行李", "秤重、早睡、從容出發", "把行李好好整理、秤重，早點睡，明天從容去機場。", "nanny:3,capybara:2,accountant:1", "chill:2", 0),
                    O("C", "明早 Outlet 最後一搏", "明早最後衝刺再去機場", "今晚先睡，明天提早出發到臨空城 Outlet 最後衝刺，再直接去機場。", "accountant:2,taxi:2,shopper:1", "shop:2,chill:1", 2000),
                } },
        };

        /// <summary>隊友台詞：Chatter[站][人格]，人格順序同 Order。前 4 站沿用網頁版。</summary>
        static readonly string[][] Chatter =
        {
            new[] {
                "五點半就醒了，鞋都穿好了，誰還在被窩裡？",
                "5 度欸⋯⋯被窩就是我的領土，午餐再叫我。",
                "清晨的光超柔！窗戶結霜先讓我拍一張再說。",
                "市場隔壁的乾貨店 8 點開，要不要順路掃一波？",
                "早市海鮮丼比晚上便宜三成，早起等於賺到。",
                "市場後面那條巷子聽說有間老喫茶，我想去探探。",
                "走過去要 15 分鐘？我已經打開叫車 App 了。",
                "暖暖包我帶了 8 片，出門前每人先拿兩片！",
            },
            new[] {
                "整理券倒數 30 秒，手機電量 100%，準備開刷！",
                "要排 120 分鐘⋯⋯我可以在長椅上等你們嗎。",
                "城堡配雪景！這個機位今天不拍會後悔一輩子。",
                "限定爆米花桶只剩最後幾批，我先衝商店了！",
                "快速通關一張多少？我算一下每分鐘的成本。",
                "後街好像有冬季巡演，人比較少，要不要繞過去？",
                "園區這麼大⋯⋯有沒有接駁車可以坐？",
                "人太多了，走散的話就回這個門口集合喔！",
            },
            new[] {
                "晚餐 40 分鐘內解決，後面還有三個點要跑。",
                "我只想找個有暖氣的位子坐下來⋯⋯",
                "霓虹燈下拍食物，出片率直接翻倍！",
                "吃完剛好去旁邊那間 24 小時折扣店！",
                "名店一人兩千五？巷子那間 3.6 分只要六百。",
                "巷子裡飄出來的醬香，跟著味道走就對了。",
                "下雪了，哪間離車站近我就選哪間。",
                "有人胃不舒服嗎？我包包裡有帶胃藥。",
            },
            new[] {
                "藥妝 30 分、家電 40 分、剩下機動，照表操課！",
                "你們去逛，我找個有插座的位子幫自己充電。",
                "商店街的燈光好有氛圍，給我五分鐘拍夜景。",
                "滿五千日圓就能退稅，我的清單已經列好了！",
                "退稅記得同一間合併結帳，我幫大家算。",
                "堀江那邊的古著巷，我想自己去晃晃。",
                "逛完叫一台大車回飯店，戰利品太重了。",
                "分頭的話，每 30 分鐘在群組報平安喔！",
            },
            new[] {
                "8:55 到售票口，開門第一批進去，11 點前搞定。",
                "雪地裡的長椅⋯⋯擦一擦還是可以坐的吧。",
                "天守閣配雪景，這個角度一年只有幾天拍得到！",
                "聽說天守閣裡有限定御守，我要去看看。",
                "周遊卡可以免費進天守閣，有買的記得拿出來。",
                "我想沿著石牆走一圈，找那塊最大的巨石。",
                "公園也太大了吧⋯⋯有沒有園內的小火車可以坐？",
                "地上結冰很滑，大家走路小心，鞋底有防滑嗎？",
            },
            new[] {
                "午餐 30 分鐘，下午還有兩個點，邊走邊吃最省時。",
                "人好多⋯⋯可以找個有椅子的地方坐著吃嗎？",
                "海膽開殼的瞬間要錄起來，慢動作超好看！",
                "草莓一盒 3,000 日圓？可是這個品種台灣買不到欸！",
                "同一串和牛，市場口比裡面貴兩百日圓，往裡面走。",
                "我剛剛看到一條小巷，裡面有阿伯在賣關東煮。",
                "我腳好痠，吃完可以直接叫車去下一站嗎？",
                "生食吃太多會鬧肚子，我包包裡有整腸藥。",
            },
            new[] {
                "展望台排隊 20 分鐘，日落 17:20，現在去剛好。",
                "串炸店有暖桌嗎⋯⋯我想坐著等天黑。",
                "黃昏配霓虹，這是一天裡最好拍的十分鐘！",
                "射擊遊戲的獎品好醜好可愛，我全部都想要。",
                "串炸一支 100 日圓起，這裡是全大阪 CP 值最高的地方。",
                "後面的商店街有好幾間沒招牌的老店，好有味道。",
                "爬塔要排隊又要走樓梯？我在下面等你們。",
                "炸物吃多了容易渴，我先去買幾瓶熱茶。",
            },
            new[] {
                "日落 17:10，排隊 30 分，我已經算好要幾點出發。",
                "地下街有暖氣⋯⋯我宣布今天傍晚待在地下。",
                "空中庭園的日落是大阪最強機位，腳架我帶了！",
                "百貨今天最後一天打折，我的購物清單有兩頁。",
                "空中庭園門票 1,500 日圓，晚上去比較划算嗎？我查一下。",
                "地下街像迷宮一樣，我想試試不看地圖能不能走出去。",
                "梅田站太大了，我們直接在計程車招呼站集合好不好。",
                "高樓上風很大，帽子圍巾都戴好再上去。",
            },
            new[] {
                "休息是給沒排好行程的人，海遊館 15:00 的場次還有位！",
                "溫泉⋯⋯浴衣⋯⋯休息區的躺椅⋯⋯我已經在那裡了。",
                "鯨鯊的水槽超好拍，可是穿浴衣拍照也很可愛⋯⋯",
                "溫泉的販賣部有限定入浴劑，買來送人剛好。",
                "溫泉平日下午比假日便宜 500 日圓，今天去最划算。",
                "天神橋筋 2.6 公里長，我想從頭走到尾。",
                "溫泉有免費接駁車，我可以。",
                "大家腳起水泡了嗎？我有帶人工皮，泡湯前先貼好。",
            },
            new[] {
                "明早 9:00 退房、9:40 的車，我已經設好三個鬧鐘。",
                "我的行李箱⋯⋯坐上去應該就關得起來了吧。",
                "最後一晚要拍全員合照！大家先別換睡衣！",
                "唐吉訶德還有三樣東西沒買到，我十分鐘就回來！",
                "大家來對帳，這趟旅費我已經整理成表格了。",
                "飯店後面那間小酒館還亮著燈，最後一杯？",
                "明天去機場，我已經預約好大台的車了。",
                "行李秤我有帶，超重的趕快把東西分到我這邊。",
            },
        };
    }
}

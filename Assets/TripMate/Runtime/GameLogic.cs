using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TripMate
{
    public class Player
    {
        public string id, name;
        public Color color;
        public Persona type, alt;        // 隊友的人格（玩家自己的由快測決定）
        public bool isUser, isHost;
        public MockDef mock;
        public Teammate mate;            // 場景裡的角色（玩家自己是 null）
        public Persona Persona { get { return isUser ? Session.SeedPersona : type; } }
    }

    public class KeywordHit { public Persona key; public string word; }

    public class Answer
    {
        public Player player;
        public string choice;            // A／B／C／D（自己說一句）
        public string custom = "";
        public List<KeywordHit> hits = new List<KeywordHit>();
    }

    public class RoundResult
    {
        public int round;
        public List<Answer> answers = new List<Answer>();
        public bool consensus;
        public int topCount;
        public Answer topAnswer;
        public Answer Of(Player p) { return answers.FirstOrDefault(a => a.player == p); }
    }

    public class HintRow { public string tone, text, sub; }
    public class Duel { public bool agree; public Player a, b; }

    public class Hint
    {
        public string headline, sub;
        public List<HintRow> rows = new List<HintRow>();
        public List<Duel> duels = new List<Duel>();
    }

    public class MemberProfile
    {
        public Player player;
        public List<Answer> answers;
        public Persona persona;
        public Dictionary<Persona, float> score;
        public float[] dims;
        public List<KeyValuePair<Persona, int>> mix; // 前三名人格與百分比
    }

    public class PairInfo { public MemberProfile a, b; public float agree, cos; }

    public class TeamAnalysis
    {
        public List<MemberProfile> members;
        public int vibe;
        public string vibeLabel, vibeDesc;
        public PairInfo bestPair, splitPair;
        public List<KeyValuePair<int, int>> prefs; // 維度 index → 百分比
        public int splitPct;
    }

    /// <summary>
    /// 這一趟的設定與紀錄：房間、預算、天數、隊員、每站答案。
    /// 計分規則與網頁版相同（人格權重、默契值、共識、大提示）。
    /// </summary>
    public static class Session
    {
        public static string nickname = "趣友";
        public static bool isHost = true;
        public static string roomCode = "OSK-7Q2F";
        public static string destKey = "osaka";
        public static System.DateTime date = new System.DateTime(2027, 2, 3);
        public static int days = 5;
        public static int size = 4;
        public static string budget = "standard";
        public static string network = "";
        public static string quizKey = "";
        public static Persona SeedPersona = Persona.Base;
        public static readonly List<Player> players = new List<Player>();
        public static readonly List<RoundResult> history = new List<RoundResult>();
        public static int lastVibe = -1;
        /// <summary>場景互動收集到的旅行回憶。</summary>
        public static readonly List<string> memories = new List<string>();
        /// <summary>排完行程後買的 eSIM（null＝還沒買）。</summary>
        public static string boughtPlan, boughtCarrier;
        public static int boughtPrice;
        public static System.DateTime lockedAt;
        public static int lockedSpots;
        public static float lockedGb;
        public static bool triedEsimInGame;

        public static void AddMemory(string m)
        {
            if (!memories.Contains(m)) memories.Add(m);
        }

        public static Player Me { get { return players.FirstOrDefault(p => p.isUser); } }
        public static IEnumerable<Player> Mates { get { return players.Where(p => !p.isUser); } }
        public static int Consensus { get { return history.Count(h => h.consensus); } }
        public static BudgetDef Budget { get { return GameData.Budgets.FirstOrDefault(b => b.key == budget) ?? GameData.Budgets[1]; } }
        public static DestDef Dest { get { return GameData.Destinations.FirstOrDefault(d => d.key == destKey) ?? GameData.Destinations[0]; } }
        public static string TripLabel { get { return days + " 天 " + (days - 1) + " 夜"; } }

        public static void Reset()
        {
            players.Clear();
            history.Clear();
            network = "";
            quizKey = "";
            memories.Clear();
            boughtPlan = boughtCarrier = null;
            boughtPrice = 0;
            triedEsimInGame = false;
            Esim.ResetStats();
            SeedPersona = Persona.Base;
            lastVibe = -1;
        }

        public static string RandomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var r = new System.Random();
            var s = "OSK-";
            for (int i = 0; i < 4; i++) s += chars[r.Next(chars.Length)];
            return s;
        }

        /// <summary>和網頁版 buildPlayers 相同：房主排第一；加入別人房間時，房主是第一位電腦隊友。</summary>
        public static void BuildPlayers()
        {
            players.Clear();
            var me = new Player { id = "me", name = string.IsNullOrEmpty(nickname) ? "趣友" : nickname, color = TM.Hex("#FF6B35"), isUser = true, isHost = isHost };
            var mocks = new List<Player>();
            for (int i = 0; i < Mathf.Max(1, size - 1) && i < GameData.MockPool.Length; i++)
            {
                var m = GameData.MockPool[i];
                mocks.Add(new Player { id = "m" + i, name = m.name, color = m.color, type = m.type, alt = m.alt, mock = m, isHost = !isHost && i == 0 });
            }
            if (isHost) { players.Add(me); players.AddRange(mocks); }
            else { players.Add(mocks[0]); players.Add(me); players.AddRange(mocks.Skip(1)); }
        }
    }

    public static class Scoring
    {
        /// <summary>電腦隊友作答：依人格（主＋副）挑最合的選項；有專屬台詞時 40% 機率自己說一句。</summary>
        public static Answer PickMockAnswer(Player p, int r)
        {
            var opts = GameData.Rounds[r].options;
            string line;
            if (p.mock != null && p.mock.dLines.TryGetValue(r, out line) && Random.value < 0.4f)
                return MakeAnswer(p, "D", line);
            System.Func<OptionDef, float> fit = o => W(o, p.type) + W(o, p.alt) * 0.6f;
            var best = opts.OrderByDescending(fit).First();
            if (fit(best) > 0 && Random.value < 0.78f) return MakeAnswer(p, best.letter, "");
            return MakeAnswer(p, opts[Random.Range(0, 3)].letter, "");
        }

        static float W(OptionDef o, Persona p) { float v; return o.w.TryGetValue(p, out v) ? v : 0f; }

        public static Answer MakeAnswer(Player p, string choice, string custom)
        {
            var a = new Answer { player = p, choice = choice, custom = custom ?? "" };
            if (choice == "D") a.hits = Classify(a.custom);
            return a;
        }

        /// <summary>從「自己說一句」抓關鍵字，判斷偏向哪些人格（網頁版 classifyCustom）。</summary>
        public static List<KeywordHit> Classify(string text)
        {
            var hits = new List<KeywordHit>();
            if (string.IsNullOrEmpty(text)) return hits;
            foreach (var p in GameData.Order)
            {
                var m = GameData.P(p).keyword.Match(text);
                if (m.Success) hits.Add(new KeywordHit { key = p, word = m.Value });
            }
            return hits;
        }

        public static void Weights(int r, Answer a, out Dictionary<Persona, float> w, out float[] d)
        {
            w = new Dictionary<Persona, float>();
            d = new float[Dims.N];
            if (a == null) return;
            if (a.choice == "D")
            {
                if (a.hits.Count == 0) { w[Persona.Explorer] = 2; d[Dims.Explore] = 2; return; }
                foreach (var h in a.hits)
                {
                    float v; w.TryGetValue(h.key, out v); w[h.key] = v + 3;
                    var pd = GameData.P(h.key).dims;
                    for (int i = 0; i < Dims.N; i++) d[i] += pd[i];
                }
                return;
            }
            var opt = GameData.Rounds[r].options.First(o => o.letter == a.choice);
            foreach (var kv in opt.w) w[kv.Key] = kv.Value;
            for (int i = 0; i < Dims.N; i++) d[i] = opt.d[i];
        }

        /// <summary>人格判定：快測雛形（+2.5 起手分）＋各站權重，最高分勝出。</summary>
        public static MemberProfile Profile(Player p, List<Answer> answers, Persona seed)
        {
            var score = GameData.Order.ToDictionary(k => k, k => 0f);
            var dims = new float[Dims.N];
            if (seed != Persona.Base && score.ContainsKey(seed))
            {
                score[seed] += 2.5f;
                var sd = GameData.P(seed).dims;
                for (int i = 0; i < Dims.N; i++) dims[i] += sd[i] * 0.5f;
            }
            for (int r = 0; r < answers.Count; r++)
            {
                Dictionary<Persona, float> w; float[] d;
                Weights(r, answers[r], out w, out d);
                foreach (var kv in w) if (score.ContainsKey(kv.Key)) score[kv.Key] += kv.Value + (r + 1) * 0.001f;
                for (int i = 0; i < Dims.N; i++) dims[i] += d[i];
            }
            var ranked = GameData.Order.OrderByDescending(k => score[k]).ToList();
            var persona = score[ranked[0]] > 0 ? ranked[0] : Persona.Capybara;
            float total = score.Values.Sum(); if (total <= 0) total = 1;
            var mix = ranked.Take(3).Where(k => score[k] > 0).Select(k => new KeyValuePair<Persona, int>(k, Mathf.RoundToInt(score[k] / total * 100))).ToList();
            return new MemberProfile { player = p, answers = answers, persona = persona, score = score, dims = dims, mix = mix };
        }

        public static bool SameChoice(Answer a, Answer b)
        {
            if (a == null || b == null) return false;
            if (a.choice == "D" && b.choice == "D")
            {
                var keys = new HashSet<Persona>(a.hits.Select(h => h.key));
                return b.hits.Any(h => keys.Contains(h.key));
            }
            return a.choice == b.choice;
        }

        public static string ShortOf(int r, Answer a)
        {
            if (a == null) return "發呆";
            if (a.choice == "D") return a.custom;
            return GameData.Rounds[r].options.First(o => o.letter == a.choice).shortName;
        }

        static float DimDistance(float[] a, float[] b)
        {
            float s = 0; for (int i = 0; i < Dims.N; i++) s += (a[i] - b[i]) * (a[i] - b[i]);
            return Mathf.Sqrt(s);
        }

        static float Cosine(float[] a, float[] b)
        {
            float dot = 0, na = 0, nb = 0;
            for (int i = 0; i < Dims.N; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
            return na > 0 && nb > 0 ? dot / Mathf.Sqrt(na * nb) : 0;
        }

        /// <summary>75% 共識：最大同選人數 ≥ 人數 × 0.75（無條件進位）。</summary>
        public static void Consensus(RoundResult res)
        {
            int n = res.answers.Count;
            int best = 0; Answer top = null;
            foreach (var a in res.answers)
            {
                int c = res.answers.Count(b => b == a || SameChoice(a, b));
                if (c > best) { best = c; top = a; }
            }
            res.topCount = best;
            res.topAnswer = top;
            res.consensus = best >= Mathf.CeilToInt(n * 0.75f);
        }

        /// <summary>即時默契值：每兩人「選一樣」的比例平均（網頁版 liveVibe）。</summary>
        public static int LiveVibe()
        {
            var ps = Session.players;
            var h = Session.history;
            if (h.Count == 0 || ps.Count < 2) return -1;
            var pairs = new List<float>();
            for (int i = 0; i < ps.Count; i++)
                for (int j = i + 1; j < ps.Count; j++)
                {
                    int agree = h.Count(rr => SameChoice(rr.Of(ps[i]), rr.Of(ps[j])));
                    pairs.Add(agree / (float)h.Count);
                }
            return Mathf.Clamp(Mathf.RoundToInt(35 + 65 * pairs.Average()), 12, 99);
        }

        static string Money(int n) { return "NT$" + n.ToString("N0"); }

        /// <summary>預算反饋：有價格的站，把你的選擇和設定的預算比一比。</summary>
        public static HintRow BudgetFeedback(int r, Answer me)
        {
            if (me == null || me.choice == "D") return null;
            var opt = GameData.Rounds[r].options.First(o => o.letter == me.choice);
            if (opt.price < 0) return null;
            var b = Session.Budget;
            int price = opt.price;
            if (b.key == "thrifty" && price > b.mealCap) return new HintRow { tone = "warn", text = "超預算了", sub = "這一項 " + Money(price) + "，你設的是「" + b.name + "」。" };
            if (b.key == "standard" && price > b.mealCap) return new HintRow { tone = "warn", text = "小小超支，但值得", sub = "這一項 " + Money(price) + "，之後找便宜的平衡回來。" };
            if (b.key == "luxury" && price <= 600) return new HintRow { tone = "luxe", text = "預算還有一大半沒用", sub = "只花 " + Money(price) + "，可以再加點好料。" };
            return new HintRow { tone = "ok", text = "預算剛好", sub = Money(price) + "，落在「" + b.name + "」裡。" };
        }

        /// <summary>大提示：只點出最極端的分歧或驚人的共識，再列誰跟你一樣、你說的話抓到什麼。</summary>
        public static Hint BuildHint(RoundResult res, bool lagged, bool won)
        {
            int r = res.round;
            var active = res.answers;
            var hint = new Hint();
            Answer wa = null, wb = null; float worst = 0;

            var letters = new HashSet<string>(active.Select(a => a.choice));
            if (active.Count >= 2 && letters.Count == 1 && !letters.Contains("D"))
            {
                hint.headline = "全隊都選「" + ShortOf(r, active[0]) + "」";
                hint.sub = "這種默契可以直接組戰隊出道。";
            }
            else
            {
                for (int i = 0; i < active.Count; i++)
                    for (int j = i + 1; j < active.Count; j++)
                    {
                        Dictionary<Persona, float> w1, w2; float[] d1, d2;
                        Weights(r, active[i], out w1, out d1);
                        Weights(r, active[j], out w2, out d2);
                        float dist = DimDistance(d1, d2);
                        if (dist > worst) { worst = dist; wa = active[i]; wb = active[j]; }
                    }
                if (wa != null)
                {
                    hint.headline = wa.player.name + " vs " + wb.player.name + " 差最多";
                    hint.sub = "一個要「" + ShortOf(r, wa) + "」，一個想「" + ShortOf(r, wb) + "」。";
                }
                else
                {
                    hint.headline = "全隊節奏一致";
                    hint.sub = "這一站可以放心一起行動。";
                }
            }

            var me = active.FirstOrDefault(a => a.player.isUser);
            var same = active.Where(a => !a.player.isUser && SameChoice(a, me)).Select(a => a.player.name).ToList();
            if (same.Count > 0)
                hint.rows.Add(new HintRow { tone = "ok", text = string.Join("、", same.ToArray()) + " 跟你選一樣", sub = same.Count >= active.Count - 1 ? "全隊同步，默契拉滿。" : "默契 +1。" });
            else if (active.Count > 1)
                hint.rows.Add(new HintRow { tone = "warn", text = "只有你這樣選", sub = "分頭行動也是旅行的一部分。" });

            if (me != null && me.choice == "D")
            {
                var words = me.hits.Select(h => h.word).Distinct().ToList();
                if (words.Count > 0)
                {
                    var traits = me.hits.Select(h => GameData.P(h.key).shortName).Distinct().ToArray();
                    hint.rows.Add(new HintRow { tone = "ok", text = "你說「" + me.custom + "」，抓到「" + string.Join("」「", words.ToArray()) + "」", sub = "很" + string.Join("、", traits) + "，排行程會往這方向找。" });
                }
                else hint.rows.Add(new HintRow { tone = "warn", text = "你說「" + me.custom + "」，記下來了", sub = "沒抓到明確關鍵字，但會列入參考。" });
            }

            if (r == 3 && me != null && me.choice == "A" && Session.network != "esim")
                hint.rows.Add(new HintRow { tone = "warn", text = "分頭之後，你的網路很難互傳位置", sub = Session.network == "wifi" ? "免費 Wi-Fi 一離開店家就斷線。" : "漫遊在人潮裡最容易降速。" });
            if (lagged) hint.rows.Add(new HintRow { tone = "warn", text = "剛才卡住的那幾秒，整理券被搶完了。" });
            if (won) hint.rows.Add(new HintRow { tone = "ok", text = "2 秒就搶到整理券，去趣 eSIM 在人潮裡也滿速。" });
            var fb = BudgetFeedback(r, me);
            if (fb != null) hint.rows.Add(fb);

            // 招牌動作：先演最合拍的一對（優先有你在的），再演差最多的一對；由隊友對你發動
            Player ma = null, mb = null; bool withMe = false;
            for (int i = 0; i < active.Count; i++)
                for (int j = i + 1; j < active.Count; j++)
                {
                    if (!SameChoice(active[i], active[j])) continue;
                    bool wm = active[i].player.isUser || active[j].player.isUser;
                    if (ma == null || (wm && !withMe)) { ma = active[i].player; mb = active[j].player; withMe = wm; }
                }
            if (ma != null) hint.duels.Add(Front(true, ma, mb));
            if (wa != null && worst > 0) hint.duels.Add(Front(false, wa.player, wb.player));
            return hint;
        }

        static Duel Front(bool agree, Player a, Player b)
        {
            return a.isUser ? new Duel { agree = agree, a = b, b = a } : new Duel { agree = agree, a = a, b = b };
        }

        public static TeamAnalysis Analyze()
        {
            var members = Session.players.Select(p =>
                Profile(p, Session.history.Select(h => h.Of(p)).ToList(), p.isUser ? Session.SeedPersona : p.type)).ToList();
            var pairs = new List<PairInfo>();
            int hc = Session.history.Count;
            for (int i = 0; i < members.Count; i++)
                for (int j = i + 1; j < members.Count; j++)
                {
                    var a = members[i]; var b = members[j];
                    float agree = hc > 0 ? Session.history.Count(h => SameChoice(h.Of(a.player), h.Of(b.player))) / (float)hc : 0;
                    pairs.Add(new PairInfo { a = a, b = b, agree = agree, cos = Cosine(a.dims, b.dims) });
                }
            int vibe = pairs.Count == 0 ? 60 :
                Mathf.Clamp(Mathf.RoundToInt(35 + 65 * (0.45f * pairs.Average(x => x.agree) + 0.55f * pairs.Average(x => x.cos))), 12, 99);
            var ranked = pairs.OrderByDescending(x => x.cos + x.agree).ToList();

            var team = new float[Dims.N];
            foreach (var m in members) for (int i = 0; i < Dims.N; i++) team[i] += m.dims[i];
            float tot = team.Sum(); if (tot <= 0) tot = 1;
            var prefs = Enumerable.Range(0, Dims.N).Select(i => new KeyValuePair<int, int>(i, Mathf.RoundToInt(team[i] / tot * 100)))
                .OrderByDescending(x => x.Value).ToList();

            string label, desc;
            if (vibe >= 80) { label = "靈魂同步戰隊"; desc = "節奏超合拍，同樂時段放心排滿。"; }
            else if (vibe >= 60) { label = "互補型冒險團"; desc = "偏好有落差，適度分流更盡興。"; }
            else { label = "分流保友情團"; desc = "節奏落差明顯，記得排分流時段。"; }

            return new TeamAnalysis
            {
                members = members, vibe = vibe, vibeLabel = label, vibeDesc = desc,
                bestPair = ranked.FirstOrDefault(), splitPair = ranked.Count > 1 ? ranked.Last() : null,
                prefs = prefs, splitPct = Mathf.Clamp(Mathf.RoundToInt(18 + (100 - vibe) * 0.4f), 15, 45),
            };
        }

        public static int TopDim(float[] d)
        {
            int best = 0; for (int i = 1; i < Dims.N; i++) if (d[i] > d[best]) best = i;
            return best;
        }

        public static string PairReason(PairInfo pair, bool good)
        {
            if (good)
            {
                for (int r = 0; r < Session.history.Count; r++)
                {
                    var a = Session.history[r].Of(pair.a.player); var b = Session.history[r].Of(pair.b.player);
                    if (SameChoice(a, b)) return "第 " + (r + 1) + " 站都選了「" + ShortOf(r, a) + "」，偏好雷達也最接近。";
                }
                return "偏好雷達最接近，一起行動最省心。";
            }
            int ta = TopDim(pair.a.dims), tb = TopDim(pair.b.dims);
            return ta == tb ? "節奏落差最大，建議把分流時段排開。" : "一個想" + Dims.Verb[ta] + "、一個想" + Dims.Verb[tb] + "，分流時段最適合他們。";
        }
    }
}

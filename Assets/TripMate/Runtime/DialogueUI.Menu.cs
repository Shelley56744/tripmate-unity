using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TripMate
{
    /// <summary>主選單 → 建立／加入房間 → 等待室 → 旅遊人格快測（流程同網頁版 SetupScreen）。</summary>
    public partial class DialogueUI
    {
        string nickField = "";
        string codeField = "";
        bool joinOk;
        bool menuDone;
        int lobbyJoined;
        float lobbyNextJoin, autoStartAt;

        int quizIdx;
        readonly Dictionary<string, string> quizAns = new Dictionary<string, string>();
        float quizBorn, revealBorn;
        Persona revealP;
        bool revealGo;

        // ---------------- 流程 ----------------
        public IEnumerator MenuCo()
        {
            Busy = true;
            if (player) player.locked = true;
            Session.roomCode = Session.RandomCode();
            menuDone = false;
            screen = Page.Title;
            while (!menuDone)
            {
                if (screen == Page.Lobby)
                {
                    int need = Session.players.Count - 1;
                    if (lobbyJoined < need && Time.time > lobbyNextJoin)
                    {
                        lobbyJoined++;
                        lobbyNextJoin = Time.time + Random.Range(0.6f, 1.1f);
                        Sfx.Pop(1f + lobbyJoined * 0.1f);
                    }
                    if (!Session.isHost && lobbyJoined >= need && autoStartAt <= 0) autoStartAt = Time.time + 1.8f;
                    if (autoStartAt > 0 && Time.time > autoStartAt) menuDone = true;
                    if (Session.isHost && lobbyJoined >= need && Key("space")) menuDone = true;
                }
                yield return null;
            }
            Sfx.Fanfare();
            screen = Page.None;
        }

        void EnterLobby()
        {
            Session.nickname = string.IsNullOrEmpty(nickField.Trim()) ? "趣友" : nickField.Trim();
            Session.BuildPlayers();
            lobbyJoined = Session.isHost ? 0 : 1; // 加入別人房間時，房主已經在裡面
            lobbyNextJoin = Time.time + 0.7f;
            autoStartAt = 0;
            GUIUtility.keyboardControl = 0;
            screen = Page.Lobby;
            Sfx.Chime();
        }

        public IEnumerator QuizCo()
        {
            Busy = true;
            if (player) player.locked = true;
            if (cam) { cam.showcase = false; cam.close = true; }
            Vector3 fwd = cam ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            player.transform.rotation = Quaternion.LookRotation(-fwd);

            quizAns.Clear();
            quizIdx = 0;
            quizBorn = Time.time;
            screen = Page.Quiz;
            while (quizIdx < GameData.Quiz.Length)
            {
                if (Key("1")) PickQuiz(true);
                else if (Key("2")) PickQuiz(false);
                yield return null;
            }
            yield return new WaitForSeconds(0.25f);

            string key = string.Join("-", GameData.Quiz.Select(q => quizAns[q.id]).ToArray());
            Persona p;
            if (!GameData.QuizMap.TryGetValue(key, out p)) p = Persona.Capybara;
            Session.quizKey = key;
            Session.SeedPersona = p;

            var q2 = player.GetComponent<Ququ>();
            if (q2) q2.SetPersona(p);
            Fx.Sparkle(player.transform.position + Vector3.up, 40);
            Fx.Confetti(player.transform.position + Vector3.up * 0.5f, 60);
            Sfx.Fanfare();
            StartCoroutine(player.Hop(2));
            revealP = p;
            revealBorn = Time.time;
            revealGo = false;
            screen = Page.QuizReveal;
            yield return new WaitForSeconds(0.6f);
            while (!revealGo && !Key("space")) yield return null;
            screen = Page.None;
            if (cam) cam.close = false;
            yield return null;
        }

        void PickQuiz(bool a)
        {
            if (quizIdx >= GameData.Quiz.Length) return;
            var q = GameData.Quiz[quizIdx];
            quizAns[q.id] = a ? q.a.key : q.b.key;
            Sfx.Pop(1f + quizIdx * 0.12f);
            quizIdx++;
            quizBorn = Time.time;
        }

        // ---------------- 畫面 ----------------
        Rect PanelRect(float w, float h, float y = -1)
        {
            if (y < 0) y = (H - h) * 0.5f;
            return new Rect(W * 0.5f - w * 0.5f, y, w, h);
        }

        void DrawTitle()
        {
            var p = PanelRect(980, 650);
            Shadowed(p, new Color(1f, 0.97f, 0.93f, 0.96f), 40);
            var logo = St(sBanner, 96, Persimmon, TextAnchor.MiddleCenter, true);
            GUI.Label(new Rect(p.x + 4, p.y + 64, p.width, 120), "去趣 TripMate", St(logo, 96, Ink, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(p.x, p.y + 60, p.width, 120), "去趣 TripMate", logo);
            GUI.Label(new Rect(p.x, p.y + 190, p.width, 50), "和朋友一起排行程的冒險 RPG", St(sTitle, 34, Ink, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(p.x, p.y + 240, p.width, 40), "10 站情境題 × 收集共識 × 最後一起把行程排好", St(sSmall, 24, Grey, TextAnchor.MiddleCenter, false));
            if (Button(new Rect(p.center.x - 400, p.y + 320, 380, 96), "建立房間", Persimmon, Color.white, 34))
            {
                Session.isHost = true;
                Session.roomCode = Session.RandomCode();
                screen = Page.Create;
                Sfx.Pop();
            }
            if (Button(new Rect(p.center.x + 20, p.y + 320, 380, 96), "加入房間", TM.Hex("#3E8EDE"), Color.white, 34))
            {
                Session.isHost = false;
                joinOk = false;
                codeField = "";
                screen = Page.Join;
                Sfx.Pop();
            }
            if (Button(new Rect(p.center.x - 260, p.y + 440, 520, 80), "趣趣商店　·　印章 " + Accessories.Wallet + " 枚", TM.Hex("#8C5BE0"), Color.white, 30))
            {
                shopTab = 0;
                screen = Page.Shop;
                Sfx.Pop();
            }
            GUI.Label(new Rect(p.x, p.yMax - 70, p.width, 40), "目前開放：日本大阪・2 月冬季劇本", St(sSmall, 22, Grey, TextAnchor.MiddleCenter, false));
        }

        // ---------------- 趣趣商店（主選單）----------------
        int shopTab;   // 0 配件，1 隱藏景點

        void DrawShop()
        {
            float pw = 880;
            var p = new Rect(W - pw - 40, 50, pw, 980);
            Shadowed(p, Cream, 36);
            GUI.Label(new Rect(p.x + 40, p.y + 26, 400, 60), "趣趣商店", St(sTitle, 42, Ink, TextAnchor.MiddleLeft, true));
            var wr = new Rect(p.x + 300, p.y + 34, 260, 52);
            Box(wr, TM.Hex("#FFE7A8"), 26);
            Circle(new Vector2(wr.x + 28, wr.center.y), 30, TM.Hex("#E2463B"));
            GUI.Label(new Rect(wr.x + 52, wr.y, wr.width - 60, wr.height), "印章 " + Accessories.Wallet + " 枚", St(sName, 26, Ink, TextAnchor.MiddleLeft, true));
            if (Button(new Rect(p.xMax - 160, p.y + 32, 120, 56), "返回", TM.Hex("#E3D6C6"), Ink, 24)) { screen = Page.Title; Sfx.Pop(); }

            // 分頁
            string[] tabs = { "配件", "隱藏景點 " + Accessories.HiddenFound().Count + "／" + Accessories.HiddenSpots.Length };
            for (int i = 0; i < 2; i++)
            {
                var tr = new Rect(p.x + 40 + i * 250, p.y + 106, 236, 56);
                bool on = shopTab == i;
                Box(tr, on ? Ink : TM.Hex("#EEE8E0"), 28);
                GUI.Label(tr, tabs[i], St(sName, 24, on ? Color.white : Ink, TextAnchor.MiddleCenter, true));
                if (GUI.Button(tr, GUIContent.none, GUIStyle.none)) { shopTab = i; Sfx.Pop(); }
            }
            GUI.Label(new Rect(p.x + 560, p.y + 106, pw - 600, 56), shopTab == 0 ? "旅途中撿印章來換" : "撿到紫色印章就解鎖", St(sSmall, 21, Grey, TextAnchor.MiddleRight, false));

            if (shopTab == 0) DrawShopItems(p);
            else DrawShopHidden(p);
        }

        void DrawShopItems(Rect p)
        {
            float cw = 256, ch = 150, gap = 16;
            float x0 = p.x + 40, y0 = p.y + 184;
            var me = player ? player.GetComponent<Ququ>() : null;
            for (int i = 0; i < Accessories.All.Length; i++)
            {
                var a = Accessories.All[i];
                var r = new Rect(x0 + (i % 3) * (cw + gap), y0 + (i / 3) * (ch + gap), cw, ch);
                bool owned = Accessories.Owned(a.id);
                bool worn = Accessories.Equipped(a.slot) == a.id;
                if (worn) Box(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), Green, 22);
                Box(r, Color.white, 18);
                float lum = a.color.r * 0.3f + a.color.g * 0.59f + a.color.b * 0.11f;
                Circle(new Vector2(r.x + 34, r.y + 34), 44, lum > 0.92f ? TM.Hex("#E9E4F5") : a.color);
                GUI.Label(new Rect(r.x + 12, r.y + 12, 44, 44), Accessories.SlotName[a.slot].Substring(0, 1), St(sName, 18, lum > 0.6f ? Ink : Color.white, TextAnchor.MiddleCenter, true));
                GUI.Label(new Rect(r.x + 66, r.y + 10, cw - 72, 36), a.name, St(sCardName, 24, Ink, TextAnchor.MiddleLeft, true));
                GUI.Label(new Rect(r.x + 66, r.y + 44, cw - 72, 28), a.desc, St(sSmall, 17, Grey, TextAnchor.MiddleLeft, false));
                var br = new Rect(r.x + 14, r.yMax - 58, cw - 28, 46);
                if (!owned)
                {
                    bool can = Accessories.Wallet >= a.price;
                    if (Button(br, a.price + " 枚印章換", TM.Hex("#8C5BE0"), Color.white, 21, can))
                    {
                        if (Accessories.Buy(a.id))
                        {
                            Accessories.Equip(a.id);
                            Accessories.Apply(me);
                            Sfx.Fanfare();
                            if (player) Fx.Sparkle(player.transform.position + Vector3.up * 1.2f, 30);
                            Toast("換到「" + a.name + "」，已經穿上了！");
                        }
                    }
                }
                else if (worn)
                {
                    if (Button(br, "穿著中　·　卸下", TM.Hex("#E3D6C6"), Ink, 20)) { Accessories.Unequip(a.slot); Accessories.Apply(me); Sfx.Pop(0.9f); }
                }
                else if (Button(br, "穿上", Green, Color.white, 22)) { Accessories.Equip(a.id); Accessories.Apply(me); Sfx.Pop(1.2f); }
            }
            int rows = (Accessories.All.Length + 2) / 3;
            GUI.Label(new Rect(p.x + 40, y0 + rows * (ch + gap) + 4, p.width - 80, 34), "同一個位置只能戴一個，一般印章 1 枚、紫色隱藏印章 3 枚", St(sSmall, 20, Grey, TextAnchor.MiddleLeft, false));
        }

        void DrawShopHidden(Rect p)
        {
            var found = Accessories.HiddenFound();
            float y = p.y + 184;
            foreach (var h in Accessories.HiddenSpots)
            {
                var poi = Planner.Get(h.poiId);
                var area = World.Get(h.area);
                bool got = found.Contains(h.poiId);
                var r = new Rect(p.x + 40, y, p.width - 80, 82);
                Box(r, got ? Color.white : TM.Hex("#EEE8E0"), 18);
                Circle(new Vector2(r.x + 40, r.center.y), 48, got ? TM.Hex("#8C5BE0") : TM.Hex("#C9C1B6"));
                GUI.Label(new Rect(r.x + 16, r.center.y - 24, 48, 48), got ? "★" : "?", St(sName, 24, Color.white, TextAnchor.MiddleCenter, true));
                if (got && poi != null)
                {
                    GUI.Label(new Rect(r.x + 80, r.y + 6, r.width - 100, 38), poi.name, St(sCardName, 25, Ink, TextAnchor.MiddleLeft, true));
                    GUI.Label(new Rect(r.x + 80, r.y + 42, r.width - 100, 32), poi.blurb, St(sSmall, 18, Grey, TextAnchor.MiddleLeft, false));
                }
                else
                {
                    GUI.Label(new Rect(r.x + 80, r.y + 6, r.width - 100, 38), "？？？", St(sCardName, 25, Grey, TextAnchor.MiddleLeft, true));
                    GUI.Label(new Rect(r.x + 80, r.y + 42, r.width - 100, 32), "藏在「" + (area != null ? area.name : h.area) + "」某個角落", St(sSmall, 18, Grey, TextAnchor.MiddleLeft, false));
                }
                y += 92;
            }
        }

        void Label(Rect r, string text, int size = 26, bool bold = true)
        {
            GUI.Label(r, text, St(sSmall, size, Ink, TextAnchor.MiddleLeft, bold));
        }

        void NickRow(float x, float y)
        {
            Label(new Rect(x, y, 140, 60), "暱稱");
            nickField = InputBox(new Rect(x + 150, y, 420, 60), "nick", nickField, 12, "輸入你的暱稱");
        }

        bool Stepper(float x, float y, string text, out int delta)
        {
            delta = 0;
            const float lw = 340;
            if (Button(new Rect(x, y, 64, 60), "－", TM.Hex("#E3D6C6"), Ink, 30)) delta = -1;
            Box(new Rect(x + 74, y, lw, 60), Color.white, 16);
            GUI.Label(new Rect(x + 74, y, lw, 60), text, St(sTitle, 28, Ink, TextAnchor.MiddleCenter, true));
            if (Button(new Rect(x + 84 + lw, y, 64, 60), "＋", TM.Hex("#E3D6C6"), Ink, 30)) delta = 1;
            return delta != 0;
        }

        void BudgetCards(float x, float y, float w)
        {
            float cw = (w - 40) / 3f;
            for (int i = 0; i < GameData.Budgets.Length; i++)
            {
                var b = GameData.Budgets[i];
                var r = new Rect(x + i * (cw + 20), y, cw, 132);
                if (Card(r, Session.budget == b.key, b.color)) { Session.budget = b.key; Sfx.Pop(0.9f + i * 0.1f); }
                Circle(new Vector2(r.x + 30, r.y + 32), 26, b.color);
                GUI.Label(new Rect(r.x + 52, r.y + 12, cw - 60, 40), b.name, St(sCardName, 28, Ink, TextAnchor.MiddleLeft, true));
                GUI.Label(new Rect(r.x + 20, r.y + 52, cw - 30, 30), b.range, St(sSmall, 24, b.color, TextAnchor.MiddleLeft, true));
                GUI.Label(new Rect(r.x + 20, r.y + 84, cw - 30, 40), b.desc, St(sCardText, 20, Grey, TextAnchor.UpperLeft, false, true));
            }
        }

        static readonly string[] WeekDays = { "日", "一", "二", "三", "四", "五", "六" };
        static string FmtDate(System.DateTime d) { return d.ToString("yyyy/MM/dd") + "（" + WeekDays[(int)d.DayOfWeek] + "）"; }

        void DrawCreate()
        {
            var p = PanelRect(1080, 940);
            Shadowed(p, Cream, 36);
            GUI.Label(new Rect(p.x, p.y + 24, p.width, 60), "建立房間", sTitle);
            float x = p.x + 70, y = p.y + 104;
            NickRow(x, y);
            y += 84;

            Label(new Rect(x, y, 140, 64), "目的地");
            for (int i = 0; i < GameData.Destinations.Length; i++)
            {
                var d = GameData.Destinations[i];
                var r = new Rect(x + 150 + i * 260, y, 240, 64);
                if (Card(r, Session.destKey == d.key, Persimmon, d.ready)) { Session.destKey = d.key; Sfx.Pop(); }
                GUI.Label(new Rect(r.x + 18, r.y + 4, r.width - 20, 34), d.name, St(sCardName, 26, d.ready ? Ink : Grey, TextAnchor.MiddleLeft, true));
                GUI.Label(new Rect(r.x + 18, r.y + 34, r.width - 20, 26), d.note, St(sSmall, 18, Grey, TextAnchor.MiddleLeft, false));
            }
            y += 88;

            Label(new Rect(x, y, 140, 60), "出發日");
            int dd;
            if (Stepper(x + 150, y, FmtDate(Session.date), out dd)) Session.date = Session.date.AddDays(dd);
            y += 76;

            Label(new Rect(x, y, 140, 60), "天數");
            if (Stepper(x + 150, y, Session.TripLabel, out dd)) Session.days = Mathf.Clamp(Session.days + dd, 3, 7);
            y += 76;

            Label(new Rect(x, y, 140, 60), "人數");
            if (Stepper(x + 150, y, Session.size + " 人", out dd)) Session.size = Mathf.Clamp(Session.size + dd, 2, 6);
            y += 82;

            Label(new Rect(x, y, 400, 40), "你的預算（每人）");
            BudgetCards(x, y + 46, p.width - 140);
            if (Button(new Rect(p.x + 70, p.yMax - 110, 240, 80), "返回", TM.Hex("#E3D6C6"), Ink)) { screen = Page.Title; GUIUtility.keyboardControl = 0; }
            if (Button(new Rect(p.xMax - 430, p.yMax - 110, 360, 80), "建立房間 ▶", Persimmon, Color.white)) EnterLobby();
        }

        void DrawJoin()
        {
            var p = PanelRect(1080, joinOk ? 820 : 560);
            Shadowed(p, Cream, 36);
            GUI.Label(new Rect(p.x, p.y + 24, p.width, 60), "加入房間", sTitle);
            float x = p.x + 70, y = p.y + 110;
            NickRow(x, y);
            y += 86;
            Label(new Rect(x, y, 140, 60), "房間代碼");
            codeField = InputBox(new Rect(x + 150, y, 420, 60), "code", codeField, 10, "例如 OSK-7Q2F").ToUpperInvariant();
            if (Button(new Rect(x + 600, y, 200, 60), joinOk ? "已加入 ✓" : "加入", joinOk ? Green : TM.Hex("#3E8EDE"), Color.white, 28, !joinOk))
            {
                string c = codeField.Trim().ToUpperInvariant();
                int n = c.Count(ch => char.IsLetterOrDigit(ch));
                if (n < 4) Toast("房間代碼至少要 4 碼，例如 OSK-7Q2F");
                else
                {
                    joinOk = true;
                    Session.roomCode = c;
                    Session.size = 4;
                    Session.days = 5;
                    Session.date = new System.DateTime(2027, 2, 3);
                    Session.destKey = "osaka";
                    GUIUtility.keyboardControl = 0;
                    Sfx.Chime();
                }
            }
            y += 90;
            if (!joinOk)
            {
                GUI.Label(new Rect(x, y, p.width - 140, 40), "跟房主要代碼，輸入後按「加入」。", St(sSmall, 24, Grey, TextAnchor.MiddleLeft, false));
            }
            else
            {
                var info = new Rect(x, y, p.width - 140, 70);
                Box(info, TM.Hex("#FFE7CC"), 18);
                GUI.Label(new Rect(info.x + 24, info.y, info.width - 30, info.height),
                    "房主「" + GameData.MockPool[0].name + "」的房間：" + Session.Dest.name + "・" + Session.TripLabel + "・" + Session.size + " 人・" + FmtDate(Session.date),
                    St(sSmall, 24, Ink, TextAnchor.MiddleLeft, true));
                y += 94;
                Label(new Rect(x, y, 400, 40), "你的預算（每人）");
                BudgetCards(x, y + 46, p.width - 140);
            }
            if (Button(new Rect(p.x + 70, p.yMax - 110, 240, 80), "返回", TM.Hex("#E3D6C6"), Ink)) { screen = Page.Title; GUIUtility.keyboardControl = 0; }
            if (joinOk && Button(new Rect(p.xMax - 430, p.yMax - 110, 360, 80), "進入房間 ▶", TM.Hex("#3E8EDE"), Color.white)) EnterLobby();
        }

        void DrawLobby()
        {
            var p = PanelRect(1080, 820);
            Shadowed(p, Cream, 36);
            GUI.Label(new Rect(p.x, p.y + 24, p.width, 40), "房間代碼", St(sSmall, 24, Grey, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(p.x, p.y + 58, p.width, 90), Session.roomCode, St(sBanner, 72, Persimmon, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(p.x, p.y + 146, p.width, 36), Session.isHost ? "把代碼傳給朋友，他們在「加入房間」輸入就能進來" : "你已經在房間裡了，等房主開始",
                St(sSmall, 22, Grey, TextAnchor.MiddleCenter, false));
            var trip = new Rect(p.x + 70, p.y + 196, p.width - 140, 64);
            Box(trip, TM.Hex("#FFE7CC"), 18);
            GUI.Label(trip, Session.Dest.name + "・" + Session.TripLabel + "・" + FmtDate(Session.date) + "・" + Session.size + " 人・你的預算：" + Session.Budget.name,
                St(sSmall, 24, Ink, TextAnchor.MiddleCenter, true));

            // 成員格子
            var players = Session.players;
            int shown = 0;
            for (int i = 0; i < players.Count; i++)
            {
                var pl = players[i];
                bool here = pl.isUser || (pl.isHost && !Session.isHost) || CountJoined(pl);
                var r = new Rect(p.x + 70 + (i % 2) * 480, p.y + 290 + (i / 2) * 100, 460, 84);
                Shadowed(r, here ? Color.white : TM.Hex("#EEE8E0"), 20);
                if (here)
                {
                    Circle(new Vector2(r.x + 46, r.y + 42), 54, pl.color);
                    GUI.Label(new Rect(r.x + 19, r.y + 15, 54, 54), pl.name.Substring(0, 1), St(sName, 26, Color.white, TextAnchor.MiddleCenter, true));
                    GUI.Label(new Rect(r.x + 90, r.y, 220, r.height), pl.name, St(sCardName, 28, Ink, TextAnchor.MiddleLeft, true));
                    float cx = r.xMax - 20;
                    if (pl.isUser) { cx -= 70; Box(new Rect(cx, r.y + 24, 60, 36), Persimmon, 18); GUI.Label(new Rect(cx, r.y + 24, 60, 36), "你", St(sName, 20, Color.white, TextAnchor.MiddleCenter, true)); }
                    if (pl.isHost) { cx -= 80; Box(new Rect(cx, r.y + 24, 70, 36), Gold, 18); GUI.Label(new Rect(cx, r.y + 24, 70, 36), "房主", St(sName, 20, Ink, TextAnchor.MiddleCenter, true)); }
                    shown++;
                }
                else
                {
                    GUI.Label(new Rect(r.x + 30, r.y, r.width - 40, r.height), "等待加入⋯", St(sSmall, 24, Grey, TextAnchor.MiddleLeft, false));
                }
            }

            bool all = lobbyJoined >= players.Count - 1;
            if (Session.isHost)
            {
                if (Button(new Rect(p.center.x - 200, p.yMax - 110, 400, 84), all ? "開始旅程 ▶" : "等大家到齊⋯", Persimmon, Color.white, 30, all)) menuDone = true;
            }
            else
            {
                GUI.Label(new Rect(p.x, p.yMax - 110, p.width, 84), all ? "房主準備出發了！" : "等其他人加入⋯", St(sTitle, 30, Ink, TextAnchor.MiddleCenter, true));
            }
        }

        bool CountJoined(Player pl)
        {
            // 依加入順序：沒被算進來的電腦隊友顯示「等待加入」
            int idx = 0;
            foreach (var x in Session.players)
            {
                if (x.isUser || (x.isHost && !Session.isHost)) continue;
                if (x == pl) return idx < lobbyJoined - (Session.isHost ? 0 : 1);
                idx++;
            }
            return false;
        }

        void DrawQuiz()
        {
            var qs = GameData.Quiz;
            int qi = Mathf.Min(quizIdx, qs.Length - 1);
            var q = qs[qi];
            float k = EaseOutBack((Time.time - quizBorn) / 0.3f);
            var p = PanelRect(1040, 520, 50);
            Shadowed(p, Cream, 36);
            for (int i = 0; i < qs.Length; i++)
                Box(new Rect(p.x + 60 + i * 110, p.y + 34, 96, 14), i < quizIdx ? Green : i == quizIdx ? Persimmon : TM.Hex("#E3D6C6"), 7);
            GUI.Label(new Rect(p.x + 60 + qs.Length * 110 + 10, p.y + 20, 400, 40), "旅遊人格快測　" + (qi + 1) + "／" + qs.Length, St(sSmall, 22, Grey, TextAnchor.MiddleLeft, true));
            GUI.Label(ScaleRect(new Rect(p.x + 40, p.y + 76, p.width - 80, 100), k), q.ask, St(sTitle, 44, Ink, TextAnchor.MiddleCenter, true, true));
            for (int i = 0; i < 2; i++)
            {
                var o = i == 0 ? q.a : q.b;
                var r = new Rect(p.x + 50 + i * 480, p.y + 200, 460, 210);
                if (Card(r, false, Persimmon)) PickQuiz(i == 0);
                Circle(new Vector2(r.x + 50, r.y + 50), 56, i == 0 ? Persimmon : TM.Hex("#3E8EDE"));
                GUI.Label(new Rect(r.x + 22, r.y + 22, 56, 56), (i + 1).ToString(), St(sName, 28, Color.white, TextAnchor.MiddleCenter, true));
                GUI.Label(new Rect(r.x + 30, r.y + 92, r.width - 50, 50), o.label, St(sCardName, 30, Ink, TextAnchor.UpperLeft, true, true));
                GUI.Label(new Rect(r.x + 30, r.y + 144, r.width - 50, 50), o.sub, St(sCardText, 23, Grey, TextAnchor.UpperLeft, false, true));
            }
            GUI.Label(new Rect(p.x, p.yMax - 56, p.width, 40), "點選或按 1／2", St(sSmall, 20, Grey, TextAnchor.MiddleCenter, false));
        }

        void DrawQuizReveal()
        {
            var d = GameData.P(revealP);
            float k = EaseOutBack((Time.time - revealBorn) / 0.4f);
            var p = ScaleRect(PanelRect(980, 560, 40), k);
            Shadowed(p, Cream, 36);
            if (k < 0.6f) return;
            Box(new Rect(p.x, p.y, p.width, 16), d.color, 8);
            GUI.Label(new Rect(p.x, p.y + 36, p.width, 40), "你的旅遊人格是", St(sSmall, 26, Grey, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(p.x, p.y + 80, p.width, 96), d.name, St(sBanner, 72, d.color, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(p.x, p.y + 176, p.width, 44), d.title, St(sTitle, 32, Ink, TextAnchor.MiddleCenter, true));
            var qs = new GUIStyle(St(sBody, 28, Grey, TextAnchor.MiddleCenter, false, true));
            qs.fontStyle = FontStyle.Italic;
            GUI.Label(new Rect(p.x + 60, p.y + 228, p.width - 120, 56), "「" + d.quote + "」", qs);
            float gx = p.center.x - (d.gear.Length * 220) * 0.5f;
            for (int i = 0; i < d.gear.Length; i++)
            {
                var g = new Rect(gx + i * 220, p.y + 296, 200, 46);
                Box(g, d.soft, 23);
                GUI.Label(g, d.gear[i], St(sName, 22, d.color, TextAnchor.MiddleCenter, true));
            }
            if (Button(new Rect(p.center.x - 180, p.yMax - 96, 360, 76), "出發！▶", Persimmon, Color.white, 32)) revealGo = true;
        }
    }
}

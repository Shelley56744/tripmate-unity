using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 10 站旅程的畫面：站間轉場、視覺小說式答題（旁白 → 趣趣發問 → 隊友搭話 → A／B／C／自己說一句 → 揭曉）、
    /// 大提示（誰跟你一樣、差最多的兩個人、共識技／衝突技、默契值）、第 1 站後的網路選擇、USJ 的卡頓事件。
    /// </summary>
    public partial class DialogueUI
    {
        // 轉場
        int transRound;
        float transBorn, transAlpha;
        // 答題
        int curRound;
        int chosen = -1;      // 0..2 = A..C，3 = 自己說一句
        string customField = "";
        bool customSend, customBack;
        RoundResult curResult;
        Hint curHint;
        int vibeBefore, vibeAfter;
        float hintBorn;
        bool hintGo;
        // 網路
        int netPick = -1;
        // 卡頓
        bool lagEsim;
        float lagBorn;

        static readonly string[] Letters = { "A", "B", "C", "D" };

        // ---------------- 站間轉場 ----------------
        public IEnumerator TransitionCo(int r, Action mid)
        {
            Busy = true;
            if (player) player.locked = true;
            transRound = r;
            transBorn = Time.time;
            screen = Page.Transition;
            Sfx.Chime();
            for (float t = 0; t < 0.45f; t += Time.deltaTime) { transAlpha = t / 0.45f; yield return null; }
            transAlpha = 1;
            if (mid != null) mid();
            yield return new WaitForSeconds(2.0f);
            for (float t = 0; t < 0.45f; t += Time.deltaTime) { transAlpha = 1 - t / 0.45f; yield return null; }
            transAlpha = 0;
            screen = Page.None;
            if (player) player.locked = false;
            Busy = false;
        }

        void DrawTransition()
        {
            var old = GUI.color;
            GUI.color = new Color(1, 1, 1, transAlpha);
            Box(new Rect(-10, -10, W + 20, H + 20), TM.Hex("#1F2350"), 0);
            var R = GameData.Rounds[transRound];
            var area = World.Get(R.area);
            float k = EaseOutBack((Time.time - transBorn - 0.3f) / 0.4f);
            var c = ScaleRect(new Rect(W * 0.5f - 460, 250, 920, 520), Mathf.Max(0.01f, k));
            Shadowed(c, Cream, 36);
            if (k > 0.6f)
            {
                GUI.Label(new Rect(c.x, c.y + 34, c.width, 40), "第 " + (transRound + 1) + " 站／" + GameData.Rounds.Length + "　" + R.clock, St(sSmall, 28, Persimmon, TextAnchor.MiddleCenter, true));
                GUI.Label(new Rect(c.x, c.y + 86, c.width, 90), R.title, St(sBanner, 64, Ink, TextAnchor.MiddleCenter, true));
                GUI.Label(new Rect(c.x, c.y + 176, c.width, 44), R.subtitle, St(sTitle, 30, Grey, TextAnchor.MiddleCenter, true));
                if (area != null) GUI.Label(new Rect(c.x, c.y + 226, c.width, 40), "地點：" + area.name, St(sSmall, 26, Ink, TextAnchor.MiddleCenter, true));
                // 路線進度
                float x0 = c.center.x - (GameData.Rounds.Length - 1) * 38f;
                Box(new Rect(x0, c.y + 330, (GameData.Rounds.Length - 1) * 76f, 6), TM.Hex("#E3D6C6"), 3);
                for (int i = 0; i < GameData.Rounds.Length; i++)
                {
                    var pc = new Vector2(x0 + i * 76f, c.y + 333);
                    Color col = i < Session.history.Count ? (Session.history[i].consensus ? Green : Grey) : i == transRound ? Persimmon : TM.Hex("#E3D6C6");
                    Circle(pc, i == transRound ? 40 : 28, col);
                    GUI.Label(new Rect(pc.x - 20, pc.y - 20, 40, 40), (i + 1).ToString(), St(sName, i == transRound ? 22 : 17, i < Session.history.Count || i == transRound ? Color.white : Grey, TextAnchor.MiddleCenter, true));
                }
                GUI.Label(new Rect(c.x, c.y + 380, c.width, 40), "已收集共識 " + Session.Consensus + " 站　·　綠色＝共識達成", St(sSmall, 22, Grey, TextAnchor.MiddleCenter, false));
                GUI.Label(new Rect(c.x, c.y + 430, c.width, 40), "到了之後自由逛逛，準備好就走到發光的站點", St(sSmall, 22, Ink, TextAnchor.MiddleCenter, true));
            }
            GUI.color = old;
        }

        // ---------------- 答題 ----------------
        public IEnumerator RoundCo(int r, Ququ guide)
        {
            Busy = true;
            curRound = r;
            var R = GameData.Rounds[r];
            player.locked = true;
            if (cam) cam.close = true;

            // 隊友在玩家「鏡頭對面」圍成半圓
            Vector3 center = player.transform.position;
            Vector3 fwd = cam ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
            for (int i = 0; i < teammates.Count; i++)
            {
                float a = teammates.Count == 1 ? 0f : -70f + i * (140f / (teammates.Count - 1));
                teammates[i].gatherPoint = center + Quaternion.AngleAxis(a, Vector3.up) * fwd * 2.7f;
            }
            player.transform.rotation = Quaternion.LookRotation(-fwd);
            Sfx.Chime();
            Banner(R.title, R.clock + "　·　" + R.subtitle, Orange, 1.8f);
            yield return new WaitForSeconds(2.0f);

            // 隊友各自碎念一句（頭上的小氣泡，不用一句句按）
            if (guide) StartCoroutine(HopVisual(guide));
            var mates = Session.Mates.ToList();
            ClearBubbles();
            if (mates.Count > 0)
            {
                var talkers = new List<Player> { mates[r % mates.Count] };
                if (mates.Count > 1) talkers.Add(mates[(r + 1) % mates.Count]);
                foreach (var t in talkers)
                {
                    if (t.mate == null) continue;
                    StartCoroutine(t.mate.Hop(1));
                    AddBubble(t.mate.transform, GameData.P(t.type).chatter[r], GameData.P(t.type).color, true);
                    Sfx.Blip(1.1f);
                    yield return new WaitForSeconds(0.5f);
                }
                yield return new WaitForSeconds(2.2f);
            }
            ClearBubbles();

            // 作答：直接點一個就算（不用再確認）
            Answer mine = null;
            while (mine == null)
            {
                chosen = -1;
                screen = Page.Choices;
                dialogOn = false;
                while (chosen < 0)
                {
                    if (Key("1")) chosen = 0;
                    else if (Key("2")) chosen = 1;
                    else if (Key("3")) chosen = 2;
                    else if (Key("4")) chosen = 3;
                    yield return null;
                }
                Sfx.Pop(0.9f + chosen * 0.1f);
                yield return null;
                if (chosen < 3) mine = Scoring.MakeAnswer(Session.Me, Letters[chosen], "");
                else
                {
                    customField = "";
                    customSend = customBack = false;
                    screen = Page.Custom;
                    while (!customSend && !customBack) yield return null;
                    GUIUtility.keyboardControl = 0;
                    if (customSend) mine = Scoring.MakeAnswer(Session.Me, "D", customField.Trim());
                }
            }
            screen = Page.None;
            yield return null;

            // USJ：要靠網路搶整理券的選項
            bool lagged = false, won = false;
            if (r == 1 && mine.choice != "D" && R.options.First(o => o.letter == mine.choice).needNet)
            {
                lagEsim = Session.network == "esim";
                lagBorn = Time.time;
                screen = Page.Lag;
                yield return new WaitForSeconds(lagEsim ? 1.8f : 3.6f);
                screen = Page.None;
                lagged = !lagEsim;
                won = lagEsim;
                if (!lagEsim) Esim.Spend(0.1f);
            }

            // 隊友陸續選好 → 一起揭曉
            var res = new RoundResult { round = r };
            foreach (var p in Session.players)
                res.answers.Add(p.isUser ? mine : Scoring.PickMockAnswer(p, r));
            foreach (var p in Session.Mates)
            {
                yield return new WaitForSeconds(0.3f);
                if (p.mate) AddBubble(p.mate.transform, "✓", Grey, true);
                Sfx.Blip(1.4f);
            }
            yield return new WaitForSeconds(0.5f);
            ClearBubbles();
            foreach (var a in res.answers)
            {
                var t = a.player.isUser ? player.transform : a.player.mate ? a.player.mate.transform : null;
                if (t == null) continue;
                int li = Array.IndexOf(Letters, a.choice);
                AddBubble(t, a.choice == "D" ? "說" : a.choice, Letter[Mathf.Max(0, li)]);
                Sfx.Pop(0.9f + li * 0.12f);
                yield return new WaitForSeconds(0.3f);
            }
            yield return new WaitForSeconds(0.8f);

            // 計算：共識、默契、大提示
            vibeBefore = Scoring.LiveVibe();
            Scoring.Consensus(res);
            Session.history.Add(res);
            vibeAfter = Scoring.LiveVibe();
            curResult = res;
            curHint = Scoring.BuildHint(res, lagged, won);

            // 招牌動作
            foreach (var d in curHint.duels) StartCoroutine(PlayDuel(d));
            if (res.consensus) { Sfx.Fanfare(); Fx.Confetti(center + Vector3.up * 2f, 90); }
            else Sfx.Buzz();

            hintGo = false;
            hintBorn = Time.time;
            screen = Page.HintCard;
            yield return new WaitForSeconds(0.8f);
            while (!hintGo && !Key("space")) yield return null;
            screen = Page.None;
            ClearBubbles();

            if (!res.consensus) yield return Say("趣趣", "折衷一下：" + R.advice);
            dialogOn = false;

            foreach (var t in teammates) t.gatherPoint = null;
            if (cam) cam.close = false;
            player.locked = false;
            Busy = false;
        }

        IEnumerator HopVisual(Ququ q)
        {
            for (float t = 0; t < 1f; t += Time.deltaTime * 4f)
            {
                q.visual.localPosition = Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.3f;
                yield return null;
            }
            q.visual.localPosition = Vector3.zero;
        }

        IEnumerator PlayDuel(Duel d)
        {
            if (!d.agree) yield return new WaitForSeconds(1.2f);
            foreach (var p in new[] { d.a, d.b })
            {
                Transform t = p.isUser ? player.transform : p.mate ? p.mate.transform : null;
                if (t == null) continue;
                if (d.agree)
                {
                    if (p.isUser) StartCoroutine(player.Hop(2)); else StartCoroutine(p.mate.Hop(2));
                    Fx.Sparkle(t.position + Vector3.up * 1.3f, 16);
                }
                else
                {
                    if (!p.isUser) StartCoroutine(p.mate.Shake());
                    Fx.Dust(t.position + Vector3.up * 0.2f, 10);
                }
            }
        }

        /// <summary>題目＋直式選項：一行一個，字母、短標題、一句說明。</summary>
        void DrawChoices()
        {
            var R = GameData.Rounds[curRound];
            float lw = Mathf.Min(980, W - 80), rowH = 86, gap = 12;
            float listH = 4 * rowH + 3 * gap;
            float x = W * 0.5f - lw * 0.5f, y0 = H - 36 - listH;

            // 題目
            var q = new Rect(x, y0 - 24 - 104, lw, 104);
            Shadowed(q, Cream, 28);
            var tag = new Rect(q.x + 30, q.y - 22, 110, 44);
            Box(tag, Orange, 22);
            GUI.Label(tag, "趣趣", St(sName, 24, Color.white, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(q.x + 40, q.y + 10, q.width - 80, q.height - 10), string.IsNullOrEmpty(R.ask) ? R.line : R.ask, St(sTitle, 38, Ink, TextAnchor.MiddleLeft, true));

            for (int i = 0; i < 4; i++)
            {
                var r = new Rect(x, y0 + i * (rowH + gap), lw, rowH);
                if (Card(r, false, Letter[i])) chosen = i;
                Circle(new Vector2(r.x + 48, r.center.y), 56, Letter[i]);
                GUI.Label(new Rect(r.x + 20, r.center.y - 28, 56, 56), Letters[i], St(sName, 28, Color.white, TextAnchor.MiddleCenter, true));
                string name = i < 3 ? R.options[i].shortName : "自己說一句";
                string hint = i < 3 ? R.options[i].hint : "用自己的話回答";
                var ns = St(sCardName, 32, Ink, TextAnchor.MiddleLeft, true);
                float nw = ns.CalcSize(new GUIContent(name)).x;
                GUI.Label(new Rect(r.x + 96, r.y, nw + 10, r.height), name, ns);
                GUI.Label(new Rect(r.x + 96 + nw + 28, r.y, r.width - nw - 200, r.height), hint ?? "", St(sSmall, 25, TM.Hex("#7A7E98"), TextAnchor.MiddleLeft, false));
                var kr = new Rect(r.xMax - 60, r.center.y - 20, 40, 40);
                Box(kr, TM.Hex("#EFE8DE"), 10);
                GUI.Label(kr, (i + 1).ToString(), St(sKey, 22, Grey, TextAnchor.MiddleCenter, true));
            }
        }

        void DrawCustom()
        {
            float dw = Mathf.Min(1000, W - 80);
            var d = new Rect((W - dw) * 0.5f, H - 40 - 260, dw, 260);
            Shadowed(d, Cream, 28);
            GUI.Label(new Rect(d.x + 40, d.y + 22, d.width - 80, 44), "你會怎麼做？", St(sCardName, 32, Ink, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(d.x + 260, d.y + 22, d.width - 300, 44), "例如：先拍照再找地方坐", St(sSmall, 24, TM.Hex("#9A948C"), TextAnchor.MiddleLeft, false));
            customField = InputBox(new Rect(d.x + 40, d.y + 80, d.width - 80, 70), "custom", customField, 40, "");
            if (GUI.GetNameOfFocusedControl() != "custom" && string.IsNullOrEmpty(customField)) GUI.FocusControl("custom");
            bool ok = customField.Trim().Length > 0;
            if (Button(new Rect(d.xMax - 360, d.yMax - 86, 320, 66), "送出 ▶", Letter[3], Color.white, 28, ok)) customSend = true;
            if (Button(new Rect(d.xMax - 560, d.yMax - 86, 180, 66), "返回", TM.Hex("#E3D6C6"), Ink)) customBack = true;
            var e = Event.current;
            if (ok && e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && !ImeBusy()) customSend = true;
        }

        static bool ImeBusy()
        {
            try { return Input.imeIsSelected && !string.IsNullOrEmpty(Input.compositionString); } catch (Exception) { return false; }
        }

        /// <summary>揭曉卡：大字的「共識達成／沒達成」、幾人一致、默契變化、最多兩句重點、一組招牌技。</summary>
        void DrawHintCard()
        {
            var res = curResult;
            var h = curHint;
            if (res == null || h == null) return;
            float k = EaseOutBack((Time.time - hintBorn) / 0.35f);
            var rows = h.rows.Take(2).ToList();
            var duel = h.duels.FirstOrDefault(d => d.agree) ?? h.duels.FirstOrDefault();
            float ch = 300 + rows.Count * 66 + (duel != null ? 76 : 0);
            var c = ScaleRect(new Rect(W * 0.5f - 500, 110, 1000, ch), k);
            Shadowed(c, Cream, 36);
            if (k < 0.6f) return;

            int n = res.answers.Count;
            // 大標
            var badge = new Rect(c.x + 40, c.y + 36, 110, 110);
            Circle(badge.center, 110, res.consensus ? Green : TM.Hex("#C9C1B6"));
            GUI.Label(badge, res.consensus ? "✓" : "×", St(sBanner, 60, Color.white, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(c.x + 175, c.y + 36, 480, 64), res.consensus ? "共識達成！" : "這站沒共識", St(sBanner, 50, Ink, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(c.x + 178, c.y + 100, 480, 44), res.topCount + "／" + n + " 人一致", St(sSmall, 30, Grey, TextAnchor.MiddleLeft, true));
            var vr = new Rect(c.xMax - 300, c.y + 58, 260, 66);
            Box(vr, TM.Hex("#FFE0EE"), 33);
            string vt = vibeBefore < 0 ? "默契 " + vibeAfter : "默契 " + vibeBefore + " → " + vibeAfter;
            GUI.Label(vr, vt, St(sName, 28, TM.Hex("#C93C7E"), TextAnchor.MiddleCenter, true));

            float y = c.y + 176;
            foreach (var row in rows)
            {
                Color col = row.tone == "ok" ? Green : row.tone == "luxe" ? Gold : TM.Hex("#FF8A6B");
                Circle(new Vector2(c.x + 60, y + 26), 24, col);
                GUI.Label(new Rect(c.x + 90, y, c.width - 130, 52), row.text, St(sCardName, 30, Ink, TextAnchor.MiddleLeft, true));
                y += 66;
            }
            if (duel != null)
            {
                var actor = GameData.P(duel.a.Persona);
                string move = duel.agree ? actor.agree : actor.conflict;
                var r = new Rect(c.x + 40, y + 4, c.width - 80, 60);
                Box(r, duel.agree ? TM.Hex("#DDF5EA") : TM.Hex("#FFE1D3"), 30);
                string who = (duel.a.isUser ? "你" : duel.a.name) + " × " + (duel.b.isUser ? "你" : duel.b.name);
                GUI.Label(new Rect(r.x + 24, r.y, r.width - 40, r.height), (duel.agree ? "合拍　" : "衝突　") + who + "・" + move,
                    St(sCardName, 28, duel.agree ? TM.Hex("#1F7A55") : TM.Hex("#B8440E"), TextAnchor.MiddleLeft, true));
            }
            if (Button(new Rect(c.center.x - 160, c.yMax - 92, 320, 70), "繼續 ▶", Persimmon, Color.white, 30)) hintGo = true;
        }

        // ---------------- 第 1 站後：網路 ----------------
        public IEnumerator NetworkCo()
        {
            Busy = true;
            player.locked = true;
            if (Session.network == "esim")
            {
                yield return Say("趣趣", "你已經開通去趣 eSIM 了，等一下 USJ 人再多都不怕，出發吧～");
                dialogOn = false;
                player.locked = false;
                Busy = false;
                yield break;
            }
            yield return Say("趣趣", "出門前先確認一件事：大家在日本要用什麼上網？等一下 USJ 人超多，網路很重要喔！", true);
            dialogOn = false;
            netPick = -1;
            screen = Page.Network;
            while (netPick < 0)
            {
                if (Key("1")) netPick = 0; else if (Key("2")) netPick = 1; else if (Key("3")) netPick = 2;
                yield return null;
            }
            Session.network = GameData.Networks[netPick].key;
            screen = Page.None;
            Sfx.Pop();
            if (Session.network == "esim") { Session.triedEsimInGame = true; yield return Say("趣趣", "掃一下 QR 就開通了，走到哪都順！出發吧～"); }
            else yield return Say("趣趣", "好～如果路上卡住了，左上角或飯店前的攤位隨時可以開通去趣 eSIM！");
            dialogOn = false;
            player.locked = false;
            Busy = false;
        }

        void DrawNetwork()
        {
            var p = PanelRect(1080, 520, 120);
            Shadowed(p, Cream, 36);
            GUI.Label(new Rect(p.x, p.y + 26, p.width, 60), "在日本要怎麼上網？", sTitle);
            GUI.Label(new Rect(p.x, p.y + 84, p.width, 40), "會影響之後的導航、小地圖、搶票和上傳照片", St(sSmall, 22, Grey, TextAnchor.MiddleCenter, false));
            float cw = 310;
            for (int i = 0; i < GameData.Networks.Length; i++)
            {
                var n = GameData.Networks[i];
                var r = new Rect(p.x + 50 + i * (cw + 25), p.y + 150, cw, 300);
                if (Card(r, false, Persimmon)) netPick = i;
                if (!string.IsNullOrEmpty(n.tag))
                {
                    var t = new Rect(r.xMax - 100, r.y + 16, 84, 36);
                    Box(t, Persimmon, 18);
                    GUI.Label(t, n.tag, St(sName, 20, Color.white, TextAnchor.MiddleCenter, true));
                }
                GUI.Label(new Rect(r.x + 24, r.y + 60, cw - 40, 80), n.name, St(sCardName, 28, Ink, TextAnchor.UpperLeft, true, true));
                GUI.Label(new Rect(r.x + 24, r.y + 140, cw - 40, 40), n.price > 0 ? "NT$" + n.price + "／天" : "免費", St(sSmall, 26, Persimmon, TextAnchor.MiddleLeft, true));
                GUI.Label(new Rect(r.x + 24, r.y + 186, cw - 40, 90), n.desc, St(sCardText, 22, Grey, TextAnchor.UpperLeft, false, true));
                GUI.Label(new Rect(r.xMax - 40, r.yMax - 40, 30, 30), (i + 1).ToString(), St(sKey, 20, Grey, TextAnchor.MiddleCenter, true));
            }
        }

        void DrawLag()
        {
            var p = PanelRect(760, 360, 220);
            Shadowed(p, Cream, 36);
            float t = Time.time - lagBorn;
            GUI.Label(new Rect(p.x, p.y + 30, p.width, 50), "搶整理券中⋯", sTitle);
            // 訊號格
            int bars = lagEsim ? 4 : (t < 0.8f ? 3 : t < 1.6f ? 2 : t < 2.4f ? 1 : 0);
            for (int i = 0; i < 4; i++)
            {
                float bh = 24 + i * 22;
                Box(new Rect(p.center.x - 90 + i * 48, p.y + 210 - bh, 34, bh), i < bars ? (lagEsim ? Green : Persimmon) : TM.Hex("#E3D6C6"), 6);
            }
            string msg;
            if (lagEsim) msg = t < 0.9f ? "連線中⋯" : "2 秒就搶到了！";
            else msg = t < 1.2f ? "人好多⋯訊號變弱了" : t < 2.6f ? "轉圈圈⋯⋯" : "卡住了，整理券被搶完⋯";
            GUI.Label(new Rect(p.x, p.y + 240, p.width, 50), msg, St(sTitle, 32, lagEsim ? TM.Hex("#1F7A55") : TM.Hex("#B8440E"), TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(p.x, p.y + 292, p.width, 40), "你的網路：" + (GameData.Networks.FirstOrDefault(n => n.key == Session.network) ?? GameData.Networks[2]).name,
                St(sSmall, 22, Grey, TextAnchor.MiddleCenter, false));
        }
    }
}

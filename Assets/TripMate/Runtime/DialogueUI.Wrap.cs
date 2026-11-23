using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 旅程收尾：人格結算 → 微調行程（拖曳排序、趣趣替換建議、隊友許願）→ 去趣 eSIM 網路方案 → 結局。
    /// </summary>
    public partial class DialogueUI
    {
        TeamAnalysis team;
        bool summaryGo;
        float summaryBorn;

        List<PlanItem> board = new List<PlanItem>();
        Dictionary<string, float> planW;
        int selDay = -1;
        string selSlot = "";
        bool planDone;

        class Wish { public Player who; public Poi poi; public bool placed; public string msg; }   // who = null 是隱藏景點
        int wishTab;   // 0 隊友想去，1 隱藏景點
        readonly List<Wish> wishes = new List<Wish>();

        // 拖曳
        class Cell { public int day; public string slot; public Rect r; }
        readonly List<Cell> cells = new List<Cell>();
        readonly Dictionary<Wish, Rect> wishRects = new Dictionary<Wish, Rect>();
        PlanItem dragItem;
        Wish dragWish;
        Vector2 dragStart;
        bool dragging;
        Rect trashRect;

        bool endRestart;
        bool endGo;
        float endBorn;

        // ---------------- 結算 ----------------
        public IEnumerator SummaryCo(TeamAnalysis t)
        {
            Busy = true;
            player.locked = true;
            team = t;
            yield return Say("趣趣", "10 站都走完了！來看看大家的旅遊人格～");
            dialogOn = false;
            summaryGo = false;
            summaryBorn = Time.time;
            screen = Page.Summary;
            Sfx.Fanfare();
            yield return new WaitForSeconds(0.6f);
            while (!summaryGo && !Key("space")) yield return null;
            screen = Page.None;
        }

        void DrawSummary()
        {
            if (team == null) return;
            float k = EaseOutBack((Time.time - summaryBorn) / 0.4f);
            var me = team.members.First(m => m.player.isUser);
            var d = GameData.P(me.persona);
            float x0 = W * 0.5f - 700;

            // 左：你的人格
            var L = ScaleRect(new Rect(x0, 50, 640, 830), k);
            Shadowed(L, Cream, 32);
            if (k < 0.6f) return;
            Box(new Rect(L.x, L.y, L.width, 190), d.color, 32);
            Box(new Rect(L.x, L.y + 150, L.width, 40), d.color, 0);
            GUI.Label(new Rect(L.x, L.y + 22, L.width, 40), "你是", St(sSmall, 30, Color.white, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(L.x, L.y + 66, L.width, 100), d.name, St(sBanner, 76, Color.white, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(L.x + 30, L.y + 214, L.width - 60, 56), d.title, St(sTitle, 38, Ink, TextAnchor.MiddleCenter, true));
            var qs = new GUIStyle(St(sBody, 30, Grey, TextAnchor.UpperCenter, false, true));
            qs.fontStyle = FontStyle.Italic;
            GUI.Label(new Rect(L.x + 40, L.y + 286, L.width - 80, 90), "「" + d.quote + "」", qs);

            float y = L.y + 410;
            foreach (var mx in me.mix.Take(3))
            {
                var pd = GameData.P(mx.Key);
                GUI.Label(new Rect(L.x + 50, y, L.width - 220, 40), pd.name, St(sCardName, 30, Ink, TextAnchor.MiddleLeft, true));
                GUI.Label(new Rect(L.xMax - 170, y, 120, 40), mx.Value + "%", St(sCardName, 30, pd.color, TextAnchor.MiddleRight, true));
                Box(new Rect(L.x + 50, y + 46, L.width - 100, 20), TM.Hex("#EEE8E0"), 10);
                Box(new Rect(L.x + 50, y + 46, Mathf.Max(20, (L.width - 100) * mx.Value / 100f), 20), pd.color, 10);
                y += 96;
            }
            if (Session.SeedPersona != me.persona)
                GUI.Label(new Rect(L.x, L.yMax - 70, L.width, 40), "快測是「" + GameData.P(Session.SeedPersona).shortName + "」，走完 10 站變成了「" + d.shortName + "」", St(sSmall, 24, Grey, TextAnchor.MiddleCenter, true));

            // 右：團隊
            var Rr = ScaleRect(new Rect(x0 + 670, 50, 730, 830), k);
            Shadowed(Rr, Cream, 32);
            GUI.Label(new Rect(Rr.x + 44, Rr.y + 30, 300, 40), "團隊默契", St(sSmall, 30, Grey, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(Rr.x + 40, Rr.y + 66, 260, 140), team.vibe.ToString(), St(sBanner, 130, TM.Hex("#C93C7E"), TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(Rr.x + 300, Rr.y + 100, Rr.width - 340, 70), team.vibeLabel, St(sTitle, 44, Ink, TextAnchor.MiddleLeft, true));

            var cb = new Rect(Rr.x + 40, Rr.y + 230, Rr.width - 80, 76);
            Box(cb, TM.Hex("#DDF5EA"), 38);
            GUI.Label(cb, "共識 " + Session.Consensus + "／" + GameData.Rounds.Length + " 站", St(sName, 34, TM.Hex("#1F7A55"), TextAnchor.MiddleCenter, true));

            y = Rr.y + 340;
            if (team.bestPair != null) { PairRow(Rr, y, "最合拍", team.bestPair, Green); y += 120; }
            if (team.splitPair != null) { PairRow(Rr, y, "可以分頭玩", team.splitPair, TM.Hex("#FF8A6B")); y += 120; }

            var top = team.prefs.Take(3).Select(p => Dims.Label[p.Key]).ToArray();
            if (top.Length > 0)
            {
                GUI.Label(new Rect(Rr.x + 44, y + 6, 300, 40), "全隊最在意", St(sSmall, 28, Grey, TextAnchor.MiddleLeft, true));
                float px = Rr.x + 44;
                foreach (var t in top)
                {
                    var ps = St(sName, 30, Color.white, TextAnchor.MiddleCenter, true);
                    float tw = ps.CalcSize(new GUIContent(t)).x + 50;
                    Box(new Rect(px, y + 56, tw, 58), Persimmon, 29);
                    GUI.Label(new Rect(px, y + 56, tw, 58), t, ps);
                    px += tw + 14;
                }
            }

            if (Button(new Rect(W * 0.5f - 220, H - 160, 440, 90), "一起來排行程 ▶", Persimmon, Color.white, 34)) summaryGo = true;
        }

        void PairRow(Rect R, float y, string label, PairInfo pair, Color col)
        {
            var r = new Rect(R.x + 40, y, R.width - 80, 100);
            Box(r, Color.white, 24);
            Box(new Rect(r.x, r.y, 12, r.height), col, 6);
            GUI.Label(new Rect(r.x + 32, r.y, 220, r.height), label, St(sSmall, 28, Grey, TextAnchor.MiddleLeft, true));
            string a = pair.a.player.isUser ? "你" : pair.a.player.name;
            string b = pair.b.player.isUser ? "你" : pair.b.player.name;
            GUI.Label(new Rect(r.x + 230, r.y, r.width - 250, r.height), a + " × " + b, St(sTitle, 38, Ink, TextAnchor.MiddleLeft, true));
        }

        // ---------------- 微調行程 ----------------
        public IEnumerator PlanCo(TeamAnalysis t)
        {
            Busy = true;
            player.locked = true;
            if (cam) cam.showcase = true;
            planW = Planner.TeamWeights(t);
            board = Planner.Seed(Session.days, planW);
            selDay = -1;
            dragItem = null; dragWish = null; dragging = false;
            wishes.Clear();
            var taken = new HashSet<string>();
            foreach (var p in Session.Mates)
            {
                var poi = Planner.WishFor(p.type, board, taken);
                if (poi == null) continue;
                taken.Add(poi.id);
                wishes.Add(new Wish { who = p, poi = poi });
            }
            // 旅途中解鎖的隱藏景點（who = null）
            foreach (var hid in Accessories.HiddenFound())
            {
                var hp = Planner.Get(hid);
                if (hp != null && !board.Any(i => i.poiId == hid)) wishes.Add(new Wish { who = null, poi = hp });
            }
            wishTab = 0;
            yield return Say("趣趣", "我照大家這 10 站的選擇，先排好一版了！格子可以直接拖來拖去換順序～");
            dialogOn = false;
            planDone = false;
            screen = Page.Plan;
            while (!planDone) yield return null;
            screen = Page.None;
            Session.lockedAt = DateTime.Now;
            Session.lockedSpots = board.Count;
            Sfx.Fanfare();
            Fx.Confetti(player.transform.position + Vector3.up * 2f, 120);
            yield return EsimPlanCo();
            if (cam) cam.showcase = false;
        }

        Rect BoardRect() { return new Rect(W * 0.5f - 720, 160, 940, 820); }

        void DrawPlan()
        {
            HandlePlanDrag();

            var head = new Rect(W * 0.5f - 720, 24, 1440, 116);
            Shadowed(head, Cream, 28);
            GUI.Label(new Rect(head.x + 36, head.y + 12, head.width - 60, 54), "微調行程｜" + Session.Dest.name + " " + Session.TripLabel, St(sTitle, 38, Ink, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(head.x + 36, head.y + 64, head.width - 60, 40), "拖曳格子換順序　·　點一下看建議　·　拖到垃圾桶移出", St(sSmall, 25, Grey, TextAnchor.MiddleLeft, true));

            var issues = Planner.Issues(board, planW);
            var B = BoardRect();
            Shadowed(B, new Color(1f, 0.97f, 0.93f, 0.97f), 28);
            var days = Planner.Days(Session.days);
            float colW = (B.width - 40) / days.Count;
            float gridTop = B.y + 82, gridH = B.height - 82 - 120;
            float rowH = gridH / 3f;
            var mouse = Event.current.mousePosition;
            bool repaint = Event.current.type == EventType.Repaint;
            if (repaint) cells.Clear();
            for (int i = 0; i < days.Count; i++)
            {
                var d = days[i];
                float cx = B.x + 20 + i * colW;
                GUI.Label(new Rect(cx, B.y + 14, colW, 36), d.label, St(sCardName, 27, Ink, TextAnchor.MiddleCenter, true));
                GUI.Label(new Rect(cx, B.y + 48, colW, 26), d.note, St(sSmall, 19, Grey, TextAnchor.MiddleCenter, false));
                var all = Planner.AllDayOf(board, d.day);
                if (all != null)
                {
                    var ar = new Rect(cx + 6, gridTop, colW - 12, rowH * 3 - 8);
                    if (repaint) cells.Add(new Cell { day = d.day, slot = Planner.AllDaySlot, r = ar });
                    DropGlow(ar, mouse);
                    PlanCell(ar, all, issues);
                    continue;
                }
                for (int s = 0; s < 3; s++)
                {
                    string slot = Planner.Slots[s];
                    var r = new Rect(cx + 6, gridTop + s * rowH, colW - 12, rowH - 10);
                    if (!d.slots.Contains(slot))
                    {
                        Box(r, TM.Hex("#EEE8E0"), 16);
                        GUI.Label(r, d.day == 1 ? "抵達前" : "回程", St(sSmall, 20, Grey, TextAnchor.MiddleCenter, false));
                        continue;
                    }
                    if (repaint) cells.Add(new Cell { day = d.day, slot = slot, r = r });
                    DropGlow(r, mouse);
                    var it = Planner.At(board, d.day, slot);
                    if (it == null)
                    {
                        bool sel = selDay == d.day && selSlot == slot;
                        if (sel) Box(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), Persimmon, 20);
                        Box(r, Color.white, 16);
                        GUI.Label(r, slot + "\n＋", St(sSmall, 22, Grey, TextAnchor.MiddleCenter, true));
                        continue;
                    }
                    PlanCell(r, it, issues);
                    int tv = Planner.TravelInto(board, it);
                    if (tv > 0 && s > 0)
                        GUI.Label(new Rect(r.x, r.y - 16, r.width, 20), "↓ " + tv + " 分", St(sSmall, 17, tv >= 35 ? TM.Hex("#E2463B") : Grey, TextAnchor.MiddleCenter, true));
                }
            }

            // 底部：費用＋垃圾桶＋定版
            int total = board.Sum(i => { var p = Planner.Get(i.poiId); return p != null ? p.cost : 0; });
            float fy = B.yMax - 108;
            GUI.Label(new Rect(B.x + 30, fy + 8, 340, 44), "每人約 NT$" + total.ToString("N0"), St(sTitle, 32, Ink, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(B.x + 30, fy + 54, 340, 32), "景點＋餐費・預算" + Session.Budget.name, St(sSmall, 21, Grey, TextAnchor.MiddleLeft, false));
            trashRect = new Rect(B.x + 380, fy + 10, 200, 80);
            bool overTrash = dragging && dragItem != null && trashRect.Contains(mouse);
            Box(trashRect, overTrash ? TM.Hex("#E2463B") : dragging && dragItem != null ? TM.Hex("#FFD6CC") : TM.Hex("#EEE8E0"), 22);
            GUI.Label(trashRect, "移出行程", St(sName, 24, overTrash ? Color.white : TM.Hex("#B8440E"), TextAnchor.MiddleCenter, true));
            if (Button(new Rect(B.xMax - 320, fy + 10, 290, 80), "定版 ✓", Persimmon, Color.white, 32, !dragging)) planDone = true;

            // 右側
            var S = new Rect(B.xMax + 20, B.y, W * 0.5f + 720 - B.xMax - 20, B.height);
            Shadowed(S, Cream, 28);
            if (selDay > 0) DrawSlotDetail(S);
            else DrawPlanTips(S, issues);

            // 拖曳中的卡片跟著滑鼠
            if (dragging)
            {
                string name = dragItem != null ? Planner.Get(dragItem.poiId).name : dragWish != null ? dragWish.poi.name : "";
                var gs = St(sCardName, 24, Color.white, TextAnchor.MiddleCenter, true);
                float gw = Mathf.Min(360, gs.CalcSize(new GUIContent(name)).x + 50);
                var g = new Rect(mouse.x - gw * 0.5f, mouse.y - 34, gw, 68);
                var oc = GUI.color; GUI.color = new Color(1, 1, 1, 0.92f);
                Shadowed(g, dragWish != null ? TM.Hex("#7C5CE0") : Persimmon, 22);
                GUI.Label(g, name, gs);
                GUI.color = oc;
            }
        }

        void DropGlow(Rect r, Vector2 mouse)
        {
            if (dragging && r.Contains(mouse)) Box(new Rect(r.x - 6, r.y - 6, r.width + 12, r.height + 12), Gold, 22);
        }

        Cell CellAt(Vector2 m) { return cells.FirstOrDefault(c => c.r.Contains(m)); }

        /// <summary>滑鼠按下／拖曳／放開：點一下選格子，拖曳交換或移動，拖到垃圾桶移出，把隊友許願拖進格子。</summary>
        void HandlePlanDrag()
        {
            var e = Event.current;
            var m = e.mousePosition;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                var c = CellAt(m);
                if (c != null)
                {
                    dragItem = Planner.At(board, c.day, c.slot);
                    dragWish = null;
                    dragStart = m;
                    dragging = false;
                    if (dragItem == null) { selDay = c.day; selSlot = c.slot; Sfx.Pop(); }
                    e.Use();
                    return;
                }
                foreach (var kv in wishRects)
                {
                    if (kv.Key.placed || !kv.Value.Contains(m)) continue;
                    dragWish = kv.Key;
                    dragItem = null;
                    dragStart = m;
                    dragging = false;
                    e.Use();
                    return;
                }
            }
            else if (e.type == EventType.MouseDrag && (dragItem != null || dragWish != null))
            {
                if (!dragging && Vector2.Distance(m, dragStart) > 10) { dragging = true; Sfx.Blip(1.3f); }
                e.Use();
            }
            else if (e.type == EventType.MouseUp && (dragItem != null || dragWish != null))
            {
                if (!dragging)
                {
                    if (dragItem != null) { selDay = dragItem.day; selSlot = dragItem.slot; Sfx.Pop(); }
                }
                else if (dragItem != null && trashRect.Contains(m))
                {
                    board.Remove(dragItem);
                    Toast("移出「" + Planner.Get(dragItem.poiId).name + "」");
                    Sfx.Land();
                    selDay = -1;
                }
                else
                {
                    var c = CellAt(m);
                    if (c != null)
                    {
                        if (dragItem != null) MoveItem(dragItem, c.day, c.slot);
                        else DropWish(dragWish, c.day, c.slot);
                    }
                }
                dragItem = null; dragWish = null; dragging = false;
                e.Use();
            }
        }

        static bool FullDay(int day) { return day > 1 && day < Session.days; }

        void SwapDays(int a, int b)
        {
            if (a == b) return;
            if (!FullDay(a) || !FullDay(b)) { Toast("抵達日和回程日不能整天交換"); Sfx.Buzz(); return; }
            foreach (var i in board)
            {
                if (i.day == a) i.day = b;
                else if (i.day == b) i.day = a;
            }
            Sfx.Pop(1.2f);
            Toast("Day " + a + " 和 Day " + b + " 對調了");
            selDay = -1;
        }

        void MoveItem(PlanItem it, int day, string slot)
        {
            if (it.day == day && it.slot == slot) return;
            bool fromAll = it.slot == Planner.AllDaySlot;
            bool toAll = slot == Planner.AllDaySlot;
            if (fromAll || toAll) { SwapDays(it.day, day); return; }
            var other = Planner.At(board, day, slot);
            if (other != null) { other.day = it.day; other.slot = it.slot; }
            it.day = day;
            it.slot = slot;
            Sfx.Pop(1.2f);
            selDay = -1;
        }

        void DropWish(Wish w, int day, string slot)
        {
            if (w == null) return;
            if (slot == Planner.AllDaySlot) { Toast("那天整天都排滿了，換一格吧"); Sfx.Buzz(); return; }
            var it = Planner.At(board, day, slot);
            if (it != null) { it.poiId = w.poi.id; it.by = w.who != null ? w.who.name : "隱藏"; }
            else board.Add(new PlanItem { day = day, slot = slot, poiId = w.poi.id, by = w.who != null ? w.who.name : "隱藏" });
            w.placed = true;
            w.msg = "放在 Day " + day + " " + slot;
            Sfx.Pop(1.3f);
            Toast(w.who != null ? w.who.name + "：謝啦！" : "放進了隱藏景點「" + w.poi.name + "」");
        }

        void PlanCell(Rect r, PlanItem it, List<PlanIssue> issues)
        {
            var poi = Planner.Get(it.poiId);
            bool sel = selDay == it.day && selSlot == it.slot;
            bool warn = issues.Any(x => x.slotKey == it.day + "-" + it.slot);
            bool lifted = dragging && dragItem == it;
            if (sel && !lifted) Box(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), Persimmon, 20);
            Box(r, lifted ? TM.Hex("#F3EDE4") : Hover(r) ? TM.Hex("#FFF6E6") : Color.white, 16);
            if (lifted) { GUI.Label(r, "⋯", St(sTitle, 30, Grey, TextAnchor.MiddleCenter, true)); return; }
            Color tagC = TagColor(poi.tags[0]);
            Box(new Rect(r.x, r.y, 8, r.height), tagC, 4);
            GUI.Label(new Rect(r.x + 16, r.y + 6, r.width - 24, 24), it.slot == Planner.AllDaySlot ? "整天" : it.slot, St(sSmall, 18, Grey, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(r.x + 16, r.y + 32, r.width - 24, r.height - 66), poi.name, St(sCardName, 22, Ink, TextAnchor.UpperLeft, true, true));
            GUI.Label(new Rect(r.x + 16, r.yMax - 32, r.width - 24, 26), Planner.Dur(poi.minutes), St(sSmall, 17, Grey, TextAnchor.MiddleLeft, false));
            if (it.by != "guide")
            {
                string who = it.by == "user" ? "你" : it.by;
                var c = new Rect(r.xMax - 70, r.y + 6, 62, 24);
                Box(c, it.by == "user" ? Persimmon : TM.Hex("#7C5CE0"), 12);
                GUI.Label(c, who, St(sName, 15, Color.white, TextAnchor.MiddleCenter, true));
            }
            if (warn) Circle(new Vector2(r.xMax - 16, r.yMax - 18), 18, TM.Hex("#E2463B"));
        }

        static Color TagColor(string tag)
        {
            switch (tag)
            {
                case "美食": return TM.Hex("#FF6B35");
                case "購物": return TM.Hex("#E0508F");
                case "拍照": return TM.Hex("#7C5CE0");
                case "放空": return TM.Hex("#3E8EDE");
                case "探索": return TM.Hex("#2F9E62");
                case "文化": return TM.Hex("#9A7400");
                case "夜生活": return TM.Hex("#5B3FC4");
                case "親子": return TM.Hex("#E8853C");
                case "溫泉": return TM.Hex("#2A83AD");
                default: return TM.Hex("#C93C7E");
            }
        }

        void DrawSlotDetail(Rect S)
        {
            wishRects.Clear();
            var it = Planner.At(board, selDay, selSlot);
            float y = S.y + 24;
            GUI.Label(new Rect(S.x + 28, y, S.width - 140, 44), "Day " + selDay + "　" + (selSlot == Planner.AllDaySlot ? "整天" : selSlot), St(sTitle, 32, Ink, TextAnchor.MiddleLeft, true));
            if (Button(new Rect(S.xMax - 120, y, 96, 46), "返回", TM.Hex("#E3D6C6"), Ink, 22)) { selDay = -1; return; }
            y += 62;
            if (it != null)
            {
                var poi = Planner.Get(it.poiId);
                var c = new Rect(S.x + 24, y, S.width - 48, 176);
                Box(c, Color.white, 18);
                GUI.Label(new Rect(c.x + 20, c.y + 12, c.width - 40, 40), poi.name, St(sCardName, 28, Ink, TextAnchor.UpperLeft, true));
                GUI.Label(new Rect(c.x + 20, c.y + 54, c.width - 40, 30), poi.area + "・" + Planner.Dur(poi.minutes) + "・" + poi.price, St(sSmall, 21, Grey, TextAnchor.MiddleLeft, false));
                GUI.Label(new Rect(c.x + 20, c.y + 88, c.width - 40, 80), poi.blurb, St(sCardText, 21, Ink, TextAnchor.UpperLeft, false, true));
                y += 190;
                if (it.slot == Planner.AllDaySlot)
                {
                    if (Button(new Rect(S.x + 24, y, S.width - 48, 64), "改成上午／下午／晚上", TM.Hex("#3E8EDE"), Color.white, 24))
                    {
                        board.Remove(it);
                        var used = new HashSet<string>(board.Select(i => i.poiId));
                        foreach (var slot in Planner.Slots)
                        {
                            var best = Planner.Candidates(board, selDay, slot, planW, used).FirstOrDefault().Key;
                            if (best == null) continue;
                            used.Add(best.id);
                            board.Add(new PlanItem { day = selDay, slot = slot, poiId = best.id, by = "guide" });
                        }
                        selSlot = "上午";
                        Toast("Day " + selDay + " 改回三個時段了");
                    }
                    return;
                }
            }
            GUI.Label(new Rect(S.x + 28, y, S.width - 56, 36), "趣趣推薦", St(sSmall, 26, Persimmon, TextAnchor.MiddleLeft, true));
            y += 44;
            var exclude = new HashSet<string>(board.Select(i => i.poiId));
            var alts = Planner.Candidates(board, selDay, selSlot, planW, exclude).Take(3).ToList();
            foreach (var kv in alts)
            {
                var p = kv.Key;
                var c = new Rect(S.x + 24, y, S.width - 48, 140);
                Box(c, Color.white, 18);
                Box(new Rect(c.x, c.y, 8, c.height), TagColor(p.tags[0]), 4);
                GUI.Label(new Rect(c.x + 22, c.y + 12, c.width - 140, 36), p.name, St(sCardName, 25, Ink, TextAnchor.UpperLeft, true));
                GUI.Label(new Rect(c.x + 22, c.y + 56, c.width - 40, 76), Planner.Reason(p, board, selDay, selSlot, planW), St(sCardText, 21, Grey, TextAnchor.UpperLeft, false, true));
                if (Button(new Rect(c.xMax - 108, c.y + 10, 92, 44), "換", Persimmon, Color.white, 22))
                {
                    if (it != null) { it.poiId = p.id; it.by = "user"; }
                    else board.Add(new PlanItem { day = selDay, slot = selSlot, poiId = p.id, by = "user" });
                    Sfx.Pop(1.2f);
                    Toast("換成「" + p.name + "」");
                }
                y += 152;
            }
        }

        void DrawPlanTips(Rect S, List<PlanIssue> issues)
        {
            float y = S.y + 24;
            GUI.Label(new Rect(S.x + 28, y, S.width - 56, 44), "趣趣提醒", St(sTitle, 32, Ink, TextAnchor.MiddleLeft, true));
            y += 54;
            if (issues.Count == 0)
            {
                var ok = new Rect(S.x + 24, y, S.width - 48, 70);
                Box(ok, TM.Hex("#DDF5EA"), 18);
                GUI.Label(ok, "很順！交通、預算、時段都 OK", St(sCardName, 24, TM.Hex("#1F7A55"), TextAnchor.MiddleCenter, true));
                y += 86;
            }
            foreach (var iss in issues.Take(2))
            {
                var c = new Rect(S.x + 24, y, S.width - 48, 132);
                Box(c, TM.Hex("#FFF1EA"), 18);
                GUI.Label(new Rect(c.x + 20, c.y + 10, c.width - 40, 60), iss.text, St(sCardText, 20, Ink, TextAnchor.UpperLeft, false, true));
                var fix = iss.fixPoi != null ? Planner.Get(iss.fixPoi) : null;
                if (fix != null && Button(new Rect(c.x + 20, c.yMax - 54, c.width - 40, 44), "換成「" + fix.name + "」", TM.Hex("#FF8A6B"), Color.white, 20))
                {
                    var parts = iss.slotKey.Split('-');
                    var it = Planner.At(board, int.Parse(parts[0]), parts[1]);
                    if (it != null) { it.poiId = fix.id; it.by = "user"; Sfx.Pop(1.2f); Toast("已換成「" + fix.name + "」"); }
                }
                y += 144;
            }
            y += 8;
            int hiddenN = wishes.Count(w => w.who == null);
            string[] wt = { "隊友想去", "隱藏景點 " + hiddenN };
            for (int i = 0; i < 2; i++)
            {
                var tr = new Rect(S.x + 24 + i * 214, y, 204, 46);
                bool on = wishTab == i;
                Box(tr, on ? (i == 0 ? TM.Hex("#7C5CE0") : TM.Hex("#8C5BE0")) : TM.Hex("#EEE8E0"), 23);
                GUI.Label(tr, wt[i], St(sName, 21, on ? Color.white : Ink, TextAnchor.MiddleCenter, true));
                if (GUI.Button(tr, GUIContent.none, GUIStyle.none)) { wishTab = i; Sfx.Pop(); }
            }
            y += 56;
            GUI.Label(new Rect(S.x + 28, y, S.width - 56, 28), "拖進左邊的格子", St(sSmall, 19, Grey, TextAnchor.MiddleLeft, false));
            y += 32;
            if (Event.current.type == EventType.Repaint) wishRects.Clear();
            var list = wishes.Where(w => (w.who == null) == (wishTab == 1)).OrderBy(w => w.placed ? 1 : 0).ToList();
            if (list.Count == 0)
                GUI.Label(new Rect(S.x + 28, y, S.width - 56, 60), wishTab == 1 ? "還沒找到。下一趟留意角落的紫色印章！" : "隊友都沒有特別想去的地方", St(sCardText, 20, Grey, TextAnchor.UpperLeft, false, true));
            int shown = 0;
            foreach (var w in list)
            {
                if (y + 80 > S.yMax - 10) break;
                var c = new Rect(S.x + 24, y, S.width - 48, 76);
                if (Event.current.type == EventType.Repaint) wishRects[w] = c;
                bool lifted = dragging && dragWish == w;
                Box(c, w.placed ? TM.Hex("#EEF8F2") : lifted ? TM.Hex("#F3EDE4") : Hover(c) ? TM.Hex("#FFF6E6") : Color.white, 18);
                Color cc = w.who != null ? w.who.color : TM.Hex("#8C5BE0");
                Circle(new Vector2(c.x + 36, c.center.y), 46, cc);
                GUI.Label(new Rect(c.x + 13, c.center.y - 23, 46, 46), w.who != null ? w.who.name.Substring(0, 1) : "★", St(sName, 22, Color.white, TextAnchor.MiddleCenter, true));
                GUI.Label(new Rect(c.x + 70, c.y + 6, c.width - 120, 26), w.placed ? w.msg : w.who != null ? w.who.name + " 想去" : "隱藏景點・" + w.poi.area, St(sSmall, 18, w.placed ? TM.Hex("#1F7A55") : Grey, TextAnchor.MiddleLeft, true));
                GUI.Label(new Rect(c.x + 70, c.y + 34, c.width - 120, 36), w.poi.name, St(sCardName, 23, Ink, TextAnchor.MiddleLeft, true));
                if (!w.placed && !lifted) GUI.Label(new Rect(c.xMax - 46, c.y, 36, c.height), "≡", St(sTitle, 28, TM.Hex("#C9C1B6"), TextAnchor.MiddleCenter, true));
                y += 84;
                shown++;
            }
            if (shown < list.Count)
                GUI.Label(new Rect(S.x + 28, S.yMax - 34, S.width - 56, 26), "還有 " + (list.Count - shown) + " 個，放進去後會往上補", St(sSmall, 17, Grey, TextAnchor.MiddleLeft, false));
        }

        // ---------------- 去趣 eSIM：照行程算網路 ----------------
        NetNeeds needs;
        string pickPlan, pickCarrier, carrierWhy;
        bool termsOk, termsOpen;
        int esimState;   // 0 選方案，1 已購買，2 跳過
        bool esimClose;

        IEnumerator EsimPlanCo()
        {
            int groups = team != null && team.splitPct >= 30 ? 2 : 1;
            needs = Esim.Needs(board, Session.days, groups);
            pickPlan = needs.recommended;
            pickCarrier = Esim.SuggestCarrier(board, out carrierWhy);
            termsOk = false;
            termsOpen = false;
            esimState = 0;
            esimClose = false;
            yield return Say("趣趣", "行程定好了！最後一件事：照這份行程，算算大家在日本要多少網路。");
            dialogOn = false;
            screen = Page.EsimPlan;
            while (!esimClose) yield return null;
            screen = Page.None;
            if (esimState == 1)
            {
                Session.network = "esim";
                Sfx.Fanfare();
                yield return Say("趣趣", "搞定！行程有了、網路也有了，這趟出發什麼都不用擔心～");
            }
            else yield return Say("趣趣", "沒問題！之後到去趣官網一樣是全館 85 折喔。");
            dialogOn = false;
        }

        void DrawEsimPlan()
        {
            var p = PanelRect(1500, 980, 50);
            Shadowed(p, Cream, 36);
            Box(new Rect(p.x, p.y, p.width, 116), Persimmon, 36);
            Box(new Rect(p.x, p.y + 80, p.width, 36), Persimmon, 0);
            if (esimState == 1) { DrawEsimDone(p); return; }

            int billable = Esim.BillableDays(Session.days);
            GUI.Label(new Rect(p.x + 44, p.y + 14, p.width - 400, 60), "照你們的行程，每天約需 " + needs.gbPerDay.ToString("0.0") + " GB", St(sBanner, 44, Color.white, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(p.x + 46, p.y + 70, p.width - 400, 34), Session.TripLabel + "・" + needs.spots + " 個景點・去趣 eSIM 全館 85 折", St(sSmall, 24, Color.white, TextAnchor.MiddleLeft, true));
            var cp = new Rect(p.xMax - 330, p.y + 30, 290, 58);
            Box(cp, Color.white, 29);
            GUI.Label(cp, "去趣官網全館 85 折", St(sName, 24, Persimmon, TextAnchor.MiddleCenter, true));

            // 為什麼要這麼多：行程裡吃網路的地方＋這趟遊戲裡的網路紀錄
            float y = p.y + 138;
            float colX = p.x + 44;
            foreach (var dr in needs.drivers.Take(3))
            {
                Circle(new Vector2(colX + 14, y + 20), 16, Persimmon);
                GUI.Label(new Rect(colX + 34, y, 640, 40), dr, St(sSmall, 23, Ink, TextAnchor.MiddleLeft, true));
                y += 42;
            }
            var st = new Rect(p.x + 760, p.y + 138, p.width - 804, 112);
            bool smooth = Esim.fails == 0 && Esim.offlineSec < 1f && Esim.slowSec < 1f;
            Box(st, smooth ? TM.Hex("#DDF5EA") : TM.Hex("#FFE1D3"), 20);
            GUI.Label(new Rect(st.x + 24, st.y + 10, st.width - 40, 36), "你這趟用的是「" + Esim.NetName + "」", St(sSmall, 22, Grey, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(st.x + 24, st.y + 46, st.width - 40, 56), Esim.StatsLine(), St(sCardName, 26, smooth ? TM.Hex("#1F7A55") : TM.Hex("#B8440E"), TextAnchor.MiddleLeft, true, true));

            // 4 個方案
            float cy = p.y + 280, cw = (p.width - 88 - 3 * 20) / 4f, ch = 250;
            for (int i = 0; i < Esim.Plans.Length; i++)
            {
                var pl = Esim.Plans[i];
                var r = new Rect(p.x + 44 + i * (cw + 20), cy, cw, ch);
                bool sel = pickPlan == pl.key;
                if (Card(r, sel, Persimmon)) { pickPlan = pl.key; Sfx.Pop(1f + i * 0.1f); }
                if (pl.key == needs.recommended)
                {
                    var t = new Rect(r.x + 16, r.y - 18, 150, 40);
                    Box(t, Green, 20);
                    GUI.Label(t, "最適合你們", St(sName, 20, Color.white, TextAnchor.MiddleCenter, true));
                }
                GUI.Label(new Rect(r.x + 22, r.y + 28, r.width - 40, 44), pl.name, St(sCardName, 30, Ink, TextAnchor.UpperLeft, true));
                GUI.Label(new Rect(r.x + 22, r.y + 78, r.width - 40, 50), "NT$" + Esim.SalePerDay(pl) + "／天", St(sTitle, 36, Persimmon, TextAnchor.MiddleLeft, true));
                GUI.Label(new Rect(r.x + 22, r.y + 126, r.width - 40, 30), "原價 NT$" + pl.listPerDay, St(sSmall, 20, TM.Hex("#A8A096"), TextAnchor.MiddleLeft, false));
                GUI.Label(new Rect(r.x + 22, r.y + 160, r.width - 40, 70), pl.good, St(sCardText, 22, Ink, TextAnchor.UpperLeft, false, true));
            }

            // 電信商
            y = cy + ch + 34;
            GUI.Label(new Rect(p.x + 44, y, 200, 50), "電信商", St(sCardName, 28, Ink, TextAnchor.MiddleLeft, true));
            float bx = p.x + 190;
            foreach (var c in Esim.Carriers)
            {
                bool sel = pickCarrier == c.key;
                var cr = new Rect(bx, y, 220, 54);
                if (Card(cr, sel, TM.Hex("#3E8EDE"))) { pickCarrier = c.key; Sfx.Pop(); }
                GUI.Label(cr, c.name, St(sName, 24, sel ? TM.Hex("#3E8EDE") : Ink, TextAnchor.MiddleCenter, true));
                bx += 236;
            }
            var car = Esim.Carrier(pickCarrier);
            GUI.Label(new Rect(bx + 10, y - 4, p.xMax - bx - 54, 62), car.fit, St(sCardText, 21, Ink, TextAnchor.MiddleLeft, false, true));
            y += 66;
            GUI.Label(new Rect(p.x + 190, y, p.width - 240, 32), "趣趣推薦：" + carrierWhy, St(sSmall, 21, TM.Hex("#3E8EDE"), TextAnchor.MiddleLeft, true));
            y += 46;

            // 隊友各自選的（同步）
            GUI.Label(new Rect(p.x + 44, y, 200, 48), "隊友", St(sCardName, 28, Ink, TextAnchor.MiddleLeft, true));
            float mx = p.x + 190;
            foreach (var m in Session.Mates)
            {
                var plan = Esim.Plan(Esim.MateBias(m.type));
                var mr = new Rect(mx, y, 270, 48);
                Box(mr, Color.white, 24);
                Circle(new Vector2(mr.x + 24, mr.center.y), 36, m.color);
                GUI.Label(new Rect(mr.x + 50, mr.y, mr.width - 56, mr.height), m.name + "・" + plan.name, St(sSmall, 21, Ink, TextAnchor.MiddleLeft, true));
                mx += 284;
                if (mx + 270 > p.xMax - 40) break;
            }
            y += 66;

            // 購買須知＋結帳
            var tb = new Rect(p.x + 44, y, 46, 46);
            Box(tb, termsOk ? Green : Color.white, 12);
            if (termsOk) GUI.Label(tb, "✓", St(sName, 28, Color.white, TextAnchor.MiddleCenter, true));
            if (GUI.Button(new Rect(tb.x, tb.y, 400, 46), GUIContent.none, GUIStyle.none)) { termsOk = !termsOk; Sfx.Pop(); }
            GUI.Label(new Rect(tb.xMax + 14, y, 330, 46), "我已閱讀購買須知", St(sCardName, 25, Ink, TextAnchor.MiddleLeft, true));
            if (Button(new Rect(tb.xMax + 330, y, 140, 46), termsOpen ? "收起" : "看須知", TM.Hex("#E3D6C6"), Ink, 20)) termsOpen = !termsOpen;
            if (termsOpen)
            {
                var tr = new Rect(p.x + 44, y - 300, 760, 290);
                Shadowed(tr, Color.white, 18);
                float ty = tr.y + 16;
                foreach (var t in Esim.Terms)
                {
                    GUI.Label(new Rect(tr.x + 24, ty, tr.width - 48, 46), "・" + t, St(sCardText, 20, Ink, TextAnchor.UpperLeft, false, true));
                    ty += 45;
                }
            }

            var chosen = Esim.Plan(pickPlan);
            int price = Esim.Price(chosen, Session.days);
            GUI.Label(new Rect(p.xMax - 640, p.yMax - 112, 300, 40), billable + " 天方案・" + car.name, St(sSmall, 22, Grey, TextAnchor.MiddleRight, true));
            GUI.Label(new Rect(p.xMax - 640, p.yMax - 76, 300, 56), "NT$" + price.ToString("N0"), St(sBanner, 46, Ink, TextAnchor.MiddleRight, true));
            if (Button(new Rect(p.xMax - 320, p.yMax - 106, 280, 84), "結帳 ▶", Persimmon, Color.white, 32, termsOk))
            {
                Session.boughtPlan = chosen.key;
                Session.boughtCarrier = car.key;
                Session.boughtPrice = price;
                Session.lockedGb = needs.gbPerDay;
                esimState = 1;
                Sfx.Fanfare();
                Fx.Confetti(player.transform.position + Vector3.up * 2f, 80);
            }
            if (!termsOk) GUI.Label(new Rect(p.xMax - 320, p.yMax - 22, 280, 20), "勾選購買須知後才能結帳", St(sSmall, 17, Grey, TextAnchor.MiddleCenter, false));
            if (Button(new Rect(p.x + 44, p.yMax - 96, 200, 64), "先不買", TM.Hex("#E3D6C6"), Ink, 24)) { esimState = 2; esimClose = true; }
        }

        void DrawEsimDone(Rect p)
        {
            var plan = Esim.Plan(Session.boughtPlan);
            var car = Esim.Carrier(Session.boughtCarrier);
            GUI.Label(new Rect(p.x, p.y + 24, p.width, 70), "訂單成立 ✓", St(sBanner, 52, Color.white, TextAnchor.MiddleCenter, true));
            var c = new Rect(p.center.x - 480, p.y + 180, 960, 300);
            Box(c, Color.white, 28);
            GUI.Label(new Rect(c.x + 50, c.y + 34, c.width - 100, 60), "去趣 eSIM・" + plan.name, St(sTitle, 42, Ink, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(c.x + 50, c.y + 104, c.width - 100, 40), Esim.BillableDays(Session.days) + " 天・" + car.name, St(sSmall, 28, Grey, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(c.x + 50, c.y + 170, c.width - 100, 80), "NT$" + Session.boughtPrice.ToString("N0"), St(sBanner, 64, Persimmon, TextAnchor.MiddleLeft, true));
            string[] steps = { "20 分鐘內 Email 收到 QR Code", "出發當天再掃描安裝", "落地連網就開始計算天數" };
            float y = c.yMax + 50;
            for (int i = 0; i < steps.Length; i++)
            {
                var r = new Rect(p.center.x - 480 + i * 330, y, 300, 150);
                Box(r, TM.Hex("#FFE7CC"), 24);
                Circle(new Vector2(r.center.x, r.y + 44), 56, Persimmon);
                GUI.Label(new Rect(r.center.x - 28, r.y + 16, 56, 56), (i + 1).ToString(), St(sName, 28, Color.white, TextAnchor.MiddleCenter, true));
                GUI.Label(new Rect(r.x + 16, r.y + 84, r.width - 32, 56), steps[i], St(sCardName, 24, Ink, TextAnchor.MiddleCenter, true, true));
            }
            if (Button(new Rect(p.center.x - 200, p.yMax - 120, 400, 86), "看旅程結果 ▶", Persimmon, Color.white, 32)) esimClose = true;
        }

        // ---------------- 結局 ----------------
        public IEnumerator EndingCo(TeamAnalysis t, Action<bool> done)
        {
            Busy = true;
            player.locked = true;
            endGo = false;
            endBorn = Time.time;
            screen = Page.Ending;
            int stars = Stars();
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(0.45f);
                if (i < stars) Sfx.Pop(1f + i * 0.2f); else Sfx.Land();
            }
            if (stars == 3) { Sfx.Fanfare(); Fx.Confetti(player.transform.position + Vector3.up * 2f); }
            while (!endGo) yield return null;
            screen = Page.None;
            done(endRestart);
        }

        public const int ConsensusGoal = 6;

        static bool[] StarsGot()
        {
            return new[]
            {
                Session.history.Count >= GameData.Rounds.Length && Session.lockedSpots > 0,  // 走完 10 站並定版行程
                Session.Consensus >= ConsensusGoal,                                          // 共識 6 站以上
                !string.IsNullOrEmpty(Session.boughtPlan),                                   // 網路也準備好了
            };
        }

        int Stars() { return StarsGot().Count(x => x); }

        void DrawEnding()
        {
            float k = EaseOutBack((Time.time - endBorn) / 0.4f);
            var c = ScaleRect(new Rect(W * 0.5f - 600, 36, 1200, 1010), k);
            Shadowed(c, Cream, 40);
            if (k < 0.6f) return;
            GUI.Label(new Rect(c.x, c.y + 26, c.width, 80), "旅程完成！", St(sBanner, 64, Ink, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(c.x, c.y + 102, c.width, 40), Session.Dest.name + " " + Session.TripLabel + "・" + Session.players.Count + " 人", St(sSmall, 26, Grey, TextAnchor.MiddleCenter, true));
            var got = StarsGot();
            string[] labels = { "定版行程", "共識 " + ConsensusGoal + " 站以上", "網路準備好" };
            var starStyle = new GUIStyle(St(sBanner, 90, Gold, TextAnchor.MiddleCenter, true));
            for (int i = 0; i < 3; i++)
            {
                float appear = (Time.time - endBorn - 0.45f * (i + 1)) / 0.3f;
                float sk = appear <= 0 ? 0.6f : EaseOutBack(appear);
                var pc = new Vector2(c.center.x + (i - 1) * 260, c.y + 236);
                bool on = got[i] && appear > 0;
                Circle(pc, 150 * Mathf.Max(0.6f, sk), on ? TM.Hex("#FFE8A3") : TM.Hex("#EDE3D6"));
                starStyle.normal.textColor = on ? TM.Hex("#FFB000") : TM.Hex("#CFC3B4");
                GUI.Label(new Rect(pc.x - 80, pc.y - 62, 160, 120), "★", starStyle);
                GUI.Label(new Rect(pc.x - 130, pc.y + 82, 260, 36), labels[i], St(sSmall, 26, Ink, TextAnchor.MiddleCenter, true));
            }
            int vibe = team != null ? team.vibe : Scoring.LiveVibe();
            string stats = "共識 " + Session.Consensus + "／10　·　默契 " + vibe + "　·　這趟撿到印章 " + Collectible.Collected + " 枚";
            GUI.Label(new Rect(c.x, c.y + 364, c.width, 44), stats, St(sToast, 28, Ink, TextAnchor.MiddleCenter, true));

            // 旅途回憶
            float y = c.y + 420;
            if (Session.memories.Count > 0)
            {
                var ms = St(sName, 21, TM.Hex("#7C5CE0"), TextAnchor.MiddleCenter, true);
                var shown = Session.memories.Take(6).ToList();
                float total = shown.Sum(m => ms.CalcSize(new GUIContent(m)).x + 40) + (shown.Count - 1) * 10;
                float mx = c.center.x - Mathf.Min(total, c.width - 80) * 0.5f;
                foreach (var m in shown)
                {
                    float tw = ms.CalcSize(new GUIContent(m)).x + 40;
                    if (mx + tw > c.xMax - 40) break;
                    Box(new Rect(mx, y, tw, 44), TM.Hex("#EDE6FF"), 22);
                    GUI.Label(new Rect(mx, y, tw, 44), m, ms);
                    mx += tw + 10;
                }
                y += 58;
            }

            // 網路
            var nr = new Rect(c.x + 60, y, c.width - 120, 56);
            bool bought = !string.IsNullOrEmpty(Session.boughtPlan);
            Box(nr, bought ? TM.Hex("#FFE7CC") : TM.Hex("#EEE8E0"), 28);
            GUI.Label(nr, bought ? "去趣 eSIM・" + Esim.Plan(Session.boughtPlan).name + "・" + Esim.Carrier(Session.boughtCarrier).name + "　已購買 ✓" : "網路還沒準備好",
                St(sName, 24, bought ? TM.Hex("#B8440E") : Grey, TextAnchor.MiddleCenter, true));
            y += 74;

            // 定版的行程
            var box = new Rect(c.x + 60, y, c.width - 120, c.yMax - 124 - y);
            Box(box, Color.white, 22);
            float ly = box.y + 16;
            foreach (var d in Planner.Days(Session.days))
            {
                var items = board.Where(i => i.day == d.day).OrderBy(i => i.slot == Planner.AllDaySlot ? -1 : Array.IndexOf(Planner.Slots, i.slot))
                    .Select(i => Planner.Get(i.poiId).name).ToArray();
                GUI.Label(new Rect(box.x + 24, ly, 110, 36), d.label, St(sCardName, 23, Persimmon, TextAnchor.MiddleLeft, true));
                GUI.Label(new Rect(box.x + 130, ly, box.width - 150, 36), items.Length > 0 ? string.Join(" → ", items) : "自由活動", St(sSmall, 22, Ink, TextAnchor.MiddleLeft, false));
                ly += 38;
                if (ly > box.yMax - 30) break;
            }
            if (Button(new Rect(c.center.x - 280, c.yMax - 104, 560, 80), "回主選單（印章 " + Accessories.Wallet + " 枚可換配件）", Persimmon, Color.white, 28)) { endRestart = true; endGo = true; }
        }
    }
}

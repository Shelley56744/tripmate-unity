using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 場景互動（按 E）：去趣 eSIM 櫃台、公告欄、搶整理券、拍照打卡上傳、退稅比價、市場試吃、熱甘酒、
    /// 射的小遊戲、地下街導航、泡溫泉。很多事情要靠網路，沒有 eSIM 時會卡住、降速或斷線。
    /// </summary>
    public partial class DialogueUI
    {
        // 去趣 eSIM 開通
        int shopChoice = -1;
        float shopScanBorn = -1f;
        // 通用選擇
        string chooseTitle = "";
        string[] chooseOpts = new string[0];
        int chosenIdx = -1;
        // 射的
        float shootEnd;
        int shootScore;
        Vector2 shootTarget;
        float shootTargetBorn;
        // 泡湯
        float soakBorn;

        public IEnumerator Activity(Interactable it)
        {
            Busy = true;
            player.locked = true;
            switch (it.kind)
            {
                case "esim": yield return EsimShopCo(it); break;
                case "board": yield return Say("公告欄", "今日 2 月 3 日・大阪 5°C 有小雪。隊長提醒：每站都要走到發光的站點喔！"); break;
                case "ticket": yield return TicketCo(); break;
                case "photo_dotonbori": yield return PhotoCo("道頓堀橋上合照"); break;
                case "photo_castle": yield return PhotoCo("天守閣雪景合照"); break;
                case "photo_umeda": yield return PhotoCo("梅田高樓夕陽"); break;
                case "photo_tower": yield return PhotoCo("通天閣下的黃昏"); break;
                case "taxfree": yield return TaxFreeCo(); break;
                case "snack": yield return SnackCo(); break;
                case "amazake": yield return AmazakeCo(); break;
                case "shooting": yield return ShootingCo(); break;
                case "underground": yield return UndergroundCo(); break;
                case "soak": yield return SoakCo(it); break;
            }
            dialogOn = false;
            screen = Page.None;
            it.MarkUsed();
            player.locked = false;
            Busy = false;
        }

        // ---------------- 去趣 eSIM 櫃台（也可以從左上角按鈕隨時開通） ----------------
        public IEnumerator EsimShopCo(Interactable it)
        {
            bool was = Busy;
            Busy = true;
            player.locked = true;
            if (Session.network == "esim")
            {
                yield return Say("趣趣", "你已經在用去趣 eSIM 了，訊號滿格，安心玩！");
            }
            else
            {
                shopChoice = -1;
                shopScanBorn = -1f;
                screen = Page.EsimShop;
                while (shopChoice < 0) yield return null;
                if (shopChoice == 1)
                {
                    shopScanBorn = Time.time;
                    Sfx.Chime();
                    yield return new WaitForSeconds(1.8f);
                    Session.network = "esim";
                    Session.triedEsimInGame = true;
                    Esim.used = 0;
                    Sfx.Fanfare();
                    Fx.Sparkle(player.transform.position + Vector3.up * 1.4f, 30);
                    screen = Page.None;
                    yield return Say("趣趣", "開通了！訊號滿格，導航和上傳都順了～");
                    Toast("已切換成去趣 eSIM", 2.5f);
                }
                screen = Page.None;
            }
            dialogOn = false;
            if (!was) { player.locked = false; Busy = false; }
        }

        void DrawEsimShop()
        {
            var p = PanelRect(900, 600);
            Shadowed(p, Cream, 36);
            Box(new Rect(p.x, p.y, p.width, 120), Persimmon, 36);
            Box(new Rect(p.x, p.y + 80, p.width, 40), Persimmon, 0);
            GUI.Label(new Rect(p.x, p.y + 16, p.width, 60), "去趣 eSIM", St(sBanner, 50, Color.white, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(p.x, p.y + 72, p.width, 36), "日本每日 2GB・掃 QR 立刻開通", St(sSmall, 24, Color.white, TextAnchor.MiddleCenter, true));
            if (shopScanBorn > 0)
            {
                float t = Mathf.Clamp01((Time.time - shopScanBorn) / 1.6f);
                // QR 掃描動畫
                var q = new Rect(p.center.x - 110, p.y + 170, 220, 220);
                Box(q, Color.white, 16);
                for (int i = 0; i < 7; i++)
                    for (int k = 0; k < 7; k++)
                        if (((i * 7 + k) * 37 % 5) < 2 || (i < 2 && k < 2) || (i > 4 && k < 2) || (i < 2 && k > 4))
                            Box(new Rect(q.x + 15 + i * 27.5f, q.y + 15 + k * 27.5f, 24, 24), Ink, 3);
                Box(new Rect(q.x, q.y + t * q.height, q.width, 6), Persimmon, 3);
                GUI.Label(new Rect(p.x, p.y + 410, p.width, 50), t < 1 ? "掃描中⋯" : "開通完成 ✓", St(sTitle, 34, t < 1 ? Ink : TM.Hex("#1F7A55"), TextAnchor.MiddleCenter, true));
                return;
            }
            float y = p.y + 150;
            string[] pros = { "訊號滿格：導航、地圖不會斷", "人潮裡搶整理券也不卡", "拍照、限動上傳不怕降速", "可自選電信商線路" };
            foreach (var line in pros)
            {
                Circle(new Vector2(p.x + 90, y + 20), 26, Green);
                GUI.Label(new Rect(p.x + 77, y + 6, 26, 28), "✓", St(sKey, 18, Color.white, TextAnchor.MiddleCenter, true));
                GUI.Label(new Rect(p.x + 120, y, p.width - 160, 40), line, St(sCardName, 28, Ink, TextAnchor.MiddleLeft, true));
                y += 56;
            }
            GUI.Label(new Rect(p.x, y + 6, p.width, 40), "去趣官網全館 85 折　每日 2GB 只要 NT$41／天", St(sSmall, 23, Persimmon, TextAnchor.MiddleCenter, true));
            if (Button(new Rect(p.center.x + 20, p.yMax - 110, 360, 80), "掃 QR 開通", Persimmon, Color.white, 30)) shopChoice = 1;
            if (Button(new Rect(p.center.x - 300, p.yMax - 110, 280, 80), "先不用", TM.Hex("#E3D6C6"), Ink, 28)) shopChoice = 0;
        }

        // ---------------- 通用：從幾個選項挑一個 ----------------
        IEnumerator Choose(string title, string[] opts)
        {
            chooseTitle = title;
            chooseOpts = opts;
            chosenIdx = -1;
            screen = Page.Choose;
            while (chosenIdx < 0)
            {
                for (int i = 0; i < opts.Length && i < 4; i++) if (Key((i + 1).ToString())) chosenIdx = i;
                yield return null;
            }
            screen = Page.None;
            Sfx.Pop(1f + chosenIdx * 0.1f);
        }

        void DrawChoose()
        {
            float w = 760, rowH = 92;
            var p = PanelRect(w + 80, 140 + chooseOpts.Length * (rowH + 16), 160);
            Shadowed(p, Cream, 32);
            GUI.Label(new Rect(p.x, p.y + 22, p.width, 60), chooseTitle, St(sTitle, 36, Ink, TextAnchor.MiddleCenter, true));
            for (int i = 0; i < chooseOpts.Length; i++)
            {
                var r = new Rect(p.x + 40, p.y + 100 + i * (rowH + 16), w, rowH);
                if (Card(r, false, Letter[i % 4])) chosenIdx = i;
                Circle(new Vector2(r.x + 46, r.center.y), 52, Letter[i % 4]);
                GUI.Label(new Rect(r.x + 20, r.center.y - 26, 52, 52), (i + 1).ToString(), St(sName, 26, Color.white, TextAnchor.MiddleCenter, true));
                GUI.Label(new Rect(r.x + 92, r.y, r.width - 110, r.height), chooseOpts[i], St(sCardName, 30, Ink, TextAnchor.MiddleLeft, true));
            }
        }

        // ---------------- USJ：搶整理券 ----------------
        IEnumerator TicketCo()
        {
            yield return Say("趣趣", "這台機器可以用 App 搶整理券！要拼手速跟網速～");
            dialogOn = false;
            bool online = Esim.Online, slow = Esim.Throttled || Session.network != "esim";
            lagEsim = Session.network == "esim" && online;
            lagBorn = Time.time;
            screen = Page.Lag;
            yield return new WaitForSeconds(lagEsim ? 1.8f : 3.6f);
            screen = Page.None;
            bool ok = Esim.Spend(0.05f) && !slow;
            if (ok)
            {
                Sfx.Fanfare();
                Session.AddMemory("搶到樂園整理券");
                yield return Say("趣趣", "搶到了！下午 2 點的整理券，大家可以一起進去～");
            }
            else
            {
                Sfx.Buzz();
                yield return Say("趣趣", !online ? "沒有網路，App 根本打不開⋯⋯" : "卡了好幾秒，整理券被搶完了⋯⋯網路慢就是會這樣。");
            }
        }

        // ---------------- 拍照打卡＋上傳 ----------------
        IEnumerator PhotoCo(string memory)
        {
            // 大家擠過來
            Vector3 c = player.transform.position;
            Vector3 fwd = cam ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
            for (int i = 0; i < teammates.Count; i++)
            {
                float a = teammates.Count == 1 ? 0 : -50f + i * (100f / Mathf.Max(1, teammates.Count - 1));
                teammates[i].gatherPoint = c + Quaternion.AngleAxis(a, Vector3.up) * fwd * 1.7f;
            }
            player.transform.rotation = Quaternion.LookRotation(-fwd);
            if (cam) cam.close = true;
            yield return Say("趣趣", "大家靠過來～ 3、2、1⋯⋯");
            dialogOn = false;
            Flash();
            Sfx.Pop(1.8f);
            foreach (var t in teammates) StartCoroutine(t.Hop(1));
            StartCoroutine(player.Hop(1));
            yield return new WaitForSeconds(0.8f);
            Session.AddMemory(memory);
            bool online = Esim.Online, throttled = Esim.Throttled;
            bool ok = Esim.Spend(0.15f);
            if (ok) yield return Say("趣趣", "拍好了！已經上傳到群組，立刻 +3 個讚～");
            else if (online && throttled) yield return Say("趣趣", "拍好了⋯⋯可是降速中，上傳卡在 37%。");
            else yield return Say("趣趣", "拍好了，但沒有網路，照片只能先存在手機裡。");
            foreach (var t in teammates) t.gatherPoint = null;
            if (cam) cam.close = false;
        }

        // ---------------- 心齋橋：退稅比價 ----------------
        IEnumerator TaxFreeCo()
        {
            yield return Say("店員", "いらっしゃいませ！滿 5,000 日圓可以免稅喔～");
            dialogOn = false;
            yield return Choose("要怎麼買？", new[] { "先上網比價再買（要網路）", "直接買到滿額免稅", "看看就好" });
            if (chosenIdx == 0)
            {
                bool ok = Esim.Spend(0.05f);
                yield return Say("趣趣", ok ? "比價完：這間比隔壁便宜 12%，省下 NT$180！" : "網頁一直轉圈圈，查不到比價⋯⋯只好先買了。");
                Session.AddMemory("藥妝戰利品");
            }
            else if (chosenIdx == 1)
            {
                yield return Say("趣趣", "滿額免稅！戰利品 +1，行李箱又更重了～");
                Session.AddMemory("藥妝戰利品");
            }
            else yield return Say("趣趣", "看看就好，荷包守住了。");
        }

        // ---------------- 黑門市場：試吃 ----------------
        IEnumerator SnackCo()
        {
            yield return Choose("要吃哪一串？", new[] { "和牛串　¥1,500", "現烤海膽　¥2,000", "玉子燒　¥300" });
            string[] names = { "和牛串", "現烤海膽", "玉子燒" };
            bool ok = Esim.Spend(0.03f);
            yield return Say("趣趣", ok ? "翻譯 App 一掃：「A5 等級、現點現烤」，看得懂菜單真好！" : "沒有網路查不了翻譯，只好用手指一指⋯⋯");
            Fx.Sparkle(player.transform.position + Vector3.up * 1.3f, 20);
            StartCoroutine(player.Hop(2));
            Sfx.Pop(1.3f);
            Session.AddMemory("黑門市場的" + names[Mathf.Clamp(chosenIdx, 0, 2)]);
            yield return Say("趣趣", "好吃！" + names[Mathf.Clamp(chosenIdx, 0, 2)] + "入手～");
        }

        // ---------------- 大阪城：熱甘酒 ----------------
        IEnumerator AmazakeCo()
        {
            yield return Say("攤販", "熱甘酒一杯 300 日圓，暖暖身子！");
            Fx.Sparkle(player.transform.position + Vector3.up * 1.3f, 16);
            StartCoroutine(player.Hop(1));
            Session.AddMemory("雪中熱甘酒");
            yield return Say("趣趣", "呼～手終於暖起來了。");
        }

        // ---------------- 梅田：地下街導航 ----------------
        IEnumerator UndergroundCo()
        {
            bool ok = Esim.Spend(0.08f);
            yield return Say("趣趣", ok ? "地下街像迷宮，但導航說 3 分鐘就到出口，順～" : "地下街沒訊號，地圖不動了⋯⋯繞了 15 分鐘才找到出口。");
            Session.AddMemory("梅田地下街探險");
        }

        // ---------------- 新世界：射的小遊戲 ----------------
        IEnumerator ShootingCo()
        {
            yield return Say("老闆", "射的一回 300 日圓！10 秒內射中越多，獎品越好～");
            dialogOn = false;
            shootScore = 0;
            shootEnd = Time.time + 10f;
            NewTarget();
            screen = Page.Shooting;
            while (Time.time < shootEnd) yield return null;
            screen = Page.None;
            string prize = shootScore >= 8 ? "超大招財貓" : shootScore >= 5 ? "昭和風扭蛋" : shootScore >= 2 ? "彈珠汽水" : "參加獎貼紙";
            Sfx.Fanfare();
            Session.AddMemory("射的獎品：" + prize);
            yield return Say("老闆", "射中 " + shootScore + " 個！獎品是「" + prize + "」～");
        }

        void NewTarget()
        {
            shootTarget = new Vector2(UnityEngine.Random.Range(W * 0.5f - 600, W * 0.5f + 600), UnityEngine.Random.Range(260, 820));
            shootTargetBorn = Time.time;
        }

        void DrawShooting()
        {
            Box(new Rect(-10, -10, W + 20, H + 20), new Color(0.12f, 0.1f, 0.2f, 0.55f), 0);
            float left = Mathf.Max(0, shootEnd - Time.time);
            GUI.Label(new Rect(0, 60, W, 70), "射的！　" + shootScore + " 中　·　剩 " + left.ToString("0.0") + " 秒", St(sBanner, 48, Color.white, TextAnchor.MiddleCenter, true));
            float age = Time.time - shootTargetBorn;
            float size = 130 * Mathf.Clamp01(age / 0.15f);
            if (age > 1.3f) NewTarget();
            var c = shootTarget;
            Circle(c, size, Color.white);
            Circle(c, size * 0.75f, TM.Hex("#E2463B"));
            Circle(c, size * 0.5f, Color.white);
            Circle(c, size * 0.25f, TM.Hex("#E2463B"));
            var e = Event.current;
            if (e.type == EventType.MouseDown && Vector2.Distance(e.mousePosition, c) < size * 0.5f)
            {
                shootScore++;
                Sfx.Pop(1f + shootScore * 0.05f);
                NewTarget();
                e.Use();
            }
            GUI.Label(new Rect(0, H - 120, W, 50), "用滑鼠點靶心", St(sSmall, 26, Color.white, TextAnchor.MiddleCenter, true));
        }

        // ---------------- 溫泉：泡湯 ----------------
        IEnumerator SoakCo(Interactable it)
        {
            yield return Choose("要泡溫泉嗎？", new[] { "泡！（一人 NT$600）", "先不要" });
            if (chosenIdx != 0) yield break;
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            Vector3 start = player.transform.position;
            Vector3 pool = it.transform.position + new Vector3(0, -0.35f, 0);
            for (float t = 0; t < 1f; t += Time.deltaTime * 1.5f)
            {
                player.transform.position = Vector3.Lerp(start, pool, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.8f;
                yield return null;
            }
            player.transform.position = pool;
            Fx.Dust(pool + Vector3.up * 0.4f, 20);
            Sfx.Land();
            // 喜歡放空的隊友一起來泡
            var joiners = new List<Teammate>();
            int k = 0;
            foreach (var p in Session.Mates)
            {
                if (p.mate == null) continue;
                if (p.type == Persona.Capybara || p.type == Persona.Taxi || p.type == Persona.Nanny || k == 0)
                {
                    var gp = it.transform.position + Quaternion.Euler(0, 70 + k * 80, 0) * Vector3.forward * 1.6f;
                    p.mate.gatherPoint = gp;
                    p.mate.transform.position = new Vector3(gp.x, -0.35f, gp.z); // 一起跳進池子
                    Fx.Dust(gp + Vector3.up * 0.6f, 12);
                    joiners.Add(p.mate);
                    k++;
                }
            }
            if (cam) cam.close = true;
            soakBorn = Time.time;
            screen = Page.Soak;
            yield return new WaitForSeconds(4.2f);
            screen = Page.None;
            Session.AddMemory("空庭溫泉泡湯");
            Sfx.Fanfare();
            yield return Say("趣趣", "體力全滿！腳也不痛了～" + (joiners.Count > 1 ? "大家都泡得好舒服。" : ""));
            foreach (var j in joiners) { j.gatherPoint = null; var jp = j.transform.position; jp.y = 0f; j.transform.position = jp; }
            Vector3 outPos = it.transform.position + (start - it.transform.position).normalized * 4.2f;
            outPos.y = 0;
            for (float t = 0; t < 1f; t += Time.deltaTime * 1.5f)
            {
                player.transform.position = Vector3.Lerp(pool, outPos, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.8f;
                yield return null;
            }
            player.transform.position = outPos;
            cc.enabled = true;
            if (cam) cam.close = false;
        }

        void DrawSoak()
        {
            float t = Mathf.Clamp01((Time.time - soakBorn) / 4f);
            var r = new Rect(W * 0.5f - 320, 120, 640, 120);
            Shadowed(r, Cream, 30);
            GUI.Label(new Rect(r.x, r.y + 12, r.width, 44), t < 1 ? "泡湯中⋯ 呼～" : "體力全滿！", St(sTitle, 32, Ink, TextAnchor.MiddleCenter, true));
            Box(new Rect(r.x + 40, r.y + 70, r.width - 80, 28), TM.Hex("#EEE8E0"), 14);
            Box(new Rect(r.x + 40, r.y + 70, Mathf.Max(28, (r.width - 80) * t), 28), TM.Hex("#5BC0DE"), 14);
        }
    }
}

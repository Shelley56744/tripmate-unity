using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 第 2、4～9 站的 3D 場景。每站是一塊 60×60 公尺的小場景，彼此相隔很遠，靠轉場傳送。
    /// 出生點在南邊，站點（發光圈）在中間，地標在北邊。
    /// 都是用基本形狀拼的示意版，之後換成正式模型時位置可以沿用。
    /// </summary>
    public static class AreaBuilder
    {
        static readonly Vector3 DefaultSpawn = new Vector3(0, 0, -20);

        public static void BuildAll(Transform parent)
        {
            Usj(parent, new Vector3(200, 0, 0));
            Shinsaibashi(parent, new Vector3(300, 0, 0));
            Castle(parent, new Vector3(400, 0, 0));
            Kuromon(parent, new Vector3(500, 0, 0));
            Shinsekai(parent, new Vector3(600, 0, 0));
            Umeda(parent, new Vector3(700, 0, 0));
            Onsen(parent, new Vector3(800, 0, 0));
        }

        // ---------- 共用 ----------
        static Transform Begin(Transform parent, Vector3 origin, string key, string name, string questName, Color ground,
                               Vector3 quest, Vector3 guide, Vector3[] homes, Vector3[] stamps, bool snowGround = true)
        {
            var area = new Area { key = key, name = name, questName = questName, origin = origin, spawn = DefaultSpawn, quest = quest, guide = guide, homes = homes };
            World.Areas[key] = area;
            TownBuilder.Target = area.shapes;
            var root = TM.Empty("Area_" + key, parent, origin);
            area.root = root;
            TM.Prim(PrimitiveType.Cube, "Ground", root, new Vector3(0, -0.5f, 0), new Vector3(64, 1, 64), ground, keepCollider: true);
            TownBuilder.MapAdd(0, 0, 64, 64, ground, 0);
            // 邊界
            TownBuilder.Wall(root, new Vector3(0, 2, -31), new Vector3(64, 4, 1));
            TownBuilder.Wall(root, new Vector3(0, 2, 31), new Vector3(64, 4, 1));
            TownBuilder.Wall(root, new Vector3(-31, 2, 0), new Vector3(1, 4, 64));
            TownBuilder.Wall(root, new Vector3(31, 2, 0), new Vector3(1, 4, 64));
            // 遠景：四周的樹牆＋高樓剪影，讓場景看起來不是浮在半空
            for (int i = 0; i < 14; i++)
            {
                float x = -28 + i * 4.3f;
                TownBuilder.PineTree(root, new Vector3(x, 0, -28.5f + TownBuilder.R(-1, 1)), TownBuilder.R(0.9f, 1.3f));
                if (i % 2 == 0) TownBuilder.PineTree(root, new Vector3(-28.5f, 0, x), TownBuilder.R(0.9f, 1.3f));
                if (i % 2 == 1) TownBuilder.PineTree(root, new Vector3(28.5f, 0, x), TownBuilder.R(0.9f, 1.3f));
            }
            for (int i = 0; i < 9; i++)
            {
                float h = TownBuilder.R(10, 20);
                TM.Prim(PrimitiveType.Cube, "FarTower", root, new Vector3(-32 + i * 8f, h * 0.5f, TownBuilder.R(36, 42)), new Vector3(TownBuilder.R(4, 7), h, 4), TM.Hex("#5B5878"));
            }
            if (snowGround)
                for (int i = 0; i < 18; i++)
                {
                    var p = new Vector3(TownBuilder.R(-26, 26), 0, TownBuilder.R(-26, 26));
                    if (Mathf.Abs(p.x) < 6) continue;
                    float s = TownBuilder.R(0.8f, 2f);
                    TM.Prim(PrimitiveType.Sphere, "Mound", root, p, new Vector3(s, s * 0.3f, s), Color.white);
                }
            TownBuilder.Snow(root, new Vector3(0, 16f, 0));
            foreach (var s in stamps) Collectible.Create(root, s);
            foreach (var h in Accessories.HiddenSpots)
                if (h.area == key) Collectible.CreateHidden(root, h.local, h.poiId);
            return root;
        }

        static void Path(Transform root, float width, float z0, float z1, Color c)
        {
            Props.PathTiles(root, new Vector3(0, 0, (z0 + z1) * 0.5f), width, z1 - z0, c);
            TownBuilder.MapAdd(0, (z0 + z1) * 0.5f, width, z1 - z0, c, 0);
        }

        static void Person(Transform p, Vector3 pos, Color c, bool phone)
        {
            Props.Npc(p, pos, TownBuilder.R(0, 360), phone);
        }

        static void Shopfront(Transform p, Vector3 pos, bool faceRight, Vector3 size, Color wall, Color glow, string sign)
        {
            Color roof = Color.Lerp(wall, TM.Hex("#3F4E7A"), 0.65f);
            Props.JShop(p, pos, faceRight ? 90f : -90f, new Vector3(size.x, size.y, size.z), roof, sign, glow);
        }

        // ---------- 第 2 站：USJ 樂園 ----------
        static void Usj(Transform parent, Vector3 o)
        {
            var root = Begin(parent, o, "usj", "樂園", "樂園中央廣場", TM.Snow,
                new Vector3(0, 0, 4), new Vector3(0, 0, 7.5f),
                new[] { new Vector3(-10, 0, 8), new Vector3(10, 0, 8), new Vector3(-6, 0, -8), new Vector3(6, 0, -8), new Vector3(-14, 0, 12), new Vector3(14, 0, 10) },
                new[] { new Vector3(-16, 0, 4), new Vector3(16, 0, 2) });
            Path(root, 6, -30, 12, TM.Stone);
            TM.Prim(PrimitiveType.Cylinder, "Plaza", root, new Vector3(0, 0.02f, 4), new Vector3(18, 0.01f, 18), TM.Hex("#E3D6C6"));
            TownBuilder.MapAdd(0, 4, 18, 18, TM.Hex("#E3D6C6"), 9);

            // 入口拱門
            var gate = TM.Empty("Gate", root, new Vector3(0, 0, -13));
            for (int s = -1; s <= 1; s += 2)
            {
                TM.Prim(PrimitiveType.Cylinder, "Pillar", gate, new Vector3(s * 4f, 2.5f, 0), new Vector3(1.2f, 2.5f, 1.2f), TM.Hex("#3F6FB5"), 0, null, true);
                TM.Prim(PrimitiveType.Sphere, "Top", gate, new Vector3(s * 4f, 5.3f, 0), Vector3.one * 1.4f, TM.Hex("#FFC93C"), 0.4f);
            }
            TM.Prim(PrimitiveType.Cube, "Beam", gate, new Vector3(0, 5.2f, 0), new Vector3(9, 1.4f, 0.6f), TM.Hex("#FF6B6B"));
            TM.Text3D("樂園入口", gate, new Vector3(0, 5.2f, -0.35f), 0.9f, Color.white);
            for (int i = 0; i < 9; i++) TM.Prim(PrimitiveType.Sphere, "Bulb", gate, new Vector3(-4 + i, 6.0f, -0.32f), Vector3.one * 0.2f, Color.white, 2f);

            // 摩天輪
            var fw = TM.Empty("FerrisWheel", root, new Vector3(-15, 0, 19));
            TM.Prim(PrimitiveType.Cube, "LegL", fw, new Vector3(-1.2f, 4.5f, 0), new Vector3(0.4f, 9.5f, 0.4f), TM.Hex("#D9DEE5"), 0, new Vector3(0, 0, -10), true);
            TM.Prim(PrimitiveType.Cube, "LegR", fw, new Vector3(1.2f, 4.5f, 0), new Vector3(0.4f, 9.5f, 0.4f), TM.Hex("#D9DEE5"), 0, new Vector3(0, 0, 10), true);
            var wheel = TM.Empty("Wheel", fw, new Vector3(0, 9, -0.4f));
            TM.MeshObj("Rim", wheel, TM.Torus(6.5f, 0.18f, 48, 8), Vector3.zero, TM.Hex("#FF8AD8"), 0.6f, new Vector3(90, 0, 0));
            string[] cab = { "#FF6B6B", "#FFC93C", "#5B8DEF", "#4DD9A0" };
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f;
                TM.Prim(PrimitiveType.Cube, "Spoke", wheel, Vector3.zero, new Vector3(0.12f, 13f, 0.12f), TM.Hex("#D9DEE5"), 0, new Vector3(0, 0, a));
                var pos = Quaternion.Euler(0, 0, a) * Vector3.up * 6.5f;
                TM.MeshObj("Cabin", wheel, TM.RoundedBox(new Vector3(1.1f, 1.1f, 1.1f), 0.3f, 4), pos, TM.Hex(cab[i % cab.Length]), 0.3f);
            }
            wheel.gameObject.AddComponent<Spin>().speed = new Vector3(0, 0, 8);
            TownBuilder.MapAdd(-15, 19, 14, 3, TM.Hex("#FF8AD8"), 1);

            // 雲霄飛車迴圈
            var rc = TM.Empty("Coaster", root, new Vector3(15, 0, 17));
            TM.MeshObj("Loop", rc, TM.Torus(5f, 0.35f, 48, 8), new Vector3(0, 6, 0), TM.Hex("#E2463B"), 0, new Vector3(90, 0, 0));
            for (int i = -3; i <= 3; i++)
                TM.Prim(PrimitiveType.Cylinder, "Post", rc, new Vector3(i * 2.2f, 0.6f, 0), new Vector3(0.35f, 0.6f, 0.35f), TM.Hex("#D9DEE5"), 0, null, true);
            TM.Prim(PrimitiveType.Cube, "Track", rc, new Vector3(0, 1.2f, 0), new Vector3(16, 0.25f, 0.8f), TM.Hex("#E2463B"));
            TownBuilder.MapAdd(15, 17, 16, 3, TM.Hex("#E2463B"), 1);

            TownBuilder.House(root, new Vector3(-13, 0, -4), 90, new Vector3(5, 3.6f, 5), TM.Hex("#FFE0EE"), TM.Hex("#C93C7E"), "爆米花");
            TownBuilder.House(root, new Vector3(13, 0, -4), -90, new Vector3(5, 3.6f, 5), TM.Hex("#CFE3F2"), TM.Hex("#3F6FB5"), "紀念品");
            TownBuilder.BigTree(root, new Vector3(0, 0, 18));
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f + 22.5f;
                TownBuilder.Lamp(root, new Vector3(0, 0, 4) + Quaternion.Euler(0, a, 0) * Vector3.forward * 9.5f, i % 3 == 0);
            }
            // 人潮：大家都在低頭滑手機
            Color[] coats = { TM.Hex("#6A6E8C"), TM.Hex("#8E8598"), TM.Hex("#5B6F8E"), TM.Hex("#7A6B8E") };
            for (int i = 0; i < 18; i++)
            {
                var p = new Vector3(TownBuilder.R(-12, 12), 0, TownBuilder.R(-8, 14));
                if (Mathf.Abs(p.x) < 3.5f && p.z > -2 && p.z < 10) continue;
                Person(root, p, coats[i % coats.Length], true);
            }
            Snowman(root, new Vector3(-7, 0, -6));
            Snowman(root, new Vector3(7, 0, -9));
        }

        static void Snowman(Transform p, Vector3 pos)
        {
            var t = TM.Empty("Snowman", p, pos);
            TM.Prim(PrimitiveType.Sphere, "B", t, new Vector3(0, 0.6f, 0), Vector3.one * 1.2f, Color.white, 0, null, true);
            TM.Prim(PrimitiveType.Sphere, "H", t, new Vector3(0, 1.5f, 0), Vector3.one * 0.8f, Color.white);
            TM.Prim(PrimitiveType.Cylinder, "Hat", t, new Vector3(0, 2.0f, 0), new Vector3(0.6f, 0.2f, 0.6f), TM.Hex("#E2463B"));
            TM.MeshObj("Nose", t, TM.Cone(0.08f, 0.3f, 8), new Vector3(0, 1.5f, -0.4f), TM.Hex("#FF8A3D"), 0, new Vector3(-90, 0, 0));
            TM.Prim(PrimitiveType.Cube, "Scarf", t, new Vector3(0, 1.15f, 0), new Vector3(0.9f, 0.15f, 0.9f), TM.Hex("#3F6FB5"));
        }

        // ---------- 第 4 站：心齋橋筋商店街 ----------
        static void Shinsaibashi(Transform parent, Vector3 o)
        {
            var root = Begin(parent, o, "shinsaibashi", "心齋橋筋商店街", "商店街中段", TM.Hex("#B9B1C2"),
                new Vector3(0, 0, 4), new Vector3(0, 0, 7f),
                new[] { new Vector3(-2.5f, 0, -10), new Vector3(2.5f, 0, -4), new Vector3(-2.5f, 0, 12), new Vector3(2.5f, 0, 18), new Vector3(-2.5f, 0, 20), new Vector3(2.5f, 0, -16) },
                new[] { new Vector3(-3, 0, -14), new Vector3(3, 0, 16) }, false);
            Path(root, 10, -30, 30, TM.Hex("#E8DED0"));

            string[] signs = { "藥妝", "TAX FREE", "古着", "SALE", "電器", "雜貨", "甜點", "鞋店" };
            string[] walls = { "#FFE0EE", "#CFE3F2", "#FBE3A6", "#E9D3F0", "#DDF5EA", "#FFE7CC" };
            string[] glows = { "#FFE2A8", "#BFE6FF", "#FFD6E8", "#E8FFD6" };
            int k = 0;
            for (float z = -24; z <= 24; z += 8f)
            {
                Shopfront(root, new Vector3(-9, 0, z), true, new Vector3(7, 4.6f, 7), TM.Hex(walls[k % walls.Length]), TM.Hex(glows[k % glows.Length]), signs[k % signs.Length]);
                Shopfront(root, new Vector3(9, 0, z + 4), false, new Vector3(7, 4.6f, 7), TM.Hex(walls[(k + 3) % walls.Length]), TM.Hex(glows[(k + 1) % glows.Length]), signs[(k + 4) % signs.Length]);
                k++;
            }
            // 拱廊屋頂：一排排拱架＋吊燈
            for (float z = -26; z <= 26; z += 4f)
            {
                for (int s = -1; s <= 1; s += 2)
                    TM.Prim(PrimitiveType.Cube, "Post", root, new Vector3(s * 5.4f, 3.2f, z), new Vector3(0.25f, 6.4f, 0.25f), TM.Hex("#6A6E8C"));
                TM.Prim(PrimitiveType.Cube, "Beam", root, new Vector3(0, 6.5f, z), new Vector3(11f, 0.25f, 0.25f), TM.Hex("#8E8598"));
                TM.Prim(PrimitiveType.Cube, "BraceL", root, new Vector3(-3.6f, 7.3f, z), new Vector3(4.2f, 0.18f, 0.18f), TM.Hex("#8E8598"), 0, new Vector3(0, 0, 25));
                TM.Prim(PrimitiveType.Cube, "BraceR", root, new Vector3(3.6f, 7.3f, z), new Vector3(4.2f, 0.18f, 0.18f), TM.Hex("#8E8598"), 0, new Vector3(0, 0, -25));
                TM.Prim(PrimitiveType.Sphere, "Light", root, new Vector3(0, 5.6f, z), new Vector3(0.6f, 0.3f, 0.6f), TM.Hex("#FFF2C4"), 2f);
            }
            TM.Prim(PrimitiveType.Cube, "RoofGlass", root, new Vector3(0, 8.4f, 0), new Vector3(11, 0.1f, 56), TM.Hex("#D6E4F5"), 0.2f);
            var banner = TM.Empty("Banner", root, new Vector3(0, 4.6f, 9));
            TM.Prim(PrimitiveType.Cube, "Board", banner, Vector3.zero, new Vector3(5f, 1f, 0.1f), TM.Hex("#E2463B"));
            TM.Text3D("營業至 20:30", banner, new Vector3(0, 0, -0.07f), 0.55f, Color.white);
            var l = new GameObject("ArcadeLight").AddComponent<Light>();
            l.type = LightType.Point; l.range = 14; l.intensity = 1.4f; l.color = TM.Hex("#FFE2A8");
            l.transform.SetParent(root, false); l.transform.localPosition = new Vector3(0, 5, 4);
            Color[] coats = { TM.Hex("#6A6E8C"), TM.Hex("#B8440E"), TM.Hex("#5B6F8E"), TM.Hex("#7A6B8E") };
            for (int i = 0; i < 10; i++)
                Person(root, new Vector3(TownBuilder.R(-4, 4), 0, TownBuilder.R(-24, 24)), coats[i % coats.Length], i % 3 == 0);
        }

        // ---------- 第 5 站：大阪城 ----------
        static void Castle(Transform parent, Vector3 o)
        {
            var root = Begin(parent, o, "castle", "大阪城公園", "天守閣前廣場", TM.Snow,
                new Vector3(0, 0, 1), new Vector3(0, 0, 4f),
                new[] { new Vector3(-12, 0, -6), new Vector3(12, 0, -6), new Vector3(-15, 0, 6), new Vector3(14, 0, 4), new Vector3(-8, 0, -14), new Vector3(8, 0, -14) },
                new[] { new Vector3(-16, 0, 0), new Vector3(17, 0, 10) });
            Path(root, 5, -30, 8, TM.Stone);
            var c = TM.Empty("Castle", root, new Vector3(0, 0, 19));
            // 護城河（四邊）＋石牆
            Color water = TM.Hex("#5B8FE0");
            TM.Prim(PrimitiveType.Cube, "MoatS_L", c, new Vector3(-6.5f, 0.02f, -9.5f), new Vector3(9, 0.05f, 3), water, 0.3f);
            TM.Prim(PrimitiveType.Cube, "MoatS_R", c, new Vector3(6.5f, 0.02f, -9.5f), new Vector3(9, 0.05f, 3), water, 0.3f);
            TM.Prim(PrimitiveType.Cube, "MoatW", c, new Vector3(-9.5f, 0.02f, 0), new Vector3(3, 0.05f, 22), water, 0.3f);
            TM.Prim(PrimitiveType.Cube, "MoatE", c, new Vector3(9.5f, 0.02f, 0), new Vector3(3, 0.05f, 22), water, 0.3f);
            TownBuilder.MapAdd(0, 19, 22, 22, water, 1);
            TM.Prim(PrimitiveType.Cube, "Bridge", c, new Vector3(0, 0.1f, -9.5f), new Vector3(4, 0.2f, 3.2f), TM.Wood);
            foreach (var w in new[] { new Vector3(-6.5f, 0.4f, -11.1f), new Vector3(6.5f, 0.4f, -11.1f) })
                TM.Prim(PrimitiveType.Cube, "MoatWall", c, w, new Vector3(9, 0.8f, 0.3f), TM.Hex("#8E8598"), 0, null, true);
            TM.Prim(PrimitiveType.Cube, "MoatWallW", c, new Vector3(-11.1f, 0.4f, 0), new Vector3(0.3f, 0.8f, 22), TM.Hex("#8E8598"), 0, null, true);
            TM.Prim(PrimitiveType.Cube, "MoatWallE", c, new Vector3(11.1f, 0.4f, 0), new Vector3(0.3f, 0.8f, 22), TM.Hex("#8E8598"), 0, null, true);
            // 石垣
            TM.Prim(PrimitiveType.Cube, "Base1", c, new Vector3(0, 1.2f, 0), new Vector3(15, 2.4f, 15), TM.Hex("#9A928A"), 0, null, true);
            TM.Prim(PrimitiveType.Cube, "Base2", c, new Vector3(0, 3.3f, 0), new Vector3(12.5f, 1.8f, 12.5f), TM.Hex("#A8A096"));
            TownBuilder.MapAdd(0, 19, 15, 15, TM.Hex("#9A928A"), 0.5f);
            // 天守閣三層
            Color wall = TM.Hex("#F4F1EC"), roof = TM.Hex("#3F8F7A"), gold = TM.Hex("#FFC93C");
            float[] widths = { 10f, 7.6f, 5.4f };
            float y = 4.2f;
            for (int i = 0; i < 3; i++)
            {
                float w = widths[i];
                TM.Prim(PrimitiveType.Cube, "Tier", c, new Vector3(0, y + 1.4f, 0), new Vector3(w, 2.8f, w), wall);
                for (int s = -1; s <= 1; s += 2)
                    TM.Prim(PrimitiveType.Cube, "Window", c, new Vector3(s * w * 0.25f, y + 1.5f, -w * 0.5f - 0.02f), new Vector3(1.0f, 0.9f, 0.05f), TM.Ink);
                TM.MeshObj("Roof", c, TM.Cone(w * 0.85f, 2.2f, 4), new Vector3(0, y + 2.6f, 0), roof, 0, new Vector3(0, 45, 0));
                TM.MeshObj("RoofSnow", c, TM.Cone(w * 0.45f, 1.1f, 4), new Vector3(0, y + 3.7f, 0), Color.white, 0, new Vector3(0, 45, 0));
                y += 3.4f;
            }
            TM.Prim(PrimitiveType.Sphere, "GoldL", c, new Vector3(-0.9f, y + 0.6f, 0), Vector3.one * 0.6f, gold, 0.6f);
            TM.Prim(PrimitiveType.Sphere, "GoldR", c, new Vector3(0.9f, y + 0.6f, 0), Vector3.one * 0.6f, gold, 0.6f);
            // 售票口和排隊的人
            TownBuilder.House(root, new Vector3(9, 0, 2), -90, new Vector3(4, 3, 3.5f), TM.Hex("#E9DCC8"), TM.Hex("#3F8F7A"), "售票口");
            for (int i = 0; i < 7; i++) Person(root, new Vector3(6.5f - i * 0.0f, 0, 3.5f - i * 1.1f) + new Vector3(0, 0, 0), TM.Hex("#6A6E8C"), i % 2 == 0);
            // 熱甘酒攤（面向步道）
            Props.Stall(root, new Vector3(8.2f, 0, -10f), -90f, "甘酒", TM.Hex("#E2463B"));
            // 松樹與石燈籠
            for (int i = 0; i < 10; i++)
            {
                var p = new Vector3(TownBuilder.R(-25, 25), 0, TownBuilder.R(-24, 6));
                if (Mathf.Abs(p.x) < 6 || (p.x > 4 && p.x < 12 && p.z > -6)) continue;
                if (p.x > 4 && p.x < 12 && p.z > -14 && p.z < -6) continue;
                if (p.x > -7 && p.x < -1 && p.z > -11 && p.z < -5) continue;
                if ((p - new Vector3(-22, 0, -20)).sqrMagnitude < 9f) continue; // 隱藏印章
                TownBuilder.PineTree(root, p, TownBuilder.R(1f, 1.5f));
            }
            for (int s = -1; s <= 1; s += 2) StoneLantern(root, new Vector3(s * 3.8f, 0, -4));
            TownBuilder.Bench(root, new Vector3(-7, 0, -2), 90);
            TownBuilder.Bench(root, new Vector3(-7, 0, 3), 90);
        }

        static void StoneLantern(Transform p, Vector3 pos)
        {
            var t = TM.Empty("StoneLantern", p, pos);
            TM.Prim(PrimitiveType.Cube, "Foot", t, new Vector3(0, 0.15f, 0), new Vector3(0.8f, 0.3f, 0.8f), TM.Hex("#A8A096"), 0, null, true);
            TM.Prim(PrimitiveType.Cylinder, "Pole", t, new Vector3(0, 0.8f, 0), new Vector3(0.3f, 0.5f, 0.3f), TM.Hex("#A8A096"));
            TM.Prim(PrimitiveType.Cube, "Box", t, new Vector3(0, 1.55f, 0), new Vector3(0.6f, 0.5f, 0.6f), TM.Hex("#B9B1A6"));
            TM.Prim(PrimitiveType.Cube, "Glow", t, new Vector3(0, 1.55f, 0), new Vector3(0.62f, 0.25f, 0.4f), TM.Hex("#FFD27A"), 1.5f);
            TM.MeshObj("Cap", t, TM.Cone(0.6f, 0.45f, 4), new Vector3(0, 1.8f, 0), TM.Hex("#9A928A"), 0, new Vector3(0, 45, 0));
            TM.Prim(PrimitiveType.Sphere, "Snow", t, new Vector3(0, 2.0f, 0), new Vector3(0.5f, 0.15f, 0.5f), Color.white);
        }

        // ---------- 第 6 站：黑門市場 ----------
        static void Kuromon(Transform parent, Vector3 o)
        {
            var root = Begin(parent, o, "kuromon", "黑門市場", "市場中段", TM.Hex("#B9B1C2"),
                new Vector3(0, 0, 4), new Vector3(0, 0, 6.5f),
                new[] { new Vector3(-2.5f, 0, -12), new Vector3(2.5f, 0, -6), new Vector3(-2.5f, 0, 12), new Vector3(2.5f, 0, 18), new Vector3(-2.5f, 0, 22), new Vector3(2.5f, 0, -18) },
                new[] { new Vector3(-3, 0, -14), new Vector3(3, 0, 15) }, false);
            Path(root, 9, -30, 30, TM.Hex("#D8CFC2"));
            string[] signs = { "海鮮", "和牛", "草莓", "海膽", "河豚", "玉子燒", "關東煮", "魚板" };
            Color[] canopies = { TM.Hex("#E2463B"), TM.Hex("#3F6FB5"), TM.Hex("#2F9E62"), TM.Hex("#FF8A3D") };
            int k = 0;
            for (float z = -24; z <= 24; z += 6f)
            {
                TownBuilder.Stall(root, new Vector3(-5.6f, 0, z), -90, signs[k % signs.Length], canopies[k % canopies.Length]);
                TownBuilder.Stall(root, new Vector3(5.6f, 0, z + 3), 90, signs[(k + 3) % signs.Length], canopies[(k + 2) % canopies.Length]);
                k++;
            }
            for (float z = -26; z <= 26; z += 5f)
            {
                TM.Prim(PrimitiveType.Cube, "Roof", root, new Vector3(0, 5.2f, z), new Vector3(16, 0.2f, 4.2f), TM.Hex("#6A6E8C"));
                TownBuilder.Lantern(root, new Vector3(-3.5f, 0, z), true);
                TownBuilder.Lantern(root, new Vector3(3.5f, 0, z + 2.5f), true);
            }
            var gate = TM.Empty("Gate", root, new Vector3(0, 0, -26));
            TM.Prim(PrimitiveType.Cube, "Sign", gate, new Vector3(0, 5.8f, 0), new Vector3(8, 1.4f, 0.3f), TM.Ink);
            TM.Text3D("黑門市場", gate, new Vector3(0, 5.8f, -0.2f), 0.9f, TM.Hex("#FFC93C"));
            var l = new GameObject("MarketLight").AddComponent<Light>();
            l.type = LightType.Point; l.range = 14; l.intensity = 1.2f; l.color = TM.Hex("#FFE2A8");
            l.transform.SetParent(root, false); l.transform.localPosition = new Vector3(0, 4.5f, 2);
            for (int i = 0; i < 12; i++)
                Person(root, new Vector3(TownBuilder.R(-3.5f, 3.5f), 0, TownBuilder.R(-24, 24)), TM.Hex(i % 2 == 0 ? "#6A6E8C" : "#8E8598"), i % 4 == 0);
        }

        // ---------- 第 7 站：新世界・通天閣 ----------
        static void Shinsekai(Transform parent, Vector3 o)
        {
            var root = Begin(parent, o, "shinsekai", "新世界", "通天閣下的路口", TM.Hex("#C9BFB2"),
                new Vector3(0, 0, 4), new Vector3(0, 0, 6.5f),
                new[] { new Vector3(-3, 0, -10), new Vector3(3, 0, -4), new Vector3(-3, 0, 10), new Vector3(3, 0, 12), new Vector3(-6, 0, 14), new Vector3(6, 0, -14) },
                new[] { new Vector3(-3, 0, -16), new Vector3(4, 0, 9) }, false);
            Path(root, 10, -30, 12, TM.Hex("#B9AC9A"));
            // 塔
            var t = TM.Empty("Tower", root, new Vector3(0, 0, 21));
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    TM.Prim(PrimitiveType.Cube, "Leg", t, new Vector3(sx * 2.6f, 4f, sz * 2.6f), new Vector3(0.6f, 8.6f, 0.6f), TM.Hex("#D9DEE5"), 0, new Vector3(sz * 12, 0, -sx * 12), true);
            TM.Prim(PrimitiveType.Cube, "Shaft", t, new Vector3(0, 12, 0), new Vector3(2.6f, 10, 2.6f), TM.Hex("#E3E6EC"));
            TM.Prim(PrimitiveType.Cube, "Deck", t, new Vector3(0, 17.5f, 0), new Vector3(5.5f, 2.6f, 5.5f), TM.Hex("#F4F1EC"));
            TM.Prim(PrimitiveType.Cube, "DeckGlass", t, new Vector3(0, 17.5f, -2.76f), new Vector3(5f, 1.2f, 0.05f), TM.Hex("#BFE6FF"), 0.6f);
            TM.Text3D("通天閣", t, new Vector3(0, 15.6f, -1.4f), 0.8f, TM.Hex("#E2463B"));
            TM.Prim(PrimitiveType.Cube, "Top", t, new Vector3(0, 20f, 0), new Vector3(3.6f, 2f, 3.6f), TM.Hex("#E3E6EC"));
            TM.Prim(PrimitiveType.Cylinder, "Spire", t, new Vector3(0, 23f, 0), new Vector3(0.3f, 2f, 0.3f), TM.Hex("#D9DEE5"));
            var ring = TM.MeshObj("NeonRing", t, TM.Torus(2.2f, 0.18f, 32, 6), new Vector3(0, 21.3f, 0), TM.Hex("#38D6FF"), 2f);
            ring.AddComponent<Spin>().speed = new Vector3(0, 40, 0);
            TownBuilder.MapAdd(0, 21, 8, 8, TM.Hex("#D9DEE5"), 1);
            // 街道兩側的老店＋大招牌
            string[] signs = { "串カツ", "將棋", "射的", "喫茶", "ホルモン", "寿司" };
            string[] neon = { "#FF4FA0", "#FFC93C", "#38D6FF", "#FF7A3D", "#4DFFB0", "#9B5BFF" };
            int k = 0;
            for (float z = -22; z <= 10; z += 8f)
            {
                Shopfront(root, new Vector3(-10, 0, z), true, new Vector3(7, 5.5f, 7), TM.Hex("#5A4E6E"), TM.Hex("#FFE2A8"), signs[k % signs.Length]);
                Shopfront(root, new Vector3(10, 0, z + 4), false, new Vector3(7, 5.5f, 7), TM.Hex("#4E4466"), TM.Hex("#FFD6E8"), signs[(k + 3) % signs.Length]);
                // 霓虹招牌條
                TM.Prim(PrimitiveType.Cube, "NeonL", root, new Vector3(-6.4f, 4.8f, z), new Vector3(0.15f, 0.9f, 5.5f), TM.Hex(neon[k % neon.Length]), 1.5f);
                TM.Prim(PrimitiveType.Cube, "NeonR", root, new Vector3(6.4f, 4.8f, z + 4), new Vector3(0.15f, 0.9f, 5.5f), TM.Hex(neon[(k + 2) % neon.Length]), 1.5f);
                k++;
            }
            // 河豚燈籠（復古街區的大招牌）
            var fugu = TM.Empty("PufferLantern", root, new Vector3(-6.2f, 5.5f, 2));
            TM.Prim(PrimitiveType.Sphere, "Body", fugu, Vector3.zero, new Vector3(1.6f, 1.3f, 1.3f), TM.Hex("#FFE8A3"), 0.6f);
            for (int i = 0; i < 10; i++)
                TM.MeshObj("Spike", fugu, TM.Cone(0.08f, 0.3f, 6), Quaternion.Euler(TownBuilder.R(0, 360), TownBuilder.R(0, 360), 0) * Vector3.up * 0.65f, TM.Hex("#C9A04A"), 0, new Vector3(TownBuilder.R(0, 360), 0, TownBuilder.R(0, 360)));
            fugu.gameObject.AddComponent<Bob>().amp = 0.08f;
            for (int i = 0; i < 12; i++) TownBuilder.Lantern(root, new Vector3(-22 + i * 4f, 0, 14f), true);
            var l = new GameObject("StreetLight").AddComponent<Light>();
            l.type = LightType.Point; l.range = 16; l.intensity = 1.4f; l.color = TM.Hex("#FF9EC2");
            l.transform.SetParent(root, false); l.transform.localPosition = new Vector3(0, 5, -4);
            for (int i = 0; i < 8; i++) Person(root, new Vector3(TownBuilder.R(-4, 4), 0, TownBuilder.R(-22, 10)), TM.Hex("#6A6E8C"), i % 3 == 0);
        }

        // ---------- 第 8 站：梅田 ----------
        static void Umeda(Transform parent, Vector3 o)
        {
            var root = Begin(parent, o, "umeda", "梅田", "雙塔前廣場", TM.Hex("#C9CED6"),
                new Vector3(0, 0, 3), new Vector3(0, 0, 5.5f),
                new[] { new Vector3(-12, 0, -2), new Vector3(12, 0, -6), new Vector3(-6, 0, -12), new Vector3(8, 0, 8), new Vector3(-14, 0, 8), new Vector3(14, 0, 4) },
                new[] { new Vector3(-15, 0, 12), new Vector3(17, 0, -10) }, false);
            Path(root, 8, -30, 14, TM.Hex("#E3E6EC"));
            // 雙塔＋頂樓空中庭園
            for (int s = -1; s <= 1; s += 2)
            {
                var tw = TM.Empty("Tower", root, new Vector3(s * 5.5f, 0, 21));
                TM.Prim(PrimitiveType.Cube, "Body", tw, new Vector3(0, 16, 0), new Vector3(6, 32, 6), TM.Hex("#7FA6C9"), 0, null, true);
                for (int f = 0; f < 14; f++)
                    TM.Prim(PrimitiveType.Cube, "Stripe", tw, new Vector3(0, 2 + f * 2.2f, -3.02f), new Vector3(5.4f, 0.5f, 0.05f), TM.Hex("#DDEBFF"), 0.5f);
                TownBuilder.MapAdd(s * 5.5f, 21, 6, 6, TM.Hex("#7FA6C9"), 0.5f);
            }
            TM.Prim(PrimitiveType.Cylinder, "Garden", root, new Vector3(0, 32.5f, 21), new Vector3(15, 0.5f, 9), TM.Hex("#E3E6EC"));
            var gr = TM.MeshObj("GardenRing", root, TM.Torus(7.2f, 0.3f, 48, 6), new Vector3(0, 33f, 21), TM.Hex("#FFC93C"), 1.4f, null, new Vector3(1, 1, 0.6f));
            gr.AddComponent<Spin>().speed = new Vector3(0, 6, 0);
            TM.Text3D("空中庭園", root, new Vector3(0, 30.5f, 16.4f), 1.2f, Color.white);
            // 百貨公司與地下街入口
            var dept = TownBuilder.Building(root, new Vector3(-15, 0, 2), new Vector3(9, 12, 10), TM.Hex("#E9D3F0"));
            TM.Prim(PrimitiveType.Cube, "Sale", dept, new Vector3(0, 5f, -5.1f), new Vector3(5, 1.6f, 0.1f), TM.Hex("#E2463B"), 0.6f);
            TM.Text3D("冬季特賣 SALE", dept, new Vector3(0, 5f, -5.2f), 0.6f, Color.white);
            var under = TM.Empty("Underground", root, new Vector3(13, 0, -2));
            TM.Prim(PrimitiveType.Cube, "Frame", under, new Vector3(0, 1.6f, 0), new Vector3(5, 3.2f, 4), TM.Hex("#3F6FB5"), 0, null, true);
            TM.Prim(PrimitiveType.Cube, "Door", under, new Vector3(0, 1.2f, -2.02f), new Vector3(3.2f, 2.2f, 0.05f), TM.Hex("#FFE2A8"), 1f);
            TM.Text3D("地下街", under, new Vector3(0, 2.8f, -2.1f), 0.55f, Color.white);
            TownBuilder.MapAdd(13, -2, 5, 4, TM.Hex("#3F6FB5"), 0.5f);
            TownBuilder.Building(root, new Vector3(16, 0, 14), new Vector3(8, 16, 8), TM.Hex("#9AA8C9"));
            TownBuilder.Building(root, new Vector3(-16, 0, 18), new Vector3(8, 20, 8), TM.Hex("#8E9CBD"));
            // 噴水池
            TM.Prim(PrimitiveType.Cylinder, "Pool", root, new Vector3(0, 0.3f, -7), new Vector3(6, 0.3f, 6), TM.Hex("#B9B1A6"), 0, null, true);
            TM.Prim(PrimitiveType.Cylinder, "Water", root, new Vector3(0, 0.62f, -7), new Vector3(5.2f, 0.02f, 5.2f), TM.Hex("#7CC6FE"), 0.6f);
            var spout = TM.Prim(PrimitiveType.Sphere, "Spout", root, new Vector3(0, 1.5f, -7), new Vector3(0.6f, 1.4f, 0.6f), TM.Hex("#CFEFFF"), 0.8f);
            spout.AddComponent<Bob>().amp = 0.2f;
            TownBuilder.MapAdd(0, -7, 6, 6, TM.Hex("#7CC6FE"), 3);
            for (int i = 0; i < 10; i++)
            {
                var p = new Vector3(TownBuilder.R(-10, 10), 0, TownBuilder.R(-18, 12));
                if (Mathf.Abs(p.x) < 3.5f && p.z > -11 && p.z < 7) continue;
                Person(root, p, TM.Hex(i % 2 == 0 ? "#6A6E8C" : "#5B6F8E"), true);
            }
            for (int i = 0; i < 6; i++) TownBuilder.Lamp(root, new Vector3(i % 2 == 0 ? -5 : 5, 0, -18 + i * 5), i % 2 == 0);
        }

        // ---------- 第 9 站：溫泉 ----------
        static void Onsen(Transform parent, Vector3 o)
        {
            var root = Begin(parent, o, "onsen", "空庭溫泉", "湯屋門口", TM.Snow,
                new Vector3(0, 0, 5), new Vector3(0, 0, 8f),
                new[] { new Vector3(-10, 0, 8), new Vector3(10, 0, 6), new Vector3(-6, 0, -8), new Vector3(7, 0, -6), new Vector3(-13, 0, -2), new Vector3(13, 0, 12) },
                new[] { new Vector3(-17, 0, 2), new Vector3(16, 0, 14) });
            Path(root, 5, -30, 12, TM.Stone);
            // 湯屋
            var h = TM.Empty("Bathhouse", root, new Vector3(0, 0, 19));
            TM.Prim(PrimitiveType.Cube, "Body", h, new Vector3(0, 2.6f, 0), new Vector3(16, 5.2f, 8), TM.Hex("#7A5A43"), 0, null, true);
            TM.MeshObj("Roof", h, TM.Cone(12.5f, 3.6f, 4), new Vector3(0, 5.1f, 0), TM.Hex("#3A3352"), 0, new Vector3(0, 45, 0), new Vector3(1, 1, 0.55f));
            TM.MeshObj("RoofSnow", h, TM.Cone(6f, 1.7f, 4), new Vector3(0, 7.1f, 0), Color.white, 0, new Vector3(0, 45, 0), new Vector3(1, 1, 0.55f));
            TownBuilder.MapAdd(0, 19, 16, 8, TM.Hex("#7A5A43"), 0.5f);
            Color[] noren = { TM.Hex("#3F6FB5"), TM.Hex("#E2463B") };
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -2.2f : 2.2f;
                for (int k = 0; k < 3; k++)
                    TM.Prim(PrimitiveType.Cube, "Noren", h, new Vector3(x - 0.6f + k * 0.6f, 2.6f, -4.06f), new Vector3(0.55f, 1.4f, 0.04f), noren[i]);
                TM.Text3D("ゆ", h, new Vector3(x, 2.8f, -4.12f), 0.7f, Color.white);
                TM.Prim(PrimitiveType.Cube, "Door", h, new Vector3(x, 1.0f, -4.03f), new Vector3(1.8f, 2f, 0.03f), TM.Hex("#FFE2A8"), 0.8f);
            }
            var sign = TM.Empty("Sign", h, new Vector3(0, 4.6f, -4.15f));
            TM.Prim(PrimitiveType.Cube, "Board", sign, Vector3.zero, new Vector3(5, 0.9f, 0.1f), TM.Hex("#3A2A20"));
            TM.Text3D("空庭溫泉", sign, new Vector3(0, 0, -0.07f), 0.55f, TM.Hex("#FFC93C"));
            // 露天池＋熱氣
            var pool = TM.Empty("OutdoorBath", root, new Vector3(-13, 0, 11));
            TM.Prim(PrimitiveType.Cylinder, "Rim", pool, new Vector3(0, 0.3f, 0), new Vector3(7, 0.3f, 5), TM.Hex("#8E8598"), 0, null, true);
            TM.Prim(PrimitiveType.Cylinder, "Water", pool, new Vector3(0, 0.62f, 0), new Vector3(6.2f, 0.02f, 4.2f), TM.Hex("#9FE6F0"), 0.7f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f;
                TM.Prim(PrimitiveType.Sphere, "Rock", pool, Quaternion.Euler(0, a, 0) * new Vector3(3.4f, 0.5f, 0) + new Vector3(0, 0, 0), new Vector3(1.1f, 0.8f, 0.9f), TM.Hex("#6A6E8C"));
            }
            Steam(pool, new Vector3(0, 0.8f, 0));
            TownBuilder.MapAdd(-13, 11, 7, 5, TM.Hex("#9FE6F0"), 2.5f);
            // 竹籬笆、石燈籠、自動販賣機、長椅
            for (int i = 0; i < 16; i++)
                TM.Prim(PrimitiveType.Cylinder, "Bamboo", root, new Vector3(-20 + i * 0.5f, 1.1f, 15.5f), new Vector3(0.18f, 1.1f, 0.18f), TM.Hex("#7FB06B"));
            StoneLantern(root, new Vector3(-3.5f, 0, 1));
            StoneLantern(root, new Vector3(3.5f, 0, 1));
            var vm = TM.Empty("Vending", root, new Vector3(8.5f, 0, 13.5f));
            TM.Prim(PrimitiveType.Cube, "Body", vm, new Vector3(0, 1, 0), new Vector3(1.4f, 2f, 0.9f), TM.Hex("#E2463B"), 0, null, true);
            TM.Prim(PrimitiveType.Cube, "Panel", vm, new Vector3(0, 1.2f, -0.46f), new Vector3(1.1f, 1.1f, 0.02f), TM.Hex("#FFF2C4"), 1.2f);
            TownBuilder.Bench(root, new Vector3(6, 0, 1), 0);
            TownBuilder.Bench(root, new Vector3(-7, 0, 3), 30);
            for (int i = 0; i < 4; i++) Person(root, new Vector3(TownBuilder.R(-8, 8), 0, TownBuilder.R(-14, 2)), TM.Hex("#6A6E8C"), false);
        }

        static void Steam(Transform p, Vector3 local)
        {
            var go = new GameObject("Steam");
            go.transform.SetParent(p, false);
            go.transform.localPosition = local;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main;
            m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.5f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            m.startColor = new Color(1, 1, 1, 0.35f);
            m.gravityModifier = -0.05f;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 14f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(5f, 0.1f, 3.5f);
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.4f));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = TM.ParticleMat();
            ps.Play();
        }
    }
}

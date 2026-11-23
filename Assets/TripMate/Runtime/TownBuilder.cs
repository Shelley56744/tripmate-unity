using System.Collections.Generic;
using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 用程式蓋出第一版地圖：冬日小鎮廣場 → 北邊小路 → 運河與橋 → 道頓堀霓虹街。
    /// 座標：+Z 是北（鏡頭預設面向北）。廣場中心在 (0,0,0)。
    /// 之後換成正式 3D 模型時，只要把對應的方法換成擺放 Prefab 即可，位置可以沿用。
    /// </summary>
    public static class TownBuilder
    {
        public static readonly Vector3 QuestPoint = new Vector3(0, 0, 24f);
        public static readonly Vector3 SpawnPoint = new Vector3(0, 0, -5f);

        static readonly List<Vector3> blocked = new List<Vector3>();

        /// <summary>小地圖用的色塊（世界座標 x/z 的矩形）。</summary>
        public struct MapShape { public float x0, z0, x1, z1, radius; public Color color; }
        public static readonly List<MapShape> MapShapes = new List<MapShape>();
        public const float MapXMin = -38f, MapXMax = 38f, MapZMin = -38f, MapZMax = 46f;

        /// <summary>MapAdd 寫進哪一份小地圖（蓋其他站點時會切換）。</summary>
        public static List<MapShape> Target = MapShapes;

        internal static void MapAdd(float cx, float cz, float w, float d, Color c, float radius = 2f)
        {
            Target.Add(new MapShape { x0 = cx - w * 0.5f, z0 = cz - d * 0.5f, x1 = cx + w * 0.5f, z1 = cz + d * 0.5f, color = c, radius = radius });
        }

        /// <summary>旅遊印章的位置（廣場、小路、橋上、道頓堀）。</summary>
        public static readonly Vector3[] StampPoints =
        {
            new Vector3(-7, 0, 8), new Vector3(9, 0, 2), new Vector3(0, 0, -9),
            new Vector3(-6, 0, -12), new Vector3(6, 0, -12), new Vector3(14, 0, -4),
            new Vector3(-3.3f, 0, 13), new Vector3(3.3f, 0, 18), new Vector3(-12, 0, 20),
            new Vector3(0, 0, 30), new Vector3(-10, 0, 35.5f), new Vector3(10, 0, 35.5f),
        };

        /// <summary>從出生點到道頓堀的發光引導路線。</summary>
        public static readonly Vector3[] GuidePoints =
        {
            new Vector3(0.6f, 0, -3.4f), new Vector3(2.7f, 0, -1.5f), new Vector3(3.0f, 0, 1.2f),
            new Vector3(1.8f, 0, 4.2f), new Vector3(0, 0, 6.5f), new Vector3(0, 0, 21.5f),
        };
        static System.Random rng;

        internal static float R(float a, float b) { if (rng == null) rng = new System.Random(7); return a + (float)rng.NextDouble() * (b - a); }

        public static Transform Build(Transform parent)
        {
            rng = new System.Random(7);
            blocked.Clear();
            MapShapes.Clear();
            blocked.AddRange(StampPoints);
            foreach (var h in Accessories.HiddenSpots) if (h.area == "hotel" || h.area == "dotonbori") blocked.Add(h.local);
            foreach (var pp in GameData.Order) blocked.Add(HotelHome(pp)); // 旅伴據點不要長樹
            Target = MapShapes;
            // 小地圖底色：南邊雪地、道頓堀街、運河、橋、廣場、小路
            MapAdd(0, -6.5f, 80, 67, TM.Hex("#F4F1EC"), 0);
            MapAdd(0, 40f, 80, 14, TM.Hex("#5D5A73"), 0);
            MapAdd(0, 30f, 80, 6, TM.Hex("#5B8FE0"), 0);
            MapAdd(0, 30f, 3.2f, 7.4f, TM.Hex("#C8553D"), 0);
            MapAdd(0, 0, 19, 19, TM.Stone, 9.5f);
            MapAdd(0, 16f, 3.2f, 18f, TM.Stone, 0);
            var root = TM.Empty("Town", parent, Vector3.zero);

            Ground(root);
            Plaza(TM.Empty("Plaza", root, Vector3.zero));
            Path(TM.Empty("Path", root, Vector3.zero));
            Canal(TM.Empty("Canal", root, Vector3.zero));
            Dotonbori(TM.Empty("Dotonbori", root, Vector3.zero));
            Trees(TM.Empty("Trees", root, Vector3.zero));
            Snow(root, new Vector3(0, 16f, 12f));
            Borders(TM.Empty("Borders", root, Vector3.zero));
            RegisterAreas(root);
            return root;
        }

        /// <summary>小鎮裡的兩個站點：飯店（第 1、10 站）和道頓堀（第 3 站）。</summary>
        static void RegisterAreas(Transform root)
        {
            var hotel = new Area
            {
                key = "hotel", name = "飯店前廣場", questName = "行李箱旅店門口",
                origin = Vector3.zero, spawn = SpawnPoint, quest = new Vector3(-8.5f, 0, 1.5f), guide = new Vector3(-11.2f, 0, 2f),
                homes = new[] { new Vector3(-4, 0, -4), new Vector3(5, 0, 3), new Vector3(-5, 0, 4), new Vector3(8, 0, -8), new Vector3(-8, 0, -8), new Vector3(3, 0, -9) },
                xMin = MapXMin, xMax = MapXMax, zMin = MapZMin, zMax = MapZMax, shapes = MapShapes,
            };
            var dot = new Area
            {
                key = "dotonbori", name = "道頓堀", questName = "道頓堀橋頭",
                origin = Vector3.zero, spawn = new Vector3(0, 0, 13f), quest = QuestPoint, guide = new Vector3(0, 0, 26.6f),
                homes = new[] { new Vector3(-4, 0, 20), new Vector3(4, 0, 19), new Vector3(-9, 0, 24), new Vector3(9, 0, 24), new Vector3(-2.5f, 0, 16), new Vector3(2.5f, 0, 22) },
                xMin = MapXMin, xMax = MapXMax, zMin = MapZMin, zMax = MapZMax, shapes = MapShapes,
            };
            hotel.root = root;
            dot.root = root;
            World.Areas["hotel"] = hotel;
            World.Areas["dotonbori"] = dot;
        }

        /// <summary>第 1 站「晨間集合」：每種人格一早在廣場做自己的事的位置。</summary>
        public static Vector3 HotelHome(Persona p)
        {
            switch (p)
            {
                case Persona.Soldier: return new Vector3(6.5f, 0, 3.2f);      // 鐘塔前盯著時間
                case Persona.Capybara: return new Vector3(-4.2f, 0, -4.3f);   // 長椅旁放空
                case Persona.Camera: return new Vector3(-5.5f, 0, 2.6f);      // 公告欄附近找角度
                case Persona.Shopper: return new Vector3(11f, 0, 2.5f);       // 紀念品店門口
                case Persona.Accountant: return new Vector3(8.5f, 0, -9f);    // 車站前查票價
                case Persona.Explorer: return new Vector3(-9f, 0, -10.5f);    // 咖啡館旁的小巷
                case Persona.Taxi: return new Vector3(10.5f, 0, -11.5f);      // 車站前等車
                case Persona.Nanny: return new Vector3(0f, 0, -8.5f);         // 拱門前點名
            }
            return Vector3.zero;
        }

        // ---------- 光線與氣氛：傍晚、冷色天空＋暖色燈 ----------
        static void Lighting()
        {
            var sun = new GameObject("Sun (Dusk)").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = TM.Hex("#FFD9B0");
            sun.intensity = 0.85f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
            sun.transform.rotation = Quaternion.Euler(38f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = TM.Hex("#9EA3E0");
            RenderSettings.ambientEquatorColor = TM.Hex("#D9B3C4");
            RenderSettings.ambientGroundColor = TM.Hex("#6B6080");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = TM.Hex("#3A3F7A");
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 75f;
        }

        // ---------- 地面（運河那段挖空） ----------
        static void Ground(Transform root)
        {
            // 南邊：z -40 ~ 27
            TM.Prim(PrimitiveType.Cube, "Ground_South", root, new Vector3(0, -0.5f, -6.5f), new Vector3(80, 1, 67), TM.Snow, keepCollider: true);
            // 北邊：z 33 ~ 70（道頓堀，石板路比較深色）
            TM.Prim(PrimitiveType.Cube, "Ground_North", root, new Vector3(0, -0.5f, 51.5f), new Vector3(80, 1, 37), TM.Hex("#5D5A73"), keepCollider: true);
            // 道頓堀路面上的積雪邊
            TM.Prim(PrimitiveType.Cube, "SnowEdge_N", root, new Vector3(0, 0.01f, 34f), new Vector3(80, 0.02f, 1.2f), TM.Snow);

            // 地上零星的雪堆
            var mounds = TM.Empty("SnowMounds", root, Vector3.zero);
            for (int i = 0; i < 40; i++)
            {
                var p = new Vector3(R(-36, 36), 0, R(-36, 22));
                if (p.magnitude < 11 || (Mathf.Abs(p.x) < 3 && p.z > 5)) continue;
                float s = R(0.8f, 2.2f);
                TM.Prim(PrimitiveType.Sphere, "Mound", mounds, p, new Vector3(s, s * 0.3f, s * R(0.7f, 1.2f)), Color.white);
            }
        }

        // ---------- 小鎮廣場 ----------
        static void Plaza(Transform p)
        {
            // 石板圓形廣場＋外圈
            TM.Prim(PrimitiveType.Cylinder, "PlazaStone", p, new Vector3(0, 0.01f, 0), new Vector3(18, 0.01f, 18), TM.Stone);
            TM.MeshObj("PlazaRing", p, TM.Torus(9f, 0.12f, 64, 6), new Vector3(0, 0.03f, 0), TM.Hex("#B9AC9A"), 0, null, new Vector3(1, 0.3f, 1));

            // 中央：掛燈飾的大雪松（集合點）
            BigTree(p, Vector3.zero);
            // 樹下圓椅
            TM.MeshObj("TreeBench", p, TM.Torus(1.7f, 0.25f, 32, 8), new Vector3(0, 0.3f, 0), TM.Wood, 0, null, new Vector3(1, 0.8f, 1));

            // 鐘塔（右後）
            ClockTower(p, new Vector3(6.5f, 0, 5.5f));
            // 公告欄（左後）
            NoticeBoard(p, new Vector3(-6.5f, 0, 5f), "旅伴公告欄", new Vector3(0, 20, 0));

            // 路燈：廣場外圈，避開南北兩條路
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f + 22.5f;
                var pos = Quaternion.Euler(0, a, 0) * Vector3.forward * 8.6f;
                Lamp(p, pos, i % 2 == 0);
            }
            // 長椅
            Bench(p, new Vector3(-4.5f, 0, -5.5f), 40);
            // 去趣 eSIM 攤位（出生點旁邊，一出門就看得到）
            Props.EsimBooth(p, new Vector3(4.8f, 0, -5.8f), -60f);

            // 廣場周圍的小房子（門都朝向廣場）
            House(p, new Vector3(-15f, 0, 2f), 90, new Vector3(6, 4, 5), TM.Hex("#F6C9A8"), TM.Hex("#C8553D"), "行李箱旅店");
            House(p, new Vector3(15f, 0, 2f), -90, new Vector3(5, 3.6f, 5), TM.Hex("#CFE3F2"), TM.Hex("#3F6FB5"), "紀念品店");
            House(p, new Vector3(-12f, 0, -13f), 45, new Vector3(5, 3.4f, 4.5f), TM.Hex("#FBE3A6"), TM.Hex("#7A9E5B"), "咖啡館");
            House(p, new Vector3(12f, 0, -13f), -45, new Vector3(5, 3.4f, 4.5f), TM.Hex("#E9D3F0"), TM.Hex("#8C5BB5"), "車站");

            // 廣場南邊的入口拱門
            Arch(p, new Vector3(0, 0, -10.5f), "TripMate 小鎮");
        }

        // ---------- 往北的小路 ----------
        static void Path(Transform p)
        {
            Props.PathTiles(p, new Vector3(0, 0, 17f), 3.2f, 18f, TM.Stone);
            for (int i = 0; i < 9; i++)
            {
                // 路邊小石頭
                TM.Prim(PrimitiveType.Sphere, "PathPebbleL", p, new Vector3(-1.9f, 0.05f, 9 + i * 2f), new Vector3(0.35f, 0.15f, 0.35f), TM.Hex("#B9AC9A"));
                TM.Prim(PrimitiveType.Sphere, "PathPebbleR", p, new Vector3(1.9f, 0.05f, 10 + i * 2f), new Vector3(0.35f, 0.15f, 0.35f), TM.Hex("#B9AC9A"));
            }
            Signpost(p, new Vector3(2.6f, 0, 11f), "↑ 道頓堀");
            Lamp(p, new Vector3(-2.4f, 0, 15f), true);
            Lamp(p, new Vector3(2.4f, 0, 20f), false);

            // 橋頭前的章魚燒攤（照參考圖：攤位、老闆、大紅燈籠、立牌、排隊的人）
            var tako = Props.Stall(p, new Vector3(-6f, 0, 22.4f), 160f, "たこ燒", TM.Hex("#E2463B"));
            blocked.Add(new Vector3(-6f, 0, 22.4f));
            Props.Npc(tako, new Vector3(0.2f, 0, -0.8f), 0, false, 8);
            TM.Prim(PrimitiveType.Cube, "Griddle", tako, new Vector3(0.5f, 1.12f, 0.2f), new Vector3(1.1f, 0.08f, 0.55f), TM.Hex("#2A2A30"));
            for (int i = 0; i < 8; i++)
                TM.Prim(PrimitiveType.Sphere, "Takoyaki", tako, new Vector3(0.1f + (i % 4) * 0.26f, 1.19f, 0.07f + (i / 4) * 0.26f), Vector3.one * 0.17f, TM.Hex("#C9803A"));
            Props.BigLantern(tako, new Vector3(-1.85f, 1.75f, 0.85f), "たこ燒");
            var menu = TM.Empty("MenuBoard", tako, new Vector3(2.0f, 0, 1.3f));
            TM.Prim(PrimitiveType.Cube, "BoardF", menu, new Vector3(0, 0.5f, 0.12f), new Vector3(0.66f, 0.95f, 0.04f), TM.Hex("#6E4A30"), 0, new Vector3(12, 0, 0));
            TM.Prim(PrimitiveType.Cube, "BoardB", menu, new Vector3(0, 0.5f, -0.12f), new Vector3(0.66f, 0.95f, 0.04f), TM.Hex("#6E4A30"), 0, new Vector3(-12, 0, 0));
            TM.Prim(PrimitiveType.Cube, "Poster", menu, new Vector3(0, 0.55f, 0.15f), new Vector3(0.54f, 0.7f, 0.01f), TM.Hex("#E2463B"), 0.4f, new Vector3(12, 0, 0));
            TM.Text3D("8個\n¥600", menu, new Vector3(0, 0.55f, 0.17f), 0.17f, Color.white, new Vector3(12, 180, 0));
            Props.Npc(tako, new Vector3(0.3f, 0, 1.8f), 180, true, 0, true);
            Props.Npc(tako, new Vector3(0.1f, 0, 2.7f), 180, false, 1);
            Props.Npc(tako, new Vector3(0.45f, 0, 3.5f), 180, false, 7, true);
            Stall(p, new Vector3(8.5f, 0, 22.6f), 20, "甘酒", TM.Hex("#3F6FB5"));

            // 南岸河濱步道：濕亮的石板、面向河的長椅
            MapAdd(0, 25.6f, 76, 2.6f, TM.Hex("#C9BFB2"), 0);
            Props.PathTiles(p, new Vector3(0, 0, 25.6f), 74f, 2.6f, TM.Hex("#B9B0A4"), true);
            foreach (var bx in new[] { -20f, -11f, 12f, 21f })
            {
                Bench(p, new Vector3(bx, 0, 25.4f), 180);
                blocked.Add(new Vector3(bx, 0, 25.4f));
            }
            Props.Npc(p, new Vector3(-15.5f, 0, 25.2f), 20, false, 3, true);
            Props.Npc(p, new Vector3(-14.4f, 0, 25.5f), -30, false, 4);
            Props.Npc(p, new Vector3(16f, 0, 25.3f), 180, true, 2, true);
            Props.Npc(p, new Vector3(25f, 0, 25f), 90, false, 5);
            Props.Npc(p, new Vector3(26.2f, 0, 25.3f), -90, false, 6);
        }

        // ---------- 運河＋橋（照參考圖：石砌河岸、石柱燈、木欄杆、花台、石拱橋、觀光船）----------
        static void Canal(Transform p)
        {
            TM.Prim(PrimitiveType.Cube, "CanalBed", p, new Vector3(0, -1.6f, 30f), new Vector3(80, 0.4f, 6f), TM.Hex("#1E2246"), keepCollider: true);
            TM.Prim(PrimitiveType.Cube, "Water", p, new Vector3(0, -0.7f, 30f), new Vector3(80, 0.05f, 6f), TM.Hex("#2E4F9A"), 0.35f).GetComponent<Renderer>().sharedMaterial = TM.Mat(TM.Hex("#2E4F9A"), 0.3f, 0.95f);

            // 石砌河岸：一塊塊深淺不同的石塊＋淺色壓頂石
            Color[] blocks = { TM.Hex("#9E968C"), TM.Hex("#8E8680"), TM.Hex("#A8A096") };
            foreach (var wz in new[] { 27.1f, 32.9f })
            {
                float face = wz < 30 ? 0.12f : -0.12f;
                TM.Prim(PrimitiveType.Cube, "Wall", p, new Vector3(0, -0.75f, wz), new Vector3(80, 1.5f, 0.2f), TM.Hex("#6E6878"));
                int k = 0;
                for (float x = -39.2f; x < 40f; x += 1.6f, k++)
                    for (int row = 0; row < 2; row++)
                    {
                        float off = row == 0 ? 0 : 0.8f;
                        TM.Prim(PrimitiveType.Cube, "Block", p, new Vector3(x + off, -0.36f - row * 0.62f, wz + face), new Vector3(1.52f, 0.56f, 0.06f), blocks[(k + row * 2) % blocks.Length]);
                    }
                TM.Prim(PrimitiveType.Cube, "Coping", p, new Vector3(0, 0.03f, wz), new Vector3(80, 0.08f, 0.5f), TM.Hex("#D8CFC2"));
            }

            // 木欄杆＋石柱燈（橋的位置留缺口），南岸加花台
            Props.WoodRail(p, 26.95f, -38f, -1.8f);
            Props.WoodRail(p, 26.95f, 1.8f, 38f);
            Props.WoodRail(p, 33.05f, -38f, -1.8f);
            Props.WoodRail(p, 33.05f, 1.8f, 38f);
            for (float x = -36f; x <= 36.1f; x += 4.5f)
            {
                if (Mathf.Abs(x) < 2.5f) continue;
                Props.StonePost(p, new Vector3(x, 0, 26.95f));
                Props.StonePost(p, new Vector3(x, 0, 33.05f));
                if (Mathf.Abs(x) > 5f && x < 35f) Props.Planter(p, new Vector3(x + 2.25f, 0, 26.45f), 0, 2.6f);
            }

            // 石拱橋：平的橋面（好走）＋兩側拱圈、石欄、四角石柱燈
            var bridge = TM.Empty("Bridge", p, new Vector3(0, 0, 30f));
            Color stone = TM.Hex("#B9B0A4");
            TM.Prim(PrimitiveType.Cube, "Deck", bridge, new Vector3(0, -0.12f, 0), new Vector3(3.4f, 0.25f, 7.6f), stone, keepCollider: true);
            Props.PathTiles(bridge, new Vector3(0, 0.01f, 0), 2.8f, 7.4f, TM.Hex("#C9BFB2"), true);
            foreach (var x in new[] { -1.5f, 1.5f })
                TM.MeshObj("Arch", bridge, TM.Torus(3.4f, 0.32f, 40, 8), new Vector3(x, -1.6f, 0), TM.Hex("#A8A096"), 0, new Vector3(0, 0, 90), new Vector3(0.4f, 1, 1));
            for (int side = -1; side <= 1; side += 2)
            {
                TM.Prim(PrimitiveType.Cube, "Parapet", bridge, new Vector3(side * 1.6f, 0.4f, 0), new Vector3(0.3f, 0.8f, 7.2f), stone, keepCollider: true);
                TM.Prim(PrimitiveType.Cube, "ParapetCap", bridge, new Vector3(side * 1.6f, 0.84f, 0), new Vector3(0.4f, 0.08f, 7.3f), TM.Hex("#D8CFC2"));
                TM.Prim(PrimitiveType.Cube, "ParapetSnow", bridge, new Vector3(side * 1.6f, 0.9f, 0), new Vector3(0.34f, 0.05f, 7.2f), Color.white);
                for (int k = -1; k <= 1; k += 2) Props.StonePost(bridge, new Vector3(side * 1.6f, 0, k * 3.8f));
            }
            Lamp(p, new Vector3(-2.6f, 0, 26.2f), true);
            Lamp(p, new Vector3(2.6f, 0, 33.8f), true);

            // 觀光船（在橋東邊來回開）
            Props.TourBoat(p, new Vector3(18f, -0.5f, 30.2f), 6f, 34f);

            // 水面上的霓虹倒影
            string[] reflect = { "#FF4FA0", "#38D6FF", "#FFC93C", "#E2463B" };
            for (int i = 0; i < 14; i++)
            {
                var c = TM.Hex(reflect[i % reflect.Length]);
                TM.Prim(PrimitiveType.Cube, "Reflection", p, new Vector3(-30 + i * 4.4f + R(-1, 1), -0.66f, 31.2f + R(-0.8f, 0.8f)), new Vector3(0.25f, 0.01f, R(1.2f, 2.6f)), c, 1.2f);
            }
        }

        // ---------- 道頓堀霓虹街（照參考圖：直式招牌、雪屋簷、條紋遮棚、紅燈籠、撐傘的路人）----------
        static void Dotonbori(Transform p)
        {
            string[] signs = { "ラーメン", "たこ燒", "串カツ", "居酒屋", "お好み燒", "カニ" };
            string[] walls = { "#4A3F5E", "#56495F", "#3E3858", "#5E4A5A", "#463E5C", "#524460" };
            Props.PathTiles(p, new Vector3(0, 0, 36f), 74f, 4f, TM.Hex("#6E6878"), true);

            float x = -26f;
            int idx = 0;
            while (x < 26f)
            {
                float w = R(4.5f, 6.5f);
                float h = R(6f, 11f);
                if (x + w > -2.6f && x < 2.6f) // 橋出口留一條街
                {
                    if (-2.6f - x >= 3f) w = -2.6f - x;
                    else { x = 2.6f; continue; }
                }
                var c = new Vector3(x + w * 0.5f, 0, 41f);
                var b = Building(p, c, new Vector3(w - 0.3f, h, 6f), TM.Hex(walls[idx % walls.Length]));
                string sign = signs[idx % signs.Length];
                float hs = sign.Length * 0.85f + 0.5f;
                Color sc = VerticalSign(b, new Vector3(w * 0.5f - 0.75f, 3.4f + hs * 0.5f, -3.3f), sign);
                // 一樓店面：暖光、木格、雪屋簷、紅白條紋遮棚、紅燈籠
                TM.Prim(PrimitiveType.Cube, "ShopGlow", b, new Vector3(0, 1.2f, -3.02f), new Vector3(w - 1.2f, 1.6f, 0.05f), TM.Hex("#FFE2A8"), 0.9f);
                for (float gx = -w * 0.5f + 1.2f; gx < w * 0.5f - 0.8f; gx += 0.8f)
                    TM.Prim(PrimitiveType.Cube, "Lattice", b, new Vector3(gx, 1.2f, -3.06f), new Vector3(0.05f, 1.6f, 0.02f), TM.Hex("#6E4A30"));
                int stripes = Mathf.Max(4, Mathf.RoundToInt((w - 0.8f) / 0.5f));
                float sw = (w - 0.8f) / stripes;
                for (int i = 0; i < stripes; i++)
                    TM.Prim(PrimitiveType.Cube, "Awning", b, new Vector3(-(w - 0.8f) * 0.5f + sw * (i + 0.5f), 2.3f, -3.45f), new Vector3(sw, 0.08f, 0.95f), i % 2 == 0 ? TM.Hex("#D8343A") : TM.Hex("#FFF4E0"), 0.15f, new Vector3(-14, 0, 0));
                TM.Prim(PrimitiveType.Cube, "AwningSnow", b, new Vector3(0, 2.45f, -3.3f), new Vector3(w - 0.9f, 0.06f, 0.7f), Color.white, 0, new Vector3(-14, 0, 0));
                TM.Prim(PrimitiveType.Cube, "Eave", b, new Vector3(0, 3.0f, -3.15f), new Vector3(w - 0.3f, 0.12f, 0.35f), TM.Hex("#2E2A40"));
                TM.Prim(PrimitiveType.Cube, "EaveSnow", b, new Vector3(0, 3.08f, -3.15f), new Vector3(w - 0.35f, 0.06f, 0.33f), Color.white);
                for (float lx = -w * 0.5f + 0.9f; lx < w * 0.5f - 0.5f; lx += 1.1f)
                    TM.Prim(PrimitiveType.Sphere, "ShopLantern", b, new Vector3(lx, 2.0f, -3.85f), new Vector3(0.26f, 0.34f, 0.26f), TM.Hex("#FF5A3A"), 1.4f);
                // 有些店多一塊橫招牌
                if (idx % 3 == 1)
                {
                    TM.Prim(PrimitiveType.Cube, "HSignFrame", b, new Vector3(-0.6f, 3.6f, -3.08f), new Vector3(2.8f, 0.8f, 0.1f), TM.Hex("#E2463B"), 0.4f);
                    TM.Prim(PrimitiveType.Cube, "HSign", b, new Vector3(-0.6f, 3.6f, -3.14f), new Vector3(2.6f, 0.62f, 0.02f), TM.Hex("#FFF4E0"), 0.6f);
                    TM.Text3D(idx % 2 == 0 ? "ラーメン" : "お好み燒", b, new Vector3(-0.6f, 3.6f, -3.17f), 0.36f, TM.Hex("#2A1A1A"));
                }
                if (idx % 2 == 0)
                {
                    var l = new GameObject("NeonLight").AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = sc;
                    l.range = 9f;
                    l.intensity = 1.6f;
                    l.transform.SetParent(b, false);
                    l.transform.localPosition = new Vector3(0, 3.5f, -4.5f);
                }
                x += w;
                idx++;
            }

            // 橋出口正對的大招牌「道頓堀」：紅底、金字、一圈燈泡
            var gate = TM.Empty("DotonboriGate", p, new Vector3(0, 0, 36.5f));
            for (int s = -1; s <= 1; s += 2)
                TM.Prim(PrimitiveType.Cube, "GatePost", gate, new Vector3(s * 2.6f, 2.7f, 0), new Vector3(0.35f, 5.4f, 0.35f), TM.Hex("#2A1A1A"), keepCollider: true);
            TM.Prim(PrimitiveType.Cube, "GateBoard", gate, new Vector3(0, 5.5f, 0), new Vector3(5.8f, 1.7f, 0.3f), TM.Hex("#8E1E24"));
            TM.Prim(PrimitiveType.Cube, "GateFace", gate, new Vector3(0, 5.5f, -0.16f), new Vector3(5.2f, 1.2f, 0.02f), TM.Hex("#C8232A"), 0.8f);
            TM.Text3D("道頓堀", gate, new Vector3(0, 5.5f, -0.2f), 0.9f, TM.Hex("#FFD27A"));
            for (int i = 0; i < 12; i++)
            {
                float bx = -2.75f + i * 0.5f;
                TM.Prim(PrimitiveType.Sphere, "GateBulb", gate, new Vector3(bx, 6.3f, -0.17f), Vector3.one * 0.14f, TM.Hex("#FFE7A8"), 2.2f);
                TM.Prim(PrimitiveType.Sphere, "GateBulb", gate, new Vector3(bx, 4.7f, -0.17f), Vector3.one * 0.14f, TM.Hex("#FFE7A8"), 2.2f);
            }
            for (int i = 0; i < 3; i++)
                foreach (var bx in new[] { -2.8f, 2.8f })
                    TM.Prim(PrimitiveType.Sphere, "GateBulb", gate, new Vector3(bx, 5.0f + i * 0.5f, -0.17f), Vector3.one * 0.14f, TM.Hex("#FFE7A8"), 2.2f);
            TM.Prim(PrimitiveType.Cube, "GateSnow", gate, new Vector3(0, 6.4f, 0), new Vector3(5.7f, 0.07f, 0.28f), Color.white);

            // 街上的紙燈籠串
            for (int i = 0; i < 12; i++)
                Lantern(p, new Vector3(-22f + i * 4f, 0, 36.3f), small: true);

            // 撐傘逛街的人
            float[] px = { -20f, -16.5f, -9f, -6.5f, 6.5f, 9.5f, 15f, 19.5f, 23f };
            for (int i = 0; i < px.Length; i++)
                Props.Npc(p, new Vector3(px[i], 0, 35f + (i % 3) * 0.8f), i % 2 == 0 ? 90 : -90, i % 3 == 0, i % 8, i % 3 != 2);

            // 背景遠方的高樓剪影（被霧淡化），有幾格亮窗
            for (int i = 0; i < 12; i++)
            {
                float h = R(12, 22);
                var tw = TM.Prim(PrimitiveType.Cube, "FarTower", p, new Vector3(-33 + i * 6f, h * 0.5f, R(52, 60)), new Vector3(R(4, 6), h, 4), TM.Hex("#2B2A48"));
                for (int k = 0; k < 4; k++)
                    TM.Prim(PrimitiveType.Cube, "FarWin", p, tw.transform.localPosition + new Vector3(R(-1.5f, 1.5f), R(-h * 0.3f, h * 0.4f), -2.02f), new Vector3(0.6f, 0.5f, 0.02f), TM.Hex(k % 2 == 0 ? "#FFE2A8" : "#FF8AD8"), 1f);
            }
        }

        // ---------- 樹 ----------
        static void Trees(Transform p)
        {
            int placed = 0, tries = 0;
            while (placed < 46 && tries < 600)
            {
                tries++;
                var pos = new Vector3(R(-36, 36), 0, R(-36, 22));
                if (pos.magnitude < 11.5f) continue;                               // 廣場
                if (Mathf.Abs(pos.x) < 4f && pos.z > -14f) continue;               // 南北路
                bool near = false;
                foreach (var b in blocked) if ((b - pos).sqrMagnitude < 5.5f * 5.5f) { near = true; break; }
                if (near) continue;
                PineTree(p, pos, R(0.8f, 1.35f));
                placed++;
            }
        }

        // ---------- 下雪 ----------
        internal static void Snow(Transform root, Vector3 local)
        {
            var go = new GameObject("Snowfall");
            go.transform.SetParent(root, false);
            go.transform.localPosition = local;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 9f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.2f);
            main.startColor = new Color(1, 1, 1, 0.9f);
            main.gravityModifier = 0.12f;
            main.maxParticles = 4000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var em = ps.emission;
            em.rateOverTime = 380f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(80f, 1f, 80f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.4f;
            noise.frequency = 0.3f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = TM.ParticleMat();
            ps.Play();
        }

        // ---------- 邊界（看不見的牆） ----------
        static void Borders(Transform p)
        {
            Wall(p, new Vector3(0, 2, -38.5f), new Vector3(80, 4, 1));
            Wall(p, new Vector3(0, 2, 46f), new Vector3(80, 4, 1));
            Wall(p, new Vector3(-38.5f, 2, 4), new Vector3(1, 4, 86));
            Wall(p, new Vector3(38.5f, 2, 4), new Vector3(1, 4, 86));
        }

        internal static void Wall(Transform p, Vector3 pos, Vector3 size)
        {
            var g = TM.Prim(PrimitiveType.Cube, "InvisibleWall", p, pos, size, Color.white, keepCollider: true);
            TM.Kill(g.GetComponent<MeshRenderer>());
        }

        // =====================================================================
        // 小物件
        // =====================================================================

        internal static void PineTree(Transform p, Vector3 pos, float s)
        {
            var t = TM.Empty("PineTree", p, pos);
            t.localScale = Vector3.one * s;
            t.localEulerAngles = new Vector3(0, R(0, 360), 0);
            TM.Prim(PrimitiveType.Cylinder, "Trunk", t, new Vector3(0, 0.5f, 0), new Vector3(0.35f, 0.5f, 0.35f), TM.Wood, keepCollider: true);
            MapAdd(pos.x, pos.z, 2.2f * s, 2.2f * s, TM.Hex("#7FB08F"), 1.1f * s);
            for (int i = 0; i < 3; i++)
            {
                float y = 0.9f + i * 0.85f;
                float rad = 1.5f - i * 0.38f;
                TM.MeshObj("Leaves", t, TM.Cone(rad, 1.5f, 10), new Vector3(0, y, 0), TM.Pine);
                TM.MeshObj("SnowCap", t, TM.Cone(rad * 0.55f, 0.75f, 10), new Vector3(0, y + 0.78f, 0), Color.white);
            }
        }

        internal static void BigTree(Transform p, Vector3 pos)
        {
            var t = TM.Empty("PlazaTree", p, pos);
            TM.Prim(PrimitiveType.Cylinder, "Trunk", t, new Vector3(0, 0.9f, 0), new Vector3(0.7f, 0.9f, 0.7f), TM.Wood, keepCollider: true);
            MapAdd(pos.x, pos.z, 4f, 4f, TM.Pine, 2f);
            string[] bulbs = { "#FF4F6B", "#FFC93C", "#38D6FF", "#FFFFFF" };
            for (int i = 0; i < 4; i++)
            {
                float y = 1.5f + i * 1.2f;
                float rad = 2.6f - i * 0.55f;
                TM.MeshObj("Leaves", t, TM.Cone(rad, 2.1f, 14), new Vector3(0, y, 0), TM.Pine);
                TM.MeshObj("SnowCap", t, TM.Cone(rad * 0.5f, 1.0f, 14), new Vector3(0, y + 1.1f, 0), Color.white);
                // 燈泡
                int n = 9 - i * 2;
                for (int k = 0; k < n; k++)
                {
                    float a = (k / (float)n + i * 0.13f) * Mathf.PI * 2f;
                    float rr = rad * 0.82f;
                    TM.Prim(PrimitiveType.Sphere, "Bulb", t, new Vector3(Mathf.Cos(a) * rr, y + 0.35f, Mathf.Sin(a) * rr), Vector3.one * 0.16f, TM.Hex(bulbs[(k + i) % bulbs.Length]), 2f);
                }
            }
            // 頂端星星
            var treeCol = t.gameObject.AddComponent<CapsuleCollider>();
            treeCol.center = new Vector3(0, 2.5f, 0); treeCol.radius = 1.9f; treeCol.height = 5f;
            var star = TM.Prim(PrimitiveType.Sphere, "Star", t, new Vector3(0, 7.1f, 0), Vector3.one * 0.5f, TM.Hex("#FFC93C"), 2.5f);
            star.AddComponent<Bob>().amp = 0.08f;
            var l = new GameObject("TreeLight").AddComponent<Light>();
            l.type = LightType.Point; l.color = TM.Hex("#FFC27A"); l.range = 12; l.intensity = 1.4f;
            l.transform.SetParent(t, false); l.transform.localPosition = new Vector3(0, 3, 0);
            blocked.Add(pos);
        }

        internal static void Lamp(Transform p, Vector3 pos, bool withLight)
        {
            var t = TM.Empty("Lamp", p, pos);
            TM.Prim(PrimitiveType.Cylinder, "Post", t, new Vector3(0, 1.2f, 0), new Vector3(0.12f, 1.2f, 0.12f), TM.Ink, keepCollider: true);
            TM.Prim(PrimitiveType.Cube, "Cap", t, new Vector3(0, 2.75f, 0), new Vector3(0.5f, 0.08f, 0.5f), TM.Ink);
            TM.Prim(PrimitiveType.Sphere, "Glow", t, new Vector3(0, 2.5f, 0), Vector3.one * 0.42f, TM.Hex("#FFD27A"), 2f);
            TM.Prim(PrimitiveType.Sphere, "SnowTop", t, new Vector3(0, 2.82f, 0), new Vector3(0.5f, 0.12f, 0.5f), Color.white);
            if (withLight)
            {
                var l = new GameObject("LampLight").AddComponent<Light>();
                l.type = LightType.Point; l.color = TM.Hex("#FFC27A"); l.range = 6.5f; l.intensity = 1.2f;
                l.transform.SetParent(t, false); l.transform.localPosition = new Vector3(0, 2.4f, 0);
            }
        }

        internal static void Lantern(Transform p, Vector3 pos, bool small = false)
        {
            var t = TM.Empty("Lantern", p, pos);
            float h = small ? 3.2f : 1.6f;
            if (!small) TM.Prim(PrimitiveType.Cylinder, "Pole", t, new Vector3(0, h * 0.5f, 0), new Vector3(0.08f, h * 0.5f, 0.08f), TM.Wood);
            else TM.Prim(PrimitiveType.Cylinder, "String", t, new Vector3(0, h + 0.4f, 0), new Vector3(0.02f, 0.25f, 0.02f), TM.Ink);
            var lan = TM.Prim(PrimitiveType.Sphere, "Paper", t, new Vector3(0, h + 0.1f, 0), new Vector3(0.45f, 0.6f, 0.45f), TM.Hex("#E2463B"), 1.1f);
            TM.Prim(PrimitiveType.Cylinder, "Top", t, new Vector3(0, h + 0.42f, 0), new Vector3(0.28f, 0.04f, 0.28f), TM.Ink);
            TM.Prim(PrimitiveType.Cylinder, "Bottom", t, new Vector3(0, h - 0.22f, 0), new Vector3(0.28f, 0.04f, 0.28f), TM.Ink);
            lan.AddComponent<Bob>().amp = 0.03f;
        }

        internal static void Bench(Transform p, Vector3 pos, float yaw)
        {
            var t = TM.Empty("Bench", p, pos);
            t.localEulerAngles = new Vector3(0, yaw, 0);
            TM.Prim(PrimitiveType.Cube, "Seat", t, new Vector3(0, 0.45f, 0), new Vector3(2f, 0.12f, 0.6f), TM.Wood, keepCollider: true);
            TM.Prim(PrimitiveType.Cube, "Back", t, new Vector3(0, 0.85f, 0.27f), new Vector3(2f, 0.5f, 0.08f), TM.Wood);
            TM.Prim(PrimitiveType.Cube, "SnowOnSeat", t, new Vector3(0, 0.53f, 0), new Vector3(1.8f, 0.04f, 0.5f), Color.white);
            for (int s = -1; s <= 1; s += 2)
                TM.Prim(PrimitiveType.Cube, "Leg", t, new Vector3(s * 0.85f, 0.22f, 0), new Vector3(0.1f, 0.45f, 0.5f), TM.Ink);
        }

        internal static void House(Transform p, Vector3 pos, float yaw, Vector3 size, Color wall, Color roof, string sign)
        {
            var t = TM.Empty("House_" + sign, p, pos);
            t.localEulerAngles = new Vector3(0, yaw, 0); // yaw = 門面向的方向（0 = 朝北，90 = 朝東）
            float fz = size.z * 0.5f;
            TM.MeshObj("Walls", t, TM.RoundedBox(size, 0.25f, 6), new Vector3(0, size.y * 0.5f, 0), wall);
            MapAdd(pos.x, pos.z, Mathf.Max(size.x, size.z), Mathf.Max(size.x, size.z), roof, 3f);
            var col = t.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, size.y * 0.5f, 0); col.size = size;
            // 屋頂：四角錐＋積雪
            TM.MeshObj("Roof", t, TM.Cone(size.x * 0.82f, size.y * 0.7f, 4), new Vector3(0, size.y - 0.05f, 0), roof, 0, new Vector3(0, 45, 0), new Vector3(1, 1, size.z / size.x));
            TM.MeshObj("RoofSnow", t, TM.Cone(size.x * 0.45f, size.y * 0.38f, 4), new Vector3(0, size.y + size.y * 0.33f, 0), Color.white, 0, new Vector3(0, 45, 0), new Vector3(1, 1, size.z / size.x));
            // 門、窗（暖光）
            TM.MeshObj("Door", t, TM.RoundedBox(new Vector3(1.1f, 1.8f, 0.2f), 0.15f, 4), new Vector3(0, 0.9f, fz), TM.Wood);
            TM.Prim(PrimitiveType.Sphere, "Knob", t, new Vector3(0.35f, 0.9f, fz + 0.12f), Vector3.one * 0.1f, TM.Hex("#FFC93C"), 0.5f);
            for (int s = -1; s <= 1; s += 2)
            {
                TM.Prim(PrimitiveType.Cube, "Window", t, new Vector3(s * size.x * 0.3f, size.y * 0.55f, fz + 0.01f), new Vector3(0.9f, 0.8f, 0.05f), TM.Hex("#FFD27A"), 1.2f);
                TM.Prim(PrimitiveType.Cube, "WindowBar", t, new Vector3(s * size.x * 0.3f, size.y * 0.55f, fz + 0.04f), new Vector3(0.06f, 0.8f, 0.03f), TM.Wood);
            }
            // 招牌（文字轉 180 度，從正面看才不會左右相反）
            TM.Prim(PrimitiveType.Cube, "SignBoard", t, new Vector3(0, 2.25f, fz + 0.08f), new Vector3(sign.Length * 0.42f + 0.5f, 0.55f, 0.08f), TM.Page);
            TM.Text3D(sign, t, new Vector3(0, 2.25f, fz + 0.14f), 0.38f, TM.Ink, new Vector3(0, 180, 0));
            blocked.Add(pos);
        }

        internal static Transform Building(Transform p, Vector3 pos, Vector3 size, Color wall)
        {
            var t = TM.Empty("Shop", p, pos);
            TM.Prim(PrimitiveType.Cube, "Block", t, new Vector3(0, size.y * 0.5f, 0), size, wall, keepCollider: true);
            MapAdd(pos.x, pos.z, size.x, size.z, TM.Hex("#2F2C4A"), 1f);
            TM.Prim(PrimitiveType.Cube, "RoofEdge", t, new Vector3(0, size.y + 0.1f, 0), new Vector3(size.x + 0.2f, 0.2f, size.z + 0.2f), TM.Hex("#26233D"));
            TM.Prim(PrimitiveType.Cube, "RoofSnow", t, new Vector3(0, size.y + 0.22f, 0), new Vector3(size.x, 0.06f, size.z), Color.white);
            // 樓上亮著的窗
            int floors = Mathf.FloorToInt((size.y - 3f) / 1.6f);
            for (int f = 0; f < floors; f++)
                for (int k = 0; k < 2; k++)
                {
                    if (R(0, 1) < 0.3f) continue;
                    TM.Prim(PrimitiveType.Cube, "Win", t, new Vector3((k - 0.5f) * size.x * 0.45f - 0.4f, 3.4f + f * 1.6f, -size.z * 0.5f - 0.01f), new Vector3(0.9f, 0.8f, 0.02f), TM.Hex(R(0, 1) < 0.5f ? "#FFE2A8" : "#BFE6FF"), 0.8f);
                }
            return t;
        }

        /// <summary>直式霓虹招牌（照參考圖配色：拉麵紅底白字、章魚燒米白底紅字、串炸黃底黑字⋯）。回傳招牌主色給燈光用。</summary>
        internal static Color VerticalSign(Transform b, Vector3 local, string text)
        {
            Color bg, fg, frame;
            switch (text)
            {
                case "ラーメン": bg = TM.Hex("#D8282E"); fg = Color.white; frame = TM.Hex("#F4E3C0"); break;
                case "たこ燒": bg = TM.Hex("#FFF1D6"); fg = TM.Hex("#D8282E"); frame = TM.Hex("#D8282E"); break;
                case "串カツ": bg = TM.Hex("#FFD24A"); fg = TM.Hex("#2A1A1A"); frame = TM.Hex("#2A1A1A"); break;
                case "居酒屋": bg = TM.Hex("#C8232A"); fg = TM.Hex("#FFF4E0"); frame = TM.Hex("#FFF4E0"); break;
                case "お好み燒": bg = TM.Hex("#FFF4E0"); fg = TM.Hex("#2A1A1A"); frame = TM.Hex("#E2463B"); break;
                default: bg = TM.Hex("#FF7A3D"); fg = Color.white; frame = Color.white; break;
            }
            float h = text.Length * 0.85f + 0.5f;
            TM.Prim(PrimitiveType.Cube, "SignBack", b, local, new Vector3(1.2f, h + 0.2f, 0.25f), TM.Hex("#1A1830"));
            TM.Prim(PrimitiveType.Cube, "SignFrame", b, local + new Vector3(0, 0, -0.13f), new Vector3(1.1f, h + 0.1f, 0.02f), frame, 0.7f);
            TM.Prim(PrimitiveType.Cube, "SignFace", b, local + new Vector3(0, 0, -0.15f), new Vector3(0.94f, h - 0.06f, 0.02f), bg, 0.75f);
            // 直書：每個字一行，長音「ー」改成直線
            var sb = new System.Text.StringBuilder();
            foreach (char ch in text) { if (sb.Length > 0) sb.Append('\n'); sb.Append(ch == 'ー' ? '｜' : ch); }
            var tm = TM.Text3D(sb.ToString(), b, local + new Vector3(0, 0, -0.17f), 0.62f, fg);
            tm.lineSpacing = 0.95f;
            // 上下的燈泡
            for (int i = 0; i < 3; i++)
                foreach (var y in new[] { h * 0.5f + 0.02f, -h * 0.5f - 0.02f })
                    TM.Prim(PrimitiveType.Sphere, "SignBulb", b, local + new Vector3(-0.35f + i * 0.35f, y, -0.16f), Vector3.one * 0.1f, TM.Hex("#FFE7A8"), 2f);
            TM.Prim(PrimitiveType.Cube, "SignSnow", b, local + new Vector3(0, h * 0.5f + 0.14f, 0), new Vector3(1.2f, 0.06f, 0.26f), Color.white);
            return bg;
        }

        internal static void ClockTower(Transform p, Vector3 pos)
        {
            var t = TM.Empty("ClockTower", p, pos);
            TM.Prim(PrimitiveType.Cube, "Base", t, new Vector3(0, 0.3f, 0), new Vector3(2.2f, 0.6f, 2.2f), TM.Hex("#B9AC9A"), keepCollider: true);
            MapAdd(pos.x, pos.z, 2.2f, 2.2f, TM.Hex("#3F6FB5"), 0.6f);
            TM.MeshObj("Tower", t, TM.RoundedBox(new Vector3(1.6f, 5f, 1.6f), 0.15f, 4), new Vector3(0, 3.1f, 0), TM.Hex("#E9DCC8"));
            var col = t.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 3, 0); col.size = new Vector3(1.6f, 6, 1.6f);
            TM.MeshObj("Roof", t, TM.Cone(1.4f, 1.6f, 4), new Vector3(0, 5.6f, 0), TM.Hex("#3F6FB5"), 0, new Vector3(0, 45, 0));
            TM.MeshObj("RoofSnow", t, TM.Cone(0.7f, 0.8f, 4), new Vector3(0, 6.4f, 0), Color.white, 0, new Vector3(0, 45, 0));
            // 鐘面朝南
            var face = TM.Empty("Clock", t, new Vector3(0, 4.6f, -0.82f));
            TM.Prim(PrimitiveType.Cylinder, "Face", face, Vector3.zero, new Vector3(1.1f, 0.03f, 1.1f), Color.white, 0.2f, new Vector3(90, 0, 0));
            TM.Prim(PrimitiveType.Cube, "HandH", face, new Vector3(0.13f, 0.06f, -0.05f), new Vector3(0.3f, 0.06f, 0.02f), TM.Ink, 0, new Vector3(0, 0, 25));
            TM.Prim(PrimitiveType.Cube, "HandM", face, new Vector3(0, 0.2f, -0.05f), new Vector3(0.05f, 0.42f, 0.02f), TM.Ink);
            blocked.Add(pos);
        }

        internal static void NoticeBoard(Transform p, Vector3 pos, string title, Vector3 euler)
        {
            var t = TM.Empty("NoticeBoard", p, pos);
            t.localEulerAngles = euler;
            for (int s = -1; s <= 1; s += 2)
                TM.Prim(PrimitiveType.Cylinder, "Post", t, new Vector3(s * 1.1f, 0.9f, 0), new Vector3(0.15f, 0.9f, 0.15f), TM.Wood);
            TM.Prim(PrimitiveType.Cube, "Board", t, new Vector3(0, 1.4f, 0), new Vector3(2.6f, 1.6f, 0.12f), TM.Wood, keepCollider: true);
            TM.Prim(PrimitiveType.Cube, "Roof", t, new Vector3(0, 2.35f, 0), new Vector3(3f, 0.15f, 0.6f), TM.Hex("#C8553D"));
            TM.Prim(PrimitiveType.Cube, "RoofSnow", t, new Vector3(0, 2.45f, 0), new Vector3(2.9f, 0.06f, 0.55f), Color.white);
            // 貼紙
            string[] notes = { "#FFF3C4", "#CFE3F2", "#FFD6D0", "#DFF2D0" };
            for (int i = 0; i < 4; i++)
                TM.Prim(PrimitiveType.Cube, "Note", t, new Vector3(-0.85f + i * 0.57f, 1.2f + (i % 2) * 0.2f, -0.07f), new Vector3(0.45f, 0.5f, 0.01f), TM.Hex(notes[i]), 0, new Vector3(0, 0, (i - 1.5f) * 5f));
            TM.Text3D(title, t, new Vector3(0, 1.92f, -0.08f), 0.3f, TM.Page);
            blocked.Add(pos);
        }

        internal static void Signpost(Transform p, Vector3 pos, string text)
        {
            var t = TM.Empty("Signpost", p, pos);
            TM.Prim(PrimitiveType.Cylinder, "Post", t, new Vector3(0, 0.8f, 0), new Vector3(0.12f, 0.8f, 0.12f), TM.Wood);
            TM.Prim(PrimitiveType.Cube, "Board", t, new Vector3(0, 1.45f, 0), new Vector3(1.6f, 0.45f, 0.08f), TM.Page);
            TM.Text3D(text, t, new Vector3(0, 1.45f, -0.06f), 0.28f, TM.Ink);
        }

        /// <summary>攤位（舊呼叫方式：yaw 是背面朝向，這裡轉成門面朝向交給 Props.Stall）。</summary>
        internal static void Stall(Transform p, Vector3 pos, float yaw, string sign, Color canopy)
        {
            Props.Stall(p, pos, yaw + 180f, sign, canopy);
            blocked.Add(pos);
        }

        internal static void Arch(Transform p, Vector3 pos, string text)
        {
            var t = TM.Empty("EntranceArch", p, pos);
            for (int s = -1; s <= 1; s += 2)
            {
                TM.Prim(PrimitiveType.Cylinder, "Pillar", t, new Vector3(s * 2f, 1.5f, 0), new Vector3(0.35f, 1.5f, 0.35f), TM.Wood, keepCollider: true);
                TM.Prim(PrimitiveType.Sphere, "PillarSnow", t, new Vector3(s * 2f, 3.05f, 0), new Vector3(0.45f, 0.18f, 0.45f), Color.white);
            }
            TM.Prim(PrimitiveType.Cube, "Beam", t, new Vector3(0, 3.1f, 0), new Vector3(5f, 0.6f, 0.3f), TM.Body);
            TM.Text3D(text, t, new Vector3(0, 3.1f, -0.17f), 0.36f, TM.Ink);
        }

        internal static void Railing(Transform p, float z, float x0, float x1)
        {
            float len = x1 - x0;
            float cx = (x0 + x1) * 0.5f;
            var rail = TM.Prim(PrimitiveType.Cube, "Rail", p, new Vector3(cx, 0.55f, z), new Vector3(len, 1.1f, 0.12f), TM.Hex("#C8553D"), keepCollider: true);
            TM.Kill(rail.GetComponent<MeshRenderer>()); // 碰撞用，外觀交給下面的細桿
            TM.Prim(PrimitiveType.Cube, "RailTop", p, new Vector3(cx, 0.9f, z), new Vector3(len, 0.08f, 0.1f), TM.Hex("#C8553D"));
            TM.Prim(PrimitiveType.Cube, "RailMid", p, new Vector3(cx, 0.5f, z), new Vector3(len, 0.05f, 0.06f), TM.Hex("#C8553D"));
            for (float x = x0; x <= x1 + 0.01f; x += 1.8f)
                TM.Prim(PrimitiveType.Cube, "RailPost", p, new Vector3(x, 0.47f, z), new Vector3(0.1f, 0.95f, 0.1f), TM.Hex("#A8432F"));
        }
    }
}

using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 場景道具（照 GPT 美術參考圖的方向做）：Q 版路人、木造攤位（雪屋頂＋燈籠＋貨箱）、
    /// 日式小店（瓦屋頂＋積雪＋暖簾＋燈籠）、石板路。
    /// 一律「門面朝 yaw 方向」：yaw 0＝朝北（+Z），90＝朝東（+X）。
    /// </summary>
    public static class Props
    {

        static int seed = 11;
        static float Rnd() { seed = (seed * 1103515245 + 12345) & 0x7fffffff; return (seed % 10000) / 10000f; }
        static string Pick(string[] a) { return a[(int)(Rnd() * a.Length) % a.Length]; }

        /// <summary>
        /// Q 版路人（照角色參考圖的 8 種）：0 毛帽滑手機女生、1 撐傘男生、2 學生妹、3 粉大衣提包小姐、
        /// 4 提購物袋男生、5 紫外套奶奶、6 鴨舌帽拄拐杖爺爺、7 黃毛帽小孩；8 章魚燒攤老闆。
        /// kind = -1 隨機。umbrella = 撐一把紅傘（傘上有雪）。
        /// </summary>
        public static Transform Npc(Transform p, Vector3 pos, float yaw, bool phone, int kind = -1, bool umbrella = false)
        {
            if (kind < 0) kind = phone && Rnd() < 0.5f ? 0 : (int)(Rnd() * 8) % 8;
            var t = TM.Empty("Traveler_" + kind, p, pos);
            t.localEulerAngles = new Vector3(0, yaw, 0);
            var body = TM.Empty("Body", t, Vector3.zero);
            if (kind == 7) body.localScale = Vector3.one * 0.78f;
            var skin = TM.Hex(Rnd() < 0.6f ? "#F8DCC6" : "#EFC9A8");
            string[] browns = { "#4A2E22", "#5A3A28", "#3A2A20" };
            var hairC = TM.Hex(browns[(int)(Rnd() * 3) % 3]);
            Color coat, pants = TM.Hex("#2E3350"), boots = TM.Hex("#B07A4A"), scarf;
            bool fur = true, longCoat = false, skirt = false, pack = false;
            switch (kind)
            {
                case 0: coat = TM.Hex("#F3ECE0"); scarf = TM.Hex("#C8323A"); pack = true; break;
                case 1: coat = TM.Hex("#2F4A8C"); scarf = TM.Hex("#5E5650"); pack = true; break;
                case 2: coat = TM.Hex("#28305A"); scarf = TM.Hex("#F1E9DA"); boots = TM.Hex("#5A3424"); fur = false; skirt = true; pack = true; break;
                case 3: coat = TM.Hex("#E7B3B8"); scarf = TM.Hex("#F1E9DA"); boots = TM.Hex("#4A2E22"); fur = false; longCoat = true; skirt = true; break;
                case 4: coat = TM.Hex("#3A2E2E"); scarf = TM.Hex("#2E4A8C"); pants = TM.Hex("#C9B79A"); boots = TM.Hex("#F4F1EC"); fur = false; longCoat = true; break;
                case 5: coat = TM.Hex("#A58BC4"); scarf = TM.Hex("#F4EBDD"); pants = TM.Hex("#3A2A2A"); boots = TM.Hex("#6E4A30"); fur = false; hairC = TM.Hex("#C9C6C9"); break;
                case 6: coat = TM.Hex("#5E6E4A"); scarf = TM.Hex("#6E5A4A"); pants = TM.Hex("#3A2E2A"); boots = TM.Hex("#3A2A20"); fur = false; hairC = TM.Hex("#C9C6C9"); break;
                case 7: coat = TM.Hex("#2F4A8C"); scarf = TM.Hex("#F1E9DA"); pack = true; break;
                default: coat = Color.white; scarf = TM.Hex("#E2463B"); pants = TM.Hex("#2A2D3E"); boots = TM.Hex("#3A2A20"); fur = false; break; // 老闆：白衣
            }

            // 靴子＋腿
            foreach (var x in new[] { -0.13f, 0.13f })
            {
                TM.MeshObj("Boot", body, TM.RoundedBox(new Vector3(0.21f, 0.17f, 0.3f), 0.07f, 4), new Vector3(x, 0.085f, 0.03f), boots);
                if (fur) TM.Prim(PrimitiveType.Cylinder, "BootFur", body, new Vector3(x, 0.19f, 0.01f), new Vector3(0.22f, 0.03f, 0.22f), TM.Hex("#F4EBDD"));
                if (kind == 2) TM.Prim(PrimitiveType.Cylinder, "Sock", body, new Vector3(x, 0.26f, 0), new Vector3(0.15f, 0.09f, 0.15f), TM.Hex("#1E2440"));
                TM.Prim(PrimitiveType.Capsule, "Leg", body, new Vector3(x, 0.3f, 0), new Vector3(0.16f, 0.15f, 0.16f), skirt && kind == 2 ? skin : pants);
            }
            // 外套（長大衣比較長）
            if (longCoat)
                TM.MeshObj("Coat", body, TM.RoundedBox(new Vector3(0.62f, 0.66f, 0.46f), 0.18f, 5), new Vector3(0, 0.62f, 0), coat);
            else
                TM.MeshObj("Coat", body, TM.RoundedBox(new Vector3(0.64f, 0.5f, 0.48f), 0.22f, 5), new Vector3(0, 0.66f, 0), coat);
            if (skirt)
                TM.MeshObj("Skirt", body, TM.Cone(0.36f, 0.5f, 14), new Vector3(0, longCoat ? 0.26f : 0.34f, 0), kind == 3 ? TM.Hex("#5A3A2E") : TM.Hex("#28305A"));
            if (!longCoat) // 羽絨衣的橫向車線
                TM.Prim(PrimitiveType.Cube, "Quilt", body, new Vector3(0, 0.66f, 0.24f), new Vector3(0.5f, 0.02f, 0.01f), coat * 0.82f);
            TM.Prim(PrimitiveType.Cube, "Zip", body, new Vector3(0, 0.62f, 0.245f), new Vector3(0.025f, 0.42f, 0.01f), coat * 0.7f);
            if (kind == 5) TM.MeshObj("FurCollar", body, TM.Torus(0.22f, 0.09f, 16, 6), new Vector3(0, 0.9f, 0), TM.Hex("#F4EBDD"), 0, null, new Vector3(1, 1.1f, 0.95f));
            else TM.MeshObj("Scarf", body, TM.Torus(0.2f, 0.075f, 16, 6), new Vector3(0, 0.9f, 0), scarf, 0, null, new Vector3(1, 1.2f, 0.9f));
            if (kind != 5) TM.Prim(PrimitiveType.Cube, "ScarfTail", body, new Vector3(0.1f, 0.72f, 0.25f), new Vector3(0.12f, 0.3f, 0.05f), scarf);
            if (kind == 8) TM.Prim(PrimitiveType.Cube, "Apron", body, new Vector3(0, 0.52f, 0.245f), new Vector3(0.46f, 0.46f, 0.02f), TM.Hex("#2E3350"));

            // 手臂＋手套（滑手機的人兩手在胸前）
            bool handsFront = kind == 0 || kind == 2 || kind == 5 || kind == 7 || (phone && kind != 4);
            bool holdUmb = umbrella || kind == 1;
            foreach (var s in new[] { -1f, 1f })
            {
                if (holdUmb && s > 0)
                {
                    TM.Prim(PrimitiveType.Capsule, "Arm", body, new Vector3(0.24f, 0.8f, 0.1f), new Vector3(0.15f, 0.17f, 0.15f), coat, 0, new Vector3(90, -40, 0));
                    continue;
                }
                if (handsFront)
                {
                    TM.Prim(PrimitiveType.Capsule, "Arm", body, new Vector3(s * 0.28f, 0.66f, 0.14f), new Vector3(0.15f, 0.17f, 0.15f), coat, 0, new Vector3(-50, 0, s * 25));
                    TM.Prim(PrimitiveType.Sphere, "Mitten", body, new Vector3(s * 0.12f, 0.66f, 0.32f), Vector3.one * 0.14f, TM.Hex("#5A3A28"));
                }
                else
                {
                    TM.Prim(PrimitiveType.Capsule, "Arm", body, new Vector3(s * 0.35f, 0.62f, 0.03f), new Vector3(0.15f, 0.19f, 0.15f), coat, 0, new Vector3(0, 0, s * 12));
                    TM.Prim(PrimitiveType.Sphere, "Mitten", body, new Vector3(s * 0.4f, 0.42f, 0.06f), Vector3.one * 0.14f, TM.Hex("#3A2E2A"));
                }
            }

            // 頭、臉
            TM.Prim(PrimitiveType.Sphere, "Head", body, new Vector3(0, 1.2f, 0), Vector3.one * 0.66f, skin);
            if (kind == 5)
                foreach (var x in new[] { -0.12f, 0.12f }) // 奶奶瞇瞇眼
                    TM.Prim(PrimitiveType.Cube, "EyeClosed", body, new Vector3(x, 1.17f, 0.315f), new Vector3(0.09f, 0.02f, 0.02f), TM.Ink, 0, new Vector3(0, 0, x > 0 ? -15 : 15));
            else
                foreach (var x in new[] { -0.12f, 0.12f })
                {
                    TM.Prim(PrimitiveType.Sphere, "Eye", body, new Vector3(x, 1.17f, 0.31f), new Vector3(0.07f, 0.1f, 0.04f), TM.Hex("#1E1A1A"));
                    TM.Prim(PrimitiveType.Sphere, "Shine", body, new Vector3(x + 0.015f, 1.19f, 0.33f), new Vector3(0.025f, 0.025f, 0.01f), Color.white);
                }
            foreach (var x in new[] { -0.2f, 0.2f })
                TM.Prim(PrimitiveType.Sphere, "Blush", body, new Vector3(x, 1.09f, 0.28f), new Vector3(0.11f, 0.055f, 0.02f), TM.Hex("#F59A8C"));
            TM.Prim(PrimitiveType.Cube, "Mouth", body, new Vector3(0, 1.065f, 0.325f), new Vector3(0.07f, 0.018f, 0.01f), TM.Hex("#8C3A32"));

            // 頭髮
            bool coveredTop = kind == 0 || kind == 6 || kind == 7;
            TM.Prim(PrimitiveType.Sphere, "Hair", body, new Vector3(0, 1.28f, -0.045f), new Vector3(0.7f, 0.62f, 0.67f), hairC);
            if (!coveredTop)
                TM.Prim(PrimitiveType.Sphere, "Bangs", body, new Vector3(0, 1.4f, 0.17f), new Vector3(0.52f, 0.2f, 0.28f), hairC);
            switch (kind)
            {
                case 0: // 長髮
                    TM.MeshObj("LongHair", body, TM.RoundedBox(new Vector3(0.66f, 0.62f, 0.28f), 0.12f, 4), new Vector3(0, 1.0f, -0.16f), hairC);
                    foreach (var x in new[] { -0.3f, 0.3f })
                        TM.Prim(PrimitiveType.Capsule, "Lock", body, new Vector3(x, 0.98f, 0.06f), new Vector3(0.16f, 0.22f, 0.14f), hairC);
                    break;
                case 2: // 鮑伯頭
                    TM.Prim(PrimitiveType.Sphere, "Bob", body, new Vector3(0, 1.18f, -0.06f), new Vector3(0.76f, 0.66f, 0.7f), hairC);
                    break;
                case 3: // 包包頭
                case 5:
                    TM.Prim(PrimitiveType.Sphere, "Bun", body, kind == 3 ? new Vector3(0, 1.62f, -0.06f) : new Vector3(0, 1.36f, -0.32f), Vector3.one * 0.26f, hairC);
                    break;
                case 4: // 捲髮
                    for (int i = 0; i < 6; i++)
                        TM.Prim(PrimitiveType.Sphere, "Curl", body, new Vector3(-0.22f + i * 0.09f, 1.5f + (i % 2) * 0.04f, 0.02f + (i % 3) * 0.04f), Vector3.one * 0.22f, hairC);
                    break;
            }

            // 帽子
            if (kind == 0 || kind == 7)
            {
                var knit = kind == 0 ? TM.Hex("#F1E9DA") : TM.Hex("#E3A93A");
                TM.Prim(PrimitiveType.Sphere, "Beanie", body, new Vector3(0, 1.38f, -0.02f), new Vector3(0.7f, 0.5f, 0.68f), knit);
                TM.Prim(PrimitiveType.Cylinder, "Cuff", body, new Vector3(0, 1.32f, -0.02f), new Vector3(0.72f, 0.07f, 0.7f), knit * 0.88f);
                TM.Prim(PrimitiveType.Sphere, "Pom", body, new Vector3(0, 1.66f, -0.02f), Vector3.one * 0.2f, knit);
                TM.Prim(PrimitiveType.Sphere, "SnowCap", body, new Vector3(0, 1.62f, -0.02f), new Vector3(0.3f, 0.08f, 0.3f), Color.white);
            }
            else if (kind == 6)
            {
                TM.Prim(PrimitiveType.Sphere, "FlatCap", body, new Vector3(0, 1.44f, 0.02f), new Vector3(0.74f, 0.3f, 0.74f), TM.Hex("#8A5A3C"));
                TM.MeshObj("Brim", body, TM.RoundedBox(new Vector3(0.5f, 0.05f, 0.26f), 0.03f, 3), new Vector3(0, 1.4f, 0.32f), TM.Hex("#7A4E34"));
                TM.Prim(PrimitiveType.Sphere, "SnowCap", body, new Vector3(0, 1.57f, 0), new Vector3(0.42f, 0.08f, 0.42f), Color.white);
            }
            else if (kind == 8)
            {
                TM.Prim(PrimitiveType.Cylinder, "Headband", body, new Vector3(0, 1.38f, 0), new Vector3(0.71f, 0.05f, 0.69f), Color.white);
            }

            // 手上的東西
            if (pack) TM.MeshObj("Pack", body, TM.RoundedBox(new Vector3(0.44f, 0.46f, 0.2f), 0.09f, 4), new Vector3(0, 0.66f, -0.32f), kind == 2 ? TM.Hex("#1E2440") : TM.Hex("#6E4A30"));
            if (kind == 0 || (phone && handsFront && kind != 5))
                TM.MeshObj("Phone", body, TM.RoundedBox(new Vector3(0.14f, 0.24f, 0.03f), 0.03f, 3), new Vector3(0, 0.74f, 0.34f), TM.Hex("#F07A9A"), 0, new Vector3(-30, 0, 0));
            if (kind == 3) // 托特包
            {
                TM.MeshObj("Tote", body, TM.RoundedBox(new Vector3(0.36f, 0.34f, 0.12f), 0.04f, 3), new Vector3(0.42f, 0.42f, 0.04f), TM.Hex("#F1E9DA"));
                TM.Prim(PrimitiveType.Cube, "ToteBase", body, new Vector3(0.42f, 0.27f, 0.04f), new Vector3(0.37f, 0.05f, 0.13f), TM.Hex("#7A4E34"));
                TM.Prim(PrimitiveType.Cube, "Strap", body, new Vector3(0.38f, 0.72f, 0.04f), new Vector3(0.04f, 0.42f, 0.03f), TM.Hex("#7A4E34"), 0, new Vector3(0, 0, 12));
            }
            if (kind == 4) // 購物袋
            {
                TM.MeshObj("BagRed", body, TM.RoundedBox(new Vector3(0.34f, 0.4f, 0.14f), 0.03f, 3), new Vector3(-0.44f, 0.28f, 0.08f), TM.Hex("#D83A32"));
                TM.MeshObj("BagKraft", body, TM.RoundedBox(new Vector3(0.28f, 0.32f, 0.13f), 0.03f, 3), new Vector3(0.44f, 0.26f, 0.08f), TM.Hex("#C9A06A"));
                foreach (var x in new[] { -0.44f, 0.44f })
                    TM.MeshObj("Handle", body, TM.Torus(0.07f, 0.012f, 12, 4), new Vector3(x, x < 0 ? 0.5f : 0.44f, 0.08f), TM.Hex("#3A2E2A"), 0, new Vector3(90, 0, 0));
            }
            if (kind == 5) // 斜背包
            {
                TM.MeshObj("Pouch", body, TM.RoundedBox(new Vector3(0.24f, 0.18f, 0.1f), 0.04f, 3), new Vector3(0.2f, 0.46f, 0.25f), TM.Hex("#8A5A3C"));
                TM.Prim(PrimitiveType.Cube, "Strap", body, new Vector3(0, 0.7f, 0.25f), new Vector3(0.04f, 0.56f, 0.02f), TM.Hex("#6E4A30"), 0, new Vector3(0, 0, 40));
            }
            if (kind == 6) // 拐杖
            {
                TM.Prim(PrimitiveType.Cylinder, "Cane", body, new Vector3(0.12f, 0.33f, 0.36f), new Vector3(0.05f, 0.33f, 0.05f), TM.Hex("#6E4A30"));
                TM.Prim(PrimitiveType.Sphere, "CaneTop", body, new Vector3(0.12f, 0.68f, 0.36f), new Vector3(0.1f, 0.07f, 0.16f), TM.Hex("#6E4A30"));
            }
            if (umbrella || kind == 1) Umbrella(body, TM.Hex("#D8343A"));

            var bob = body.gameObject.AddComponent<Bob>();
            bob.amp = 0.015f; bob.speed = 1.5f + Rnd() * 1.5f;
            return t;
        }

        /// <summary>撐著的雨傘（傘面上有積雪點點）。</summary>
        static void Umbrella(Transform body, Color c)
        {
            var u = TM.Empty("Umbrella", body, new Vector3(0.24f, 0, 0.14f));
            u.localEulerAngles = new Vector3(-4, 0, 7); // 往頭上斜
            TM.Prim(PrimitiveType.Cylinder, "Shaft", u, new Vector3(0, 1.45f, 0), new Vector3(0.03f, 0.68f, 0.03f), TM.Hex("#3A2A20"));
            TM.Prim(PrimitiveType.Sphere, "Hand", u, new Vector3(0, 0.82f, 0), Vector3.one * 0.14f, TM.Hex("#3A2E2A"));
            var top = TM.Empty("Canopy", u, new Vector3(0, 2.1f, 0));
            TM.MeshObj("Cloth", top, TM.Cone(1.0f, 0.42f, 10), Vector3.zero, c);
            TM.MeshObj("Under", top, TM.Cone(0.98f, 0.38f, 10), new Vector3(0, -0.02f, 0), c * 0.75f, 0, new Vector3(180, 0, 0), new Vector3(1, 0.15f, 1));
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f * Mathf.Deg2Rad;
                TM.Prim(PrimitiveType.Sphere, "Tip", top, new Vector3(Mathf.Cos(a), -0.02f, Mathf.Sin(a)) * 0.98f, Vector3.one * 0.05f, TM.Hex("#3A2A20"));
                if (i % 2 == 0) TM.Prim(PrimitiveType.Sphere, "SnowDot", top, new Vector3(Mathf.Cos(a + 0.3f) * 0.55f, 0.2f, Mathf.Sin(a + 0.3f) * 0.55f), Vector3.one * 0.07f, Color.white);
            }
            TM.Prim(PrimitiveType.Sphere, "Ferrule", top, new Vector3(0, 0.44f, 0), Vector3.one * 0.06f, TM.Hex("#3A2A20"));
        }

        /// <summary>木造攤位：櫃台、貨箱、木柱、雪屋頂、條紋遮棚、紙燈籠、招牌。</summary>
        public static Transform Stall(Transform p, Vector3 pos, float yaw, string sign, Color canopy)
        {
            var t = TM.Empty("Stall_" + sign, p, pos);
            t.localEulerAngles = new Vector3(0, yaw, 0);
            Color wood = TM.Hex("#9A6B47"), dark = TM.Hex("#6E4A30");
            TM.Prim(PrimitiveType.Cube, "Counter", t, new Vector3(0, 0.5f, 0.1f), new Vector3(2.8f, 1.0f, 1.1f), wood, 0, null, true);
            TM.Prim(PrimitiveType.Cube, "CounterTop", t, new Vector3(0, 1.03f, 0.15f), new Vector3(3.0f, 0.08f, 1.25f), dark);
            for (int i = -2; i <= 2; i++)
                TM.Prim(PrimitiveType.Cube, "Plank", t, new Vector3(i * 0.56f, 0.5f, 0.66f), new Vector3(0.04f, 0.9f, 0.02f), dark);
            // 貨箱與商品
            for (int i = 0; i < 3; i++)
            {
                float x = -0.9f + i * 0.9f;
                TM.Prim(PrimitiveType.Cube, "Crate", t, new Vector3(x, 1.15f, 0.25f), new Vector3(0.75f, 0.18f, 0.6f), TM.Hex("#C9A06A"));
                Color g = TM.Hex(new[] { "#FF8A6B", "#E2463B", "#FFC93C", "#87D068", "#F4F1EC" }[(int)(Rnd() * 5) % 5]);
                for (int k = 0; k < 4; k++)
                    TM.Prim(PrimitiveType.Sphere, "Goods", t, new Vector3(x - 0.2f + (k % 2) * 0.4f, 1.3f, 0.12f + (k / 2) * 0.26f), new Vector3(0.24f, 0.16f, 0.2f), g);
            }
            // 柱子
            foreach (var x in new[] { -1.35f, 1.35f })
            {
                TM.Prim(PrimitiveType.Cube, "PostF", t, new Vector3(x, 1.4f, 0.65f), new Vector3(0.14f, 2.8f, 0.14f), dark);
                TM.Prim(PrimitiveType.Cube, "PostB", t, new Vector3(x, 1.6f, -0.45f), new Vector3(0.14f, 3.2f, 0.14f), dark);
            }
            // 斜屋頂＋積雪
            TM.Prim(PrimitiveType.Cube, "Roof", t, new Vector3(0, 3.05f, 0.15f), new Vector3(3.3f, 0.14f, 1.8f), TM.Hex("#3F4E7A"), 0, new Vector3(-14, 0, 0));
            TM.MeshObj("RoofSnow", t, TM.RoundedBox(new Vector3(3.2f, 0.18f, 1.6f), 0.08f, 4), new Vector3(0, 3.17f, 0.12f), Color.white, 0, new Vector3(-14, 0, 0));
            // 前面的條紋遮棚
            for (int i = 0; i < 6; i++)
                TM.Prim(PrimitiveType.Cube, "Awning", t, new Vector3(-1.25f + i * 0.5f, 2.55f, 0.95f), new Vector3(0.5f, 0.08f, 0.7f), i % 2 == 0 ? canopy : Color.white, 0, new Vector3(-28, 0, 0));
            // 燈籠
            foreach (var x in new[] { -1.1f, 1.1f })
            {
                TM.Prim(PrimitiveType.Cylinder, "String", t, new Vector3(x, 2.3f, 1.05f), new Vector3(0.02f, 0.1f, 0.02f), TM.Ink);
                TM.Prim(PrimitiveType.Sphere, "Lantern", t, new Vector3(x, 2.0f, 1.05f), new Vector3(0.32f, 0.42f, 0.32f), TM.Hex("#FF6B4A"), 1.3f);
                TM.Prim(PrimitiveType.Cylinder, "Cap", t, new Vector3(x, 2.22f, 1.05f), new Vector3(0.2f, 0.03f, 0.2f), TM.Ink);
            }
            // 招牌（在屋頂上，面向客人）
            TM.Prim(PrimitiveType.Cube, "SignBoard", t, new Vector3(0, 3.55f, 0.55f), new Vector3(Mathf.Max(1.4f, sign.Length * 0.5f + 0.5f), 0.62f, 0.08f), TM.Hex("#FFF4E0"));
            TM.Prim(PrimitiveType.Cube, "SignFrame", t, new Vector3(0, 3.55f, 0.5f), new Vector3(Mathf.Max(1.4f, sign.Length * 0.5f + 0.5f) + 0.12f, 0.74f, 0.06f), dark);
            TM.Text3D(sign, t, new Vector3(0, 3.55f, 0.6f), 0.42f, TM.Ink, new Vector3(0, 180, 0));
            TownBuilder.MapAdd(pos.x, pos.z, 3f, 1.6f, canopy, 0.5f);
            return t;
        }

        /// <summary>日式小店：木牆、瓦屋頂（積雪）、暖簾、紙燈籠、亮著的店面、招牌、門口的貨架。</summary>
        public static Transform JShop(Transform p, Vector3 pos, float yaw, Vector3 size, Color roof, string sign, Color glow)
        {
            var t = TM.Empty("JShop_" + sign, p, pos);
            t.localEulerAngles = new Vector3(0, yaw, 0);
            float fz = size.z * 0.5f;
            Color wood = TM.Hex("#8A5A3B"), dark = TM.Hex("#5E3D27"), plaster = TM.Hex("#F2E6D3");
            TM.Prim(PrimitiveType.Cube, "Walls", t, new Vector3(0, size.y * 0.5f, 0), size, plaster, 0, null, true);
            TownBuilder.MapAdd(pos.x, pos.z, Mathf.Abs(Mathf.Sin(yaw * Mathf.Deg2Rad)) > 0.5f ? size.z : size.x, Mathf.Abs(Mathf.Sin(yaw * Mathf.Deg2Rad)) > 0.5f ? size.x : size.z, roof, 0.5f);
            // 木框
            foreach (var x in new[] { -size.x * 0.5f + 0.1f, size.x * 0.5f - 0.1f })
                TM.Prim(PrimitiveType.Cube, "Beam", t, new Vector3(x, size.y * 0.5f, fz + 0.03f), new Vector3(0.22f, size.y, 0.08f), dark);
            TM.Prim(PrimitiveType.Cube, "Lintel", t, new Vector3(0, 2.75f, fz + 0.04f), new Vector3(size.x, 0.2f, 0.1f), dark);
            // 一樓：亮著的店面＋暖簾
            TM.Prim(PrimitiveType.Cube, "ShopGlow", t, new Vector3(0, 1.3f, fz + 0.02f), new Vector3(size.x - 0.8f, 2.3f, 0.04f), glow, 1.1f);
            for (int i = 0; i < 4; i++)
                TM.Prim(PrimitiveType.Cube, "Noren", t, new Vector3(-0.75f + i * 0.5f, 2.3f, fz + 0.1f), new Vector3(0.46f, 0.7f, 0.03f), roof);
            // 二樓窗
            if (size.y > 4f)
                for (int s = -1; s <= 1; s += 2)
                {
                    TM.Prim(PrimitiveType.Cube, "Window", t, new Vector3(s * size.x * 0.25f, size.y - 1.2f, fz + 0.02f), new Vector3(1.0f, 0.8f, 0.04f), TM.Hex("#FFE2A8"), 0.9f);
                    TM.Prim(PrimitiveType.Cube, "Lattice", t, new Vector3(s * size.x * 0.25f, size.y - 1.2f, fz + 0.05f), new Vector3(0.05f, 0.8f, 0.02f), dark);
                }
            // 屋簷（瓦屋頂）＋積雪
            TM.Prim(PrimitiveType.Cube, "Eave", t, new Vector3(0, 3.1f, fz + 0.55f), new Vector3(size.x + 0.4f, 0.12f, 1.3f), roof, 0, new Vector3(-18, 0, 0));
            TM.MeshObj("EaveSnow", t, TM.RoundedBox(new Vector3(size.x + 0.3f, 0.16f, 1.1f), 0.07f, 4), new Vector3(0, 3.22f, fz + 0.5f), Color.white, 0, new Vector3(-18, 0, 0));
            TM.MeshObj("Roof", t, TM.Cone(Mathf.Max(size.x, size.z) * 0.78f, 1.6f, 4), new Vector3(0, size.y, 0), roof, 0, new Vector3(0, 45, 0), new Vector3(size.x / Mathf.Max(size.x, size.z), 1, size.z / Mathf.Max(size.x, size.z)));
            TM.MeshObj("RoofSnow", t, TM.Cone(Mathf.Max(size.x, size.z) * 0.5f, 1.0f, 4), new Vector3(0, size.y + 0.62f, 0), Color.white, 0, new Vector3(0, 45, 0), new Vector3(size.x / Mathf.Max(size.x, size.z), 1, size.z / Mathf.Max(size.x, size.z)));
            // 招牌
            float sw = Mathf.Min(size.x - 0.6f, sign.Length * 0.6f + 1f);
            TM.Prim(PrimitiveType.Cube, "SignFrame", t, new Vector3(0, 3.75f, fz + 0.12f), new Vector3(sw + 0.14f, 0.86f, 0.08f), dark);
            TM.Prim(PrimitiveType.Cube, "SignBoard", t, new Vector3(0, 3.75f, fz + 0.17f), new Vector3(sw, 0.72f, 0.06f), TM.Hex("#FFF4E0"));
            TM.Text3D(sign, t, new Vector3(0, 3.75f, fz + 0.22f), 0.48f, TM.Ink, new Vector3(0, 180, 0));
            // 燈籠
            foreach (var x in new[] { -size.x * 0.5f + 0.45f, size.x * 0.5f - 0.45f })
            {
                TM.Prim(PrimitiveType.Sphere, "Lantern", t, new Vector3(x, 2.35f, fz + 0.45f), new Vector3(0.36f, 0.5f, 0.36f), TM.Hex("#FF6B4A"), 1.3f);
                TM.Prim(PrimitiveType.Cylinder, "Cap", t, new Vector3(x, 2.62f, fz + 0.45f), new Vector3(0.22f, 0.03f, 0.22f), TM.Ink);
            }
            // 門口貨架
            TM.Prim(PrimitiveType.Cube, "Shelf", t, new Vector3(size.x * 0.28f, 0.45f, fz + 0.55f), new Vector3(1.1f, 0.9f, 0.6f), wood);
            for (int k = 0; k < 3; k++)
                TM.Prim(PrimitiveType.Cube, "Box", t, new Vector3(size.x * 0.28f - 0.35f + k * 0.35f, 1.0f, fz + 0.55f), new Vector3(0.28f, 0.22f, 0.3f), TM.Hex(new[] { "#FF8AD8", "#9CCBF0", "#FFC93C" }[k]));
            TM.Prim(PrimitiveType.Cube, "Chalkboard", t, new Vector3(-size.x * 0.3f, 0.55f, fz + 0.7f), new Vector3(0.6f, 0.9f, 0.06f), TM.Hex("#2E3A33"), 0, new Vector3(-10, 0, 0));
            return t;
        }

        /// <summary>石板路：底色＋一塊塊淺色石板。</summary>
        public static void PathTiles(Transform p, Vector3 center, float width, float length, Color baseC, bool wet = false)
        {
            var pb = TM.Prim(PrimitiveType.Cube, "PathBase", p, new Vector3(center.x, center.y + 0.015f, center.z), new Vector3(width, 0.02f, length), baseC);
            if (wet) pb.GetComponent<Renderer>().sharedMaterial = TM.Mat(baseC, 0f, 0.82f); // 下過雨，亮亮的會反光
            Color light = Color.Lerp(baseC, Color.white, 0.35f), shade = Color.Lerp(baseC, TM.Hex("#8E8598"), 0.25f);
            float tile = 1.5f;
            int nx = Mathf.Max(1, Mathf.FloorToInt(width / tile)), nz = Mathf.Max(1, Mathf.FloorToInt(length / tile));
            for (int i = 0; i < nx; i++)
                for (int k = 0; k < nz; k++)
                {
                    if ((i + k) % 2 == 1 && Rnd() < 0.5f) continue;
                    float x = center.x - width * 0.5f + tile * (i + 0.5f) + (width - nx * tile) * 0.5f;
                    float z = center.z - length * 0.5f + tile * (k + 0.5f) + (length - nz * tile) * 0.5f;
                    var tc = Rnd() < 0.5f ? light : shade;
                    var tile1 = TM.Prim(PrimitiveType.Cube, "Tile", p, new Vector3(x, center.y + 0.03f, z), new Vector3(tile - 0.14f, 0.02f, tile - 0.14f), tc);
                    if (wet) tile1.GetComponent<Renderer>().sharedMaterial = TM.Mat(tc, 0f, 0.82f);
                }
        }

        /// <summary>拍照點：地上一圈金色光環＋立牌。</summary>
        public static Transform PhotoSpot(Transform p, Vector3 pos, float yaw)
        {
            var t = TM.Empty("PhotoSpot", p, pos);
            t.localEulerAngles = new Vector3(0, yaw, 0);
            TM.Prim(PrimitiveType.Cylinder, "Mat", t, new Vector3(0, 0.02f, 0), new Vector3(1.8f, 0.01f, 1.8f), TM.Hex("#FFE7A8"));
            TM.MeshObj("Ring", t, TM.Torus(0.9f, 0.06f, 32, 6), new Vector3(0, 0.04f, 0), TM.Hex("#FFC93C"), 1.2f, null, new Vector3(1, 0.4f, 1));
            var post = TM.Empty("Stand", t, new Vector3(1.3f, 0, 0));
            TM.Prim(PrimitiveType.Cylinder, "Pole", post, new Vector3(0, 0.6f, 0), new Vector3(0.08f, 0.6f, 0.08f), TM.Hex("#6E4A30"));
            var board = TM.Empty("Board", post, new Vector3(0, 1.45f, 0));
            TM.Prim(PrimitiveType.Cube, "Panel", board, Vector3.zero, new Vector3(1.0f, 0.5f, 0.06f), TM.Hex("#FF6B35"));
            TM.Text3D("拍照點", board, new Vector3(0, 0, -0.05f), 0.24f, Color.white);
            board.gameObject.AddComponent<FaceCamera>();
            return t;
        }

        /// <summary>自助機台（整理券機）：門面朝 yaw。</summary>
        public static Transform Kiosk(Transform p, Vector3 pos, float yaw, string label)
        {
            var t = TM.Empty("Kiosk", p, pos);
            t.localEulerAngles = new Vector3(0, yaw, 0);
            TM.MeshObj("Body", t, TM.RoundedBox(new Vector3(1.1f, 1.9f, 0.8f), 0.12f, 4), new Vector3(0, 0.95f, 0), TM.Hex("#3E8EDE"));
            TM.Prim(PrimitiveType.Cube, "Hit", t, new Vector3(0, 0.95f, 0), new Vector3(1.1f, 1.9f, 0.8f), Color.white, 0, null, true).GetComponent<Renderer>().enabled = false;
            TM.Prim(PrimitiveType.Cube, "Screen", t, new Vector3(0, 1.3f, 0.41f), new Vector3(0.8f, 0.55f, 0.02f), TM.Hex("#BFF3FF"), 1.4f);
            TM.Prim(PrimitiveType.Cube, "Slot", t, new Vector3(0, 0.75f, 0.41f), new Vector3(0.5f, 0.06f, 0.02f), TM.Ink);
            TM.Prim(PrimitiveType.Cube, "Header", t, new Vector3(0, 2.15f, 0.1f), new Vector3(1.3f, 0.45f, 0.1f), TM.Hex("#FFC93C"));
            TM.Text3D(label, t, new Vector3(0, 2.15f, 0.17f), 0.26f, TM.Ink, new Vector3(0, 180, 0));
            TM.Prim(PrimitiveType.Sphere, "Snow", t, new Vector3(0, 1.93f, 0), new Vector3(1.0f, 0.12f, 0.7f), Color.white);
            return t;
        }

        /// <summary>去趣 eSIM 攤位：木攤＋橘色遮棚＋手機立牌＋QR 牌。</summary>
        public static Transform EsimBooth(Transform p, Vector3 pos, float yaw)
        {
            var t = Stall(p, pos, yaw, "去趣 eSIM", TM.Hex("#FF6B35"));
            // 大手機立牌
            var phone = TM.Empty("PhoneStandee", t, new Vector3(1.9f, 0, 0.6f));
            TM.MeshObj("Phone", phone, TM.RoundedBox(new Vector3(0.9f, 1.6f, 0.12f), 0.14f, 4), new Vector3(0, 1.2f, 0), TM.Ink);
            TM.Prim(PrimitiveType.Cube, "Display", phone, new Vector3(0, 1.22f, 0.07f), new Vector3(0.74f, 1.3f, 0.01f), TM.Hex("#FFF2C4"), 1.2f);
            for (int i = 0; i < 4; i++)
                TM.Prim(PrimitiveType.Cube, "Bar", phone, new Vector3(-0.21f + i * 0.14f, 1.5f + i * 0.04f, 0.08f), new Vector3(0.09f, 0.1f + i * 0.08f, 0.01f), TM.Hex("#2F9E62"));
            TM.Text3D("5G", phone, new Vector3(0, 1.0f, 0.09f), 0.28f, TM.Hex("#FF6B35"), new Vector3(0, 180, 0));
            TM.Prim(PrimitiveType.Cylinder, "Foot", phone, new Vector3(0, 0.2f, 0), new Vector3(0.6f, 0.2f, 0.6f), TM.Hex("#6E4A30"));
            // 櫃台上的 QR 牌
            TM.Prim(PrimitiveType.Cube, "QR", t, new Vector3(-0.8f, 1.35f, 0.45f), new Vector3(0.42f, 0.42f, 0.04f), Color.white, 0, new Vector3(-15, 0, 0));
            TM.Prim(PrimitiveType.Cube, "QRInk", t, new Vector3(-0.8f, 1.35f, 0.475f), new Vector3(0.26f, 0.26f, 0.01f), TM.Ink, 0, new Vector3(-15, 0, 0));
            return t;
        }

        // ---------------- 道頓堀河岸（照場景參考圖）----------------

        /// <summary>石柱燈：方形石柱＋發光燈箱＋小屋頂＋積雪。</summary>
        public static void StonePost(Transform p, Vector3 pos)
        {
            var t = TM.Empty("StonePost", p, pos);
            Color stone = TM.Hex("#B9B0A4");
            TM.Prim(PrimitiveType.Cube, "Pillar", t, new Vector3(0, 0.55f, 0), new Vector3(0.42f, 1.1f, 0.42f), stone, 0, null, true);
            TM.Prim(PrimitiveType.Cube, "Cap", t, new Vector3(0, 1.13f, 0), new Vector3(0.52f, 0.08f, 0.52f), stone * 0.92f);
            TM.Prim(PrimitiveType.Cube, "Lamp", t, new Vector3(0, 1.38f, 0), new Vector3(0.34f, 0.42f, 0.34f), TM.Hex("#FFD27A"), 1.6f);
            foreach (var x in new[] { -0.17f, 0.17f })
                foreach (var z in new[] { -0.17f, 0.17f })
                    TM.Prim(PrimitiveType.Cube, "Frame", t, new Vector3(x, 1.38f, z), new Vector3(0.05f, 0.44f, 0.05f), TM.Hex("#3A2A20"));
            TM.MeshObj("Roof", t, TM.Cone(0.36f, 0.22f, 4), new Vector3(0, 1.6f, 0), TM.Hex("#3A2A20"), 0, new Vector3(0, 45, 0));
            TM.Prim(PrimitiveType.Sphere, "Snow", t, new Vector3(0, 1.7f, 0), new Vector3(0.36f, 0.12f, 0.36f), Color.white);
        }

        /// <summary>石砌花台：雪＋綠葉＋小花。</summary>
        public static void Planter(Transform p, Vector3 pos, float yaw, float len)
        {
            var t = TM.Empty("Planter", p, pos);
            t.localEulerAngles = new Vector3(0, yaw, 0);
            TM.Prim(PrimitiveType.Cube, "Box", t, new Vector3(0, 0.25f, 0), new Vector3(len, 0.5f, 0.5f), TM.Hex("#C9BFB2"), 0, null, true);
            TM.Prim(PrimitiveType.Cube, "Rim", t, new Vector3(0, 0.52f, 0), new Vector3(len + 0.06f, 0.06f, 0.56f), TM.Hex("#D8CFC2"));
            string[] flowers = { "#FF8AA8", "#FFFFFF", "#FFC93C", "#E2463B" };
            int n = Mathf.Max(2, Mathf.RoundToInt(len / 0.35f));
            for (int i = 0; i < n; i++)
            {
                float x = -len * 0.5f + 0.18f + i * (len - 0.36f) / Mathf.Max(1, n - 1);
                TM.Prim(PrimitiveType.Sphere, "Leaf", t, new Vector3(x, 0.62f, (i % 2) * 0.1f - 0.05f), new Vector3(0.36f, 0.26f, 0.34f), TM.Hex("#3F7A5A"));
                TM.Prim(PrimitiveType.Sphere, "Flower", t, new Vector3(x + 0.05f, 0.74f, 0.1f), Vector3.one * 0.1f, TM.Hex(flowers[i % flowers.Length]), 0.2f);
                if (i % 2 == 0) TM.Prim(PrimitiveType.Sphere, "Snow", t, new Vector3(x - 0.04f, 0.75f, -0.05f), new Vector3(0.2f, 0.07f, 0.18f), Color.white);
            }
        }

        /// <summary>河岸木欄杆（有碰撞，玩家不會掉下河）：每段兩根橫木＋細木柱。</summary>
        public static void WoodRail(Transform p, float z, float x0, float x1)
        {
            float len = x1 - x0, cx = (x0 + x1) * 0.5f;
            var col = TM.Prim(PrimitiveType.Cube, "RailCollider", p, new Vector3(cx, 0.55f, z), new Vector3(len, 1.1f, 0.15f), Color.white, 0, null, true);
            TM.Kill(col.GetComponent<MeshRenderer>());
            Color wood = TM.Hex("#6E4A30");
            TM.Prim(PrimitiveType.Cube, "RailTop", p, new Vector3(cx, 0.95f, z), new Vector3(len, 0.1f, 0.12f), wood);
            TM.Prim(PrimitiveType.Cube, "RailSnow", p, new Vector3(cx, 1.02f, z), new Vector3(len, 0.04f, 0.1f), Color.white);
            TM.Prim(PrimitiveType.Cube, "RailMid", p, new Vector3(cx, 0.55f, z), new Vector3(len, 0.07f, 0.08f), wood);
            for (float x = x0 + 0.6f; x < x1; x += 0.6f)
                TM.Prim(PrimitiveType.Cube, "Baluster", p, new Vector3(x, 0.5f, z), new Vector3(0.06f, 0.9f, 0.06f), wood * 0.9f);
        }

        /// <summary>觀光船：黃色船身、亮燈的船艙、屋頂燈泡串、乘客剪影。會在河上來回開。</summary>
        public static Transform TourBoat(Transform p, Vector3 pos, float xMin, float xMax)
        {
            var t = TM.Empty("TourBoat", p, pos);
            var hull = TM.Empty("Hull", t, Vector3.zero);
            TM.MeshObj("Body", hull, TM.RoundedBox(new Vector3(5.2f, 0.55f, 1.9f), 0.25f, 5), new Vector3(0, 0, 0), TM.Hex("#F2B33D"));
            TM.Prim(PrimitiveType.Cube, "Stripe", hull, new Vector3(0, 0.05f, -0.96f), new Vector3(4.8f, 0.12f, 0.02f), TM.Hex("#C8553D"));
            TM.Prim(PrimitiveType.Cube, "Stripe2", hull, new Vector3(0, 0.05f, 0.96f), new Vector3(4.8f, 0.12f, 0.02f), TM.Hex("#C8553D"));
            TM.MeshObj("Cabin", hull, TM.RoundedBox(new Vector3(3.8f, 0.7f, 1.6f), 0.12f, 4), new Vector3(0, 0.6f, 0), TM.Hex("#FFF2D6"));
            foreach (var z in new[] { -0.81f, 0.81f })
                TM.Prim(PrimitiveType.Cube, "Windows", hull, new Vector3(0, 0.62f, z), new Vector3(3.5f, 0.38f, 0.02f), TM.Hex("#FFD27A"), 1.4f);
            for (int i = 0; i < 6; i++)
                TM.Prim(PrimitiveType.Sphere, "Passenger", hull, new Vector3(-1.4f + i * 0.56f, 0.62f, (i % 2 == 0 ? -0.5f : 0.5f)), Vector3.one * 0.3f, TM.Hex("#5A3A28"));
            TM.MeshObj("Roof", hull, TM.RoundedBox(new Vector3(4.0f, 0.1f, 1.8f), 0.04f, 3), new Vector3(0, 1.0f, 0), TM.Hex("#C8553D"));
            TM.Prim(PrimitiveType.Cube, "RoofSnow", hull, new Vector3(0, 1.07f, 0), new Vector3(3.8f, 0.04f, 1.6f), Color.white);
            for (int i = 0; i < 12; i++)
                foreach (var z in new[] { -0.9f, 0.9f })
                    TM.Prim(PrimitiveType.Sphere, "Bulb", hull, new Vector3(-1.9f + i * 0.345f, 1.0f, z), Vector3.one * 0.09f, TM.Hex("#FFE7A8"), 2f);
            var l = new GameObject("BoatLight").AddComponent<Light>();
            l.type = LightType.Point; l.color = TM.Hex("#FFC27A"); l.range = 7f; l.intensity = 1.4f;
            l.transform.SetParent(t, false); l.transform.localPosition = new Vector3(0, 1.4f, 0);
            var c = t.gameObject.AddComponent<Cruise>();
            c.xMin = xMin; c.xMax = xMax;
            hull.gameObject.AddComponent<Bob>().amp = 0.04f;
            return t;
        }

        /// <summary>直立紙燈籠（大）：紅底、寫字、上下黑框。</summary>
        public static Transform BigLantern(Transform p, Vector3 pos, string text)
        {
            var t = TM.Empty("BigLantern", p, pos);
            TM.Prim(PrimitiveType.Capsule, "Paper", t, Vector3.zero, new Vector3(0.75f, 0.6f, 0.75f), TM.Hex("#E2463B"), 0.6f);
            TM.Prim(PrimitiveType.Cylinder, "Top", t, new Vector3(0, 0.62f, 0), new Vector3(0.5f, 0.05f, 0.5f), TM.Ink);
            TM.Prim(PrimitiveType.Cylinder, "Bottom", t, new Vector3(0, -0.62f, 0), new Vector3(0.5f, 0.05f, 0.5f), TM.Ink);
            var sb = new System.Text.StringBuilder();
            foreach (char ch in text) { if (sb.Length > 0) sb.Append('\n'); sb.Append(ch); }
            var tm = TM.Text3D(sb.ToString(), t, new Vector3(0, 0, 0.39f), 0.3f, TM.Hex("#2A1A1A"), new Vector3(0, 180, 0));
            tm.lineSpacing = 0.9f;
            t.gameObject.AddComponent<Bob>().amp = 0.03f;
            return t;
        }
    }
}

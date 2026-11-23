using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 每種人格的眼睛與配件。配件一律掛在 Ququ 的插槽上，之後換成正式模型時，
    /// 只要把這裡的程式造型換成 Prefab 即可，插槽位置不用動。
    /// </summary>
    public static class QuquOutfits
    {
        public static void Apply(Ququ q, Persona p)
        {
            switch (p)
            {
                case Persona.Soldier: Soldier(q); break;
                case Persona.Capybara: Capybara(q); break;
                case Persona.Camera: CameraHunter(q); break;
                case Persona.Taxi: Taxi(q); break;
                case Persona.Shopper: Shopper(q); break;
                case Persona.Accountant: Accountant(q); break;
                case Persona.Explorer: Explorer(q); break;
                case Persona.Nanny: Nanny(q); break;
                default: q.MakeEyes("round"); break;
            }
        }

        // 行程特種兵：紅色運動頭帶＋飄帶、哨子、碼錶
        static void Soldier(Ququ q)
        {
            q.MakeEyes("focused");
            var red = TM.Hex("#FF4D4F");
            var head = q.slotHead;
            float y = -0.12f; // 頭帶在身體頂端往下一點
            float hw = Ququ.W * 0.5f, hd = Ququ.D * 0.5f;
            TM.Prim(PrimitiveType.Cube, "BandF", head, new Vector3(0, y, hd + 0.012f), new Vector3(Ququ.W - 0.16f, 0.12f, 0.03f), red);
            TM.Prim(PrimitiveType.Cube, "BandB", head, new Vector3(0, y, -hd - 0.012f), new Vector3(Ququ.W - 0.16f, 0.12f, 0.03f), red);
            TM.Prim(PrimitiveType.Cube, "BandL", head, new Vector3(-hw - 0.012f, y, 0), new Vector3(0.03f, 0.12f, Ququ.D - 0.16f), red);
            TM.Prim(PrimitiveType.Cube, "BandR", head, new Vector3(hw + 0.012f, y, 0), new Vector3(0.03f, 0.12f, Ququ.D - 0.16f), red);
            TM.Prim(PrimitiveType.Cube, "Tail1", head, new Vector3(hw + 0.15f, y + 0.04f, -0.12f), new Vector3(0.3f, 0.07f, 0.02f), red, 0, new Vector3(0, 25, 20));
            TM.Prim(PrimitiveType.Cube, "Tail2", head, new Vector3(hw + 0.14f, y - 0.06f, -0.14f), new Vector3(0.28f, 0.07f, 0.02f), red, 0, new Vector3(0, 25, -15));

            // 哨子＋紅繩
            var chest = q.slotChest;
            TM.Prim(PrimitiveType.Cube, "CordL", chest, new Vector3(-0.2f, 0.17f, 0.012f), new Vector3(0.5f, 0.02f, 0.01f), red, 0, new Vector3(0, 0, -35));
            TM.Prim(PrimitiveType.Cube, "CordR", chest, new Vector3(0.2f, 0.17f, 0.012f), new Vector3(0.5f, 0.02f, 0.01f), red, 0, new Vector3(0, 0, 35));
            TM.Prim(PrimitiveType.Capsule, "Whistle", chest, new Vector3(0, 0.02f, 0.04f), new Vector3(0.08f, 0.07f, 0.08f), TM.Hex("#D9DEE5"), 0, new Vector3(0, 0, 90));

            // 碼錶（右手）
            var h = q.slotHandR;
            TM.Prim(PrimitiveType.Cylinder, "Stopwatch", h, Vector3.zero, new Vector3(0.16f, 0.025f, 0.16f), TM.Hex("#D9DEE5"), 0, new Vector3(90, 0, 0));
            TM.Prim(PrimitiveType.Cylinder, "Face", h, new Vector3(0, 0, 0.027f), new Vector3(0.12f, 0.005f, 0.12f), Color.white, 0, new Vector3(90, 0, 0));
            TM.Prim(PrimitiveType.Cube, "Button", h, new Vector3(0, 0.1f, 0), new Vector3(0.04f, 0.04f, 0.04f), TM.Ink);
        }

        // 隨性慢活水豚：頭頂柚子、充氣頸枕、冰咖啡
        static void Capybara(Ququ q)
        {
            q.MakeEyes("sleepy");
            var head = q.slotHead;
            TM.Prim(PrimitiveType.Sphere, "Yuzu", head, new Vector3(0, 0.36f, 0), new Vector3(0.24f, 0.22f, 0.24f), TM.Hex("#FFC93C"));
            TM.Prim(PrimitiveType.Sphere, "Leaf", head, new Vector3(0.07f, 0.49f, 0), new Vector3(0.12f, 0.03f, 0.07f), TM.Hex("#4CA45E"), 0, new Vector3(0, 0, 25));

            // 頸枕：扁圓環套在身體上緣
            TM.MeshObj("NeckPillow", head, TM.Torus(0.5f, 0.1f), new Vector3(0, -0.12f, 0.02f), TM.Hex("#9CCBF0"),
                0, null, new Vector3(1.08f, 1f, 0.78f));
            TM.Prim(PrimitiveType.Cube, "Clip", head, new Vector3(0, -0.12f, 0.44f), new Vector3(0.12f, 0.08f, 0.05f), TM.Hex("#D9DEE5"));

            // 冰咖啡（左手）
            var h = q.slotHandL;
            TM.Prim(PrimitiveType.Cylinder, "Cup", h, new Vector3(0, 0.05f, 0), new Vector3(0.14f, 0.12f, 0.14f), TM.Hex("#8A5A3B"));
            TM.Prim(PrimitiveType.Cylinder, "Lid", h, new Vector3(0, 0.18f, 0), new Vector3(0.16f, 0.015f, 0.16f), Color.white);
            TM.Prim(PrimitiveType.Cylinder, "Straw", h, new Vector3(0.02f, 0.27f, 0), new Vector3(0.02f, 0.1f, 0.02f), TM.Hex("#2F9E62"), 0, new Vector3(0, 0, -12));
        }

        // 移動打卡機：頭上墨鏡、胸前拍立得＋背帶、閃亮眼
        static void CameraHunter(Ququ q)
        {
            q.MakeEyes("sparkle");
            var coral = TM.Hex("#FF6B6B");
            var head = q.slotHead;
            var glasses = TM.Empty("Sunglasses", head, new Vector3(0, -0.02f, 0.22f));
            glasses.localEulerAngles = new Vector3(-25, 0, 0);
            foreach (var x in new[] { -0.15f, 0.15f })
            {
                TM.Prim(PrimitiveType.Cube, "Frame", glasses, new Vector3(x, 0, 0), new Vector3(0.24f, 0.15f, 0.03f), coral);
                TM.Prim(PrimitiveType.Cube, "Lens", glasses, new Vector3(x, 0, 0.018f), new Vector3(0.19f, 0.11f, 0.01f), TM.Hex("#2A2D3E"), 0, null);
            }
            TM.Prim(PrimitiveType.Cube, "Bridge", glasses, new Vector3(0, 0.02f, 0), new Vector3(0.08f, 0.03f, 0.03f), coral);

            var chest = q.slotChest;
            TM.Prim(PrimitiveType.Cube, "StrapL", chest, new Vector3(-0.22f, 0.2f, 0.01f), new Vector3(0.55f, 0.035f, 0.01f), coral, 0, new Vector3(0, 0, -40));
            TM.Prim(PrimitiveType.Cube, "StrapR", chest, new Vector3(0.22f, 0.2f, 0.01f), new Vector3(0.55f, 0.035f, 0.01f), coral, 0, new Vector3(0, 0, 40));
            var cam = TM.Empty("InstantCamera", chest, new Vector3(0, 0.02f, 0.07f));
            TM.Prim(PrimitiveType.Cube, "Box", cam, Vector3.zero, new Vector3(0.34f, 0.24f, 0.13f), TM.Hex("#FFF4F0"));
            TM.Prim(PrimitiveType.Cube, "Stripe", cam, new Vector3(0, 0.06f, 0.066f), new Vector3(0.34f, 0.04f, 0.005f), coral);
            TM.Prim(PrimitiveType.Cylinder, "Lens", cam, new Vector3(0, -0.02f, 0.075f), new Vector3(0.13f, 0.03f, 0.13f), TM.Ink, 0, new Vector3(90, 0, 0));
            TM.Prim(PrimitiveType.Cylinder, "Glass", cam, new Vector3(0, -0.02f, 0.105f), new Vector3(0.07f, 0.005f, 0.07f), TM.Hex("#5B8DEF"), 0.3f, new Vector3(90, 0, 0));
            TM.Prim(PrimitiveType.Cube, "Flash", cam, new Vector3(0.12f, 0.08f, 0.066f), new Vector3(0.05f, 0.03f, 0.01f), TM.Hex("#FFE27A"), 0.5f);

            // 閃亮星星特效
            var star = TM.Empty("Sparkle", head, new Vector3(0.5f, 0.15f, 0.1f));
            TM.Prim(PrimitiveType.Cube, "S1", star, Vector3.zero, new Vector3(0.04f, 0.18f, 0.02f), TM.Hex("#FFD43B"), 0.8f, new Vector3(0, 0, 0));
            TM.Prim(PrimitiveType.Cube, "S2", star, Vector3.zero, new Vector3(0.18f, 0.04f, 0.02f), TM.Hex("#FFD43B"), 0.8f, new Vector3(0, 0, 0));
            star.gameObject.AddComponent<Spin>().speed = new Vector3(0, 0, 90);
        }

        // 尊榮計程車星人：懸浮 TAXI 車頂燈、VIP 徽章、叫車手機、得意眼
        static void Taxi(Ququ q)
        {
            q.MakeEyes("smug");
            var gold = TM.Hex("#F5A800");
            var light = TM.Empty("TaxiLight", q.slotHead, new Vector3(0, 0.42f, 0));
            TM.Prim(PrimitiveType.Cube, "Lamp", light, Vector3.zero, new Vector3(0.46f, 0.17f, 0.22f), TM.Hex("#FFD43B"), 0.7f);
            TM.Prim(PrimitiveType.Cube, "Base", light, new Vector3(0, -0.1f, 0), new Vector3(0.3f, 0.04f, 0.16f), TM.Ink);
            TM.Text3D("TAXI", light, new Vector3(0, 0, 0.115f), 0.12f, TM.Ink);
            TM.Text3D("TAXI", light, new Vector3(0, 0, -0.115f), 0.12f, TM.Ink, new Vector3(0, 180, 0));
            light.gameObject.AddComponent<Bob>();

            var badge = TM.Empty("VIPBadge", q.slotChest, new Vector3(0, 0.02f, 0.02f));
            TM.Prim(PrimitiveType.Cube, "Plate", badge, Vector3.zero, new Vector3(0.22f, 0.17f, 0.03f), gold, 0.15f);
            TM.Text3D("VIP", badge, new Vector3(0, -0.01f, 0.017f), 0.08f, TM.Ink);
            TM.Prim(PrimitiveType.Cube, "CordL", q.slotChest, new Vector3(-0.2f, 0.17f, 0.01f), new Vector3(0.45f, 0.02f, 0.01f), TM.Ink, 0, new Vector3(0, 0, -35));
            TM.Prim(PrimitiveType.Cube, "CordR", q.slotChest, new Vector3(0.2f, 0.17f, 0.01f), new Vector3(0.45f, 0.02f, 0.01f), TM.Ink, 0, new Vector3(0, 0, 35));

            var h = q.slotHandR;
            TM.Prim(PrimitiveType.Cube, "Phone", h, new Vector3(0, 0.06f, 0), new Vector3(0.13f, 0.23f, 0.025f), TM.Ink);
            TM.Prim(PrimitiveType.Cube, "Screen", h, new Vector3(0, 0.06f, 0.014f), new Vector3(0.1f, 0.18f, 0.005f), TM.Hex("#7CC6FE"), 0.6f);
        }

        // 暴走購物狂：愛心眼、保暖毛帽、兩手滿載購物袋
        static void Shopper(Ququ q)
        {
            q.MakeEyes("heart");
            var pink = TM.Hex("#EB2F96");
            var head = q.slotHead;
            TM.MeshObj("Beanie", head, TM.RoundedBox(new Vector3(Ququ.W + 0.04f, 0.26f, Ququ.D + 0.04f), 0.12f, 6), new Vector3(0, 0.02f, 0), pink);
            TM.MeshObj("Cuff", head, TM.RoundedBox(new Vector3(Ququ.W + 0.08f, 0.1f, Ququ.D + 0.08f), 0.05f, 4), new Vector3(0, -0.1f, 0), TM.Hex("#FFD6E8"));
            TM.Prim(PrimitiveType.Sphere, "Pompom", head, new Vector3(0, 0.24f, 0), Vector3.one * 0.2f, Color.white);
            Bag(q.slotHandL, TM.Hex("#FFB547"), "SALE");
            Bag(q.slotHandR, pink, "TAX FREE");
        }

        static void Bag(Transform hand, Color c, string label)
        {
            var b = TM.Empty("Bag", hand, new Vector3(0, -0.12f, 0.02f));
            TM.Prim(PrimitiveType.Cube, "Body", b, Vector3.zero, new Vector3(0.24f, 0.28f, 0.1f), c);
            TM.MeshObj("Handle", b, TM.Torus(0.06f, 0.012f, 16, 6), new Vector3(0, 0.17f, 0), TM.Ink, 0, new Vector3(90, 0, 0));
            TM.Text3D(label, b, new Vector3(0, 0, 0.055f), 0.05f, Color.white);
        }

        // 旅費精算師：豆豆眼＋圓框眼鏡、計算機、折價券票夾
        static void Accountant(Ququ q)
        {
            q.MakeEyes("glasses");
            var teal = TM.Hex("#13C2C2");
            var h = q.slotHandR;
            var calc = TM.Empty("Calculator", h, new Vector3(0, 0.05f, 0));
            TM.Prim(PrimitiveType.Cube, "Body", calc, Vector3.zero, new Vector3(0.18f, 0.26f, 0.04f), TM.Hex("#C9B27A"));
            TM.Prim(PrimitiveType.Cube, "Screen", calc, new Vector3(0, 0.08f, 0.022f), new Vector3(0.14f, 0.06f, 0.005f), TM.Hex("#B9E6C9"), 0.3f);
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    TM.Prim(PrimitiveType.Cube, "Key", calc, new Vector3(-0.05f + c * 0.05f, 0.01f - r * 0.05f, 0.022f), new Vector3(0.035f, 0.035f, 0.01f), r == 2 && c == 2 ? teal : TM.Ink);
            // 折價券票夾（左手）
            var hl = q.slotHandL;
            TM.Prim(PrimitiveType.Cube, "Coupon1", hl, new Vector3(0, 0.03f, 0), new Vector3(0.22f, 0.12f, 0.01f), TM.Hex("#FFF3C4"), 0, new Vector3(0, 0, 8));
            TM.Prim(PrimitiveType.Cube, "Coupon2", hl, new Vector3(0.02f, 0.05f, -0.012f), new Vector3(0.22f, 0.12f, 0.01f), teal, 0, new Vector3(0, 0, -6));
            TM.Prim(PrimitiveType.Cube, "Clip", hl, new Vector3(-0.08f, 0.1f, 0.006f), new Vector3(0.05f, 0.05f, 0.02f), TM.Hex("#D9DEE5"));
            // 胸前的小金幣
            TM.Prim(PrimitiveType.Cylinder, "Coin", q.slotChest, new Vector3(0, 0.05f, 0.02f), new Vector3(0.13f, 0.01f, 0.13f), TM.Hex("#FFC93C"), 0.3f, new Vector3(90, 0, 0));
        }

        // 靈魂探險家：墨鏡、漁夫帽、望遠鏡、私房地圖手冊
        static void Explorer(Ququ q)
        {
            q.MakeEyes("shades");
            var purple = TM.Hex("#722ED1");
            var head = q.slotHead;
            TM.Prim(PrimitiveType.Cylinder, "Brim", head, new Vector3(0, 0.01f, 0), new Vector3(1.25f, 0.02f, 0.95f), TM.Hex("#C9B98F"));
            TM.MeshObj("Crown", head, TM.RoundedBox(new Vector3(0.8f, 0.24f, 0.56f), 0.1f, 6), new Vector3(0, 0.13f, 0), TM.Hex("#D8C9A0"));
            TM.Prim(PrimitiveType.Cube, "Band", head, new Vector3(0, 0.05f, 0), new Vector3(0.82f, 0.05f, 0.58f), purple);
            // 望遠鏡（胸前）
            var bin = TM.Empty("Binoculars", q.slotChest, new Vector3(0, 0.03f, 0.06f));
            foreach (var x in new[] { -0.07f, 0.07f })
            {
                TM.Prim(PrimitiveType.Cylinder, "Tube", bin, new Vector3(x, 0, 0), new Vector3(0.1f, 0.08f, 0.1f), TM.Ink, 0, new Vector3(90, 0, 0));
                TM.Prim(PrimitiveType.Cylinder, "Glass", bin, new Vector3(x, 0, 0.081f), new Vector3(0.07f, 0.005f, 0.07f), TM.Hex("#7CC6FE"), 0.3f, new Vector3(90, 0, 0));
            }
            TM.Prim(PrimitiveType.Cube, "StrapL", q.slotChest, new Vector3(-0.2f, 0.17f, 0.01f), new Vector3(0.45f, 0.02f, 0.01f), purple, 0, new Vector3(0, 0, -35));
            TM.Prim(PrimitiveType.Cube, "StrapR", q.slotChest, new Vector3(0.2f, 0.17f, 0.01f), new Vector3(0.45f, 0.02f, 0.01f), purple, 0, new Vector3(0, 0, 35));
            // 地圖手冊（左手）
            var h = q.slotHandL;
            TM.Prim(PrimitiveType.Cube, "MapL", h, new Vector3(-0.06f, 0.05f, 0), new Vector3(0.13f, 0.2f, 0.01f), TM.Hex("#FFF8EE"), 0, new Vector3(0, 20, 0));
            TM.Prim(PrimitiveType.Cube, "MapR", h, new Vector3(0.06f, 0.05f, 0), new Vector3(0.13f, 0.2f, 0.01f), TM.Hex("#FFF8EE"), 0, new Vector3(0, -20, 0));
            TM.Prim(PrimitiveType.Cube, "Route", h, new Vector3(0.06f, 0.05f, 0.01f), new Vector3(0.08f, 0.015f, 0.005f), purple, 0, new Vector3(0, -20, 30));
        }

        // 全能旅行保母：彎彎笑眼、大背包、救急醫療包、和平徽章
        static void Nanny(Ququ q)
        {
            q.MakeEyes("smiling");
            var yellow = TM.Hex("#FADB14");
            // 大背包（背後）
            var back = q.slotBack;
            TM.MeshObj("Pack", back, TM.RoundedBox(new Vector3(0.78f, 0.82f, 0.3f), 0.12f, 6), new Vector3(0, 0.05f, -0.16f), TM.Hex("#1F7A55"));
            TM.MeshObj("Pocket", back, TM.RoundedBox(new Vector3(0.5f, 0.3f, 0.12f), 0.06f, 4), new Vector3(0, -0.15f, -0.33f), TM.Hex("#2FA073"));
            TM.Prim(PrimitiveType.Cube, "StrapL", q.slotChest, new Vector3(-0.3f, 0.15f, 0.01f), new Vector3(0.07f, 0.6f, 0.02f), TM.Hex("#1F7A55"));
            TM.Prim(PrimitiveType.Cube, "StrapR", q.slotChest, new Vector3(0.3f, 0.15f, 0.01f), new Vector3(0.07f, 0.6f, 0.02f), TM.Hex("#1F7A55"));
            // 和平徽章
            var pin = TM.Empty("PeacePin", q.slotChest, new Vector3(0, 0.06f, 0.02f));
            TM.Prim(PrimitiveType.Cylinder, "Disc", pin, Vector3.zero, new Vector3(0.16f, 0.01f, 0.16f), yellow, 0.2f, new Vector3(90, 0, 0));
            TM.Prim(PrimitiveType.Cube, "V", pin, new Vector3(0, 0, 0.008f), new Vector3(0.016f, 0.12f, 0.005f), TM.Ink);
            TM.Prim(PrimitiveType.Cube, "L", pin, new Vector3(-0.025f, -0.025f, 0.008f), new Vector3(0.016f, 0.07f, 0.005f), TM.Ink, 0, new Vector3(0, 0, -40));
            TM.Prim(PrimitiveType.Cube, "R", pin, new Vector3(0.025f, -0.025f, 0.008f), new Vector3(0.016f, 0.07f, 0.005f), TM.Ink, 0, new Vector3(0, 0, 40));
            // 救急醫療包（右手）
            var h = q.slotHandR;
            TM.Prim(PrimitiveType.Cube, "Kit", h, new Vector3(0, -0.05f, 0), new Vector3(0.24f, 0.18f, 0.1f), Color.white);
            TM.Prim(PrimitiveType.Cube, "CrossV", h, new Vector3(0, -0.05f, 0.052f), new Vector3(0.04f, 0.12f, 0.005f), TM.Hex("#E2463B"));
            TM.Prim(PrimitiveType.Cube, "CrossH", h, new Vector3(0, -0.05f, 0.052f), new Vector3(0.12f, 0.04f, 0.005f), TM.Hex("#E2463B"));
            TM.Prim(PrimitiveType.Cube, "Handle", h, new Vector3(0, 0.05f, 0), new Vector3(0.1f, 0.03f, 0.03f), TM.Ink);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace TripMate
{
    public enum Persona
    {
        Base,       // 基本趣趣（還沒做測驗、或嚮導趣趣）
        Soldier,    // 行程特種兵
        Capybara,   // 隨性慢活水豚
        Camera,     // 移動打卡機
        Taxi,       // 尊榮計程車星人
        Shopper,    // 暴走購物狂
        Accountant, // 旅費精算師
        Explorer,   // 靈魂探險家
        Nanny,      // 全能旅行保母
    }

    /// <summary>
    /// 趣趣角色：身體、提把、手、輪子、臉，以及 6 個換裝插槽。
    /// 用 Ququ.Create() 產生，再用 SetPersona() 換裝。
    /// </summary>
    public class Ququ : MonoBehaviour
    {
        public static readonly string[] PersonaNames =
        {
            "趣趣", "行程特種兵", "隨性慢活水豚", "移動打卡機", "尊榮計程車星人",
            "暴走購物狂", "旅費精算師", "靈魂探險家", "全能旅行保母"
        };

        public Persona persona = Persona.Base;

        [HideInInspector] public Transform visual;
        [HideInInspector] public Transform slotHead, slotFace, slotChest, slotHandL, slotHandR, slotBack;
        [HideInInspector] public List<Transform> wheels = new List<Transform>();
        [HideInInspector] public Transform armL, armR;
        [HideInInspector] public GameObject eyes;

        // 身體尺寸（公尺），角色全高約 1.25
        public const float W = 1.0f, H = 0.92f, D = 0.62f, WheelH = 0.12f;
        public static float Front => D * 0.5f;
        public static float BodyCenterY => WheelH + H * 0.5f;
        public static float BodyTopY => WheelH + H;

        public static Ququ Create(string name, Transform parent, Vector3 pos, Persona p)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            var q = root.AddComponent<Ququ>();
            q.BuildBody();
            q.SetPersona(p);
            return q;
        }

        void BuildBody()
        {
            visual = TM.Empty("Visual", transform, Vector3.zero);

            // 身體
            TM.MeshObj("Body", visual, TM.RoundedBox(new Vector3(W, H, D), 0.2f), new Vector3(0, BodyCenterY, 0), TM.Body);
            // 拉鍊縫線
            TM.Prim(PrimitiveType.Cube, "Seam", visual, new Vector3(0, WheelH + 0.12f, Front + 0.003f), new Vector3(0.78f, 0.022f, 0.01f), TM.Seam);
            // 側邊深藍細線（行李箱分模線）
            TM.Prim(PrimitiveType.Cube, "SideLineL", visual, new Vector3(-W * 0.5f - 0.002f, BodyCenterY, 0), new Vector3(0.01f, H - 0.3f, 0.03f), TM.Ink);
            TM.Prim(PrimitiveType.Cube, "SideLineR", visual, new Vector3(W * 0.5f + 0.002f, BodyCenterY, 0), new Vector3(0.01f, H - 0.3f, 0.03f), TM.Ink);

            // 提把
            float hy = BodyTopY;
            TM.Prim(PrimitiveType.Cube, "HandlePostL", visual, new Vector3(-0.2f, hy + 0.08f, 0), new Vector3(0.08f, 0.2f, 0.09f), TM.Ink);
            TM.Prim(PrimitiveType.Cube, "HandlePostR", visual, new Vector3(0.2f, hy + 0.08f, 0), new Vector3(0.08f, 0.2f, 0.09f), TM.Ink);
            TM.Prim(PrimitiveType.Cube, "HandleBar", visual, new Vector3(0, hy + 0.18f, 0), new Vector3(0.48f, 0.08f, 0.09f), TM.Ink);

            // 輪子（四顆）
            foreach (var x in new[] { -0.32f, 0.32f })
            foreach (var z in new[] { -0.18f, 0.18f })
            {
                var pivot = TM.Empty("WheelPivot", visual, new Vector3(x, 0.075f, z));
                TM.Prim(PrimitiveType.Cylinder, "Wheel", pivot, Vector3.zero, new Vector3(0.15f, 0.035f, 0.15f), TM.Ink, 0, new Vector3(0, 0, 90));
                TM.Prim(PrimitiveType.Cylinder, "Hub", pivot, Vector3.zero, new Vector3(0.07f, 0.04f, 0.07f), TM.Page, 0, new Vector3(0, 0, 90));
                wheels.Add(pivot);
            }

            // 手
            armL = TM.Empty("ArmL", visual, new Vector3(-W * 0.5f - 0.04f, WheelH + 0.46f, 0.04f));
            armR = TM.Empty("ArmR", visual, new Vector3(W * 0.5f + 0.04f, WheelH + 0.46f, 0.04f));
            TM.Prim(PrimitiveType.Sphere, "Hand", armL, new Vector3(-0.04f, -0.06f, 0), new Vector3(0.2f, 0.26f, 0.2f), TM.Body);
            TM.Prim(PrimitiveType.Sphere, "Hand", armR, new Vector3(0.04f, -0.06f, 0), new Vector3(0.2f, 0.26f, 0.2f), TM.Body);

            // 臉：腮紅、嘴巴（眼睛依人格另外做）
            float fz = Front + 0.004f;
            TM.Prim(PrimitiveType.Sphere, "BlushL", visual, new Vector3(-0.3f, WheelH + 0.5f, fz), new Vector3(0.13f, 0.07f, 0.02f), TM.Blush);
            TM.Prim(PrimitiveType.Sphere, "BlushR", visual, new Vector3(0.3f, WheelH + 0.5f, fz), new Vector3(0.13f, 0.07f, 0.02f), TM.Blush);
            TM.Prim(PrimitiveType.Sphere, "Mouth", visual, new Vector3(0, WheelH + 0.47f, fz), new Vector3(0.12f, 0.075f, 0.025f), TM.Ink);
            TM.Prim(PrimitiveType.Sphere, "Tongue", visual, new Vector3(0, WheelH + 0.455f, fz + 0.006f), new Vector3(0.06f, 0.03f, 0.02f), TM.Blush);

            // 插槽
            slotHead = TM.Empty("Slot_Head", visual, new Vector3(0, BodyTopY, 0));
            slotFace = TM.Empty("Slot_Face", visual, new Vector3(0, WheelH + 0.62f, Front));
            slotChest = TM.Empty("Slot_Chest", visual, new Vector3(0, WheelH + 0.27f, Front));
            slotHandL = TM.Empty("Slot_HandL", armL, new Vector3(-0.08f, -0.1f, 0.12f));
            slotHandR = TM.Empty("Slot_HandR", armR, new Vector3(0.08f, -0.1f, 0.12f));
            slotBack = TM.Empty("Slot_Back", visual, new Vector3(0, BodyCenterY, -Front));
        }

        void ClearSlots()
        {
            foreach (var s in new[] { slotHead, slotFace, slotChest, slotHandL, slotHandR, slotBack })
                for (int i = s.childCount - 1; i >= 0; i--) TM.Kill(s.GetChild(i).gameObject);
            if (eyes != null) TM.Kill(eyes);
        }

        /// <summary>換裝：清空插槽，換上該人格的眼睛與配件。</summary>
        public void SetPersona(Persona p)
        {
            persona = p;
            ClearSlots();
            QuquOutfits.Apply(this, p);
        }

        public void NextPersona()
        {
            int n = System.Enum.GetValues(typeof(Persona)).Length;
            SetPersona((Persona)(((int)persona + 1) % n));
        }

        // ---------- 眼睛 ----------
        public void MakeEyes(string style)
        {
            eyes = new GameObject("Eyes");
            eyes.transform.SetParent(slotFace, false);
            var e = eyes.transform;
            float z = 0.006f;
            foreach (var x in new[] { -0.17f, 0.17f })
            {
                switch (style)
                {
                    case "sleepy": // 瞇瞇眼
                        TM.Prim(PrimitiveType.Cube, "Eye", e, new Vector3(x, -0.02f, z), new Vector3(0.15f, 0.028f, 0.02f), TM.Ink);
                        break;
                    case "smug": // 得意半瞇眼
                        TM.Prim(PrimitiveType.Sphere, "Eye", e, new Vector3(x, -0.02f, z), new Vector3(0.12f, 0.08f, 0.03f), TM.Ink);
                        TM.Prim(PrimitiveType.Cube, "Lid", e, new Vector3(x, 0.025f, z + 0.01f), new Vector3(0.16f, 0.035f, 0.02f), TM.Ink, 0, new Vector3(0, 0, x < 0 ? -8 : 8));
                        break;
                    case "focused": // 專注眼＋用力的眉毛
                        TM.Prim(PrimitiveType.Sphere, "Eye", e, new Vector3(x, -0.02f, z), new Vector3(0.11f, 0.13f, 0.03f), TM.Ink);
                        TM.Prim(PrimitiveType.Sphere, "Shine", e, new Vector3(x + 0.025f, 0.01f, z + 0.015f), new Vector3(0.035f, 0.035f, 0.01f), Color.white);
                        TM.Prim(PrimitiveType.Cube, "Brow", e, new Vector3(x, 0.085f, z + 0.005f), new Vector3(0.15f, 0.035f, 0.02f), TM.Ink, 0, new Vector3(0, 0, x < 0 ? -18 : 18));
                        break;
                    case "sparkle": // 閃亮眼（大亮點）
                        TM.Prim(PrimitiveType.Sphere, "Eye", e, new Vector3(x, 0f, z), new Vector3(0.13f, 0.17f, 0.03f), TM.Ink);
                        TM.Prim(PrimitiveType.Cube, "StarV", e, new Vector3(x, 0.01f, z + 0.016f), new Vector3(0.025f, 0.09f, 0.01f), TM.Hex("#FFE27A"), 0.6f, new Vector3(0, 0, 0));
                        TM.Prim(PrimitiveType.Cube, "StarH", e, new Vector3(x, 0.01f, z + 0.016f), new Vector3(0.07f, 0.025f, 0.01f), TM.Hex("#FFE27A"), 0.6f);
                        break;
                    case "heart": // 愛心眼（購物狂）
                        TM.Prim(PrimitiveType.Sphere, "HeartL", e, new Vector3(x - 0.03f, 0.02f, z), new Vector3(0.08f, 0.08f, 0.03f), TM.Hex("#EB2F96"), 0.2f);
                        TM.Prim(PrimitiveType.Sphere, "HeartR", e, new Vector3(x + 0.03f, 0.02f, z), new Vector3(0.08f, 0.08f, 0.03f), TM.Hex("#EB2F96"), 0.2f);
                        TM.Prim(PrimitiveType.Cube, "HeartTip", e, new Vector3(x, -0.025f, z), new Vector3(0.075f, 0.075f, 0.028f), TM.Hex("#EB2F96"), 0.2f, new Vector3(0, 0, 45));
                        break;
                    case "glasses": // 豆豆眼＋圓框眼鏡（精算師）
                        TM.Prim(PrimitiveType.Sphere, "Eye", e, new Vector3(x, -0.01f, z), new Vector3(0.06f, 0.07f, 0.03f), TM.Ink);
                        TM.MeshObj("Rim", e, TM.Torus(0.085f, 0.012f, 20, 6), new Vector3(x, -0.01f, z + 0.02f), TM.Hex("#13C2C2"), 0, new Vector3(90, 0, 0));
                        if (x > 0) TM.Prim(PrimitiveType.Cube, "Bridge", e, new Vector3(0, 0.0f, z + 0.02f), new Vector3(0.17f, 0.018f, 0.015f), TM.Hex("#13C2C2"));
                        break;
                    case "shades": // 墨鏡（探險家）
                        TM.Prim(PrimitiveType.Cube, "Lens", e, new Vector3(x, 0f, z + 0.01f), new Vector3(0.22f, 0.12f, 0.03f), TM.Hex("#2A2D3E"));
                        TM.Prim(PrimitiveType.Cube, "Glint", e, new Vector3(x - 0.05f, 0.03f, z + 0.03f), new Vector3(0.05f, 0.02f, 0.005f), Color.white, 0.3f, new Vector3(0, 0, 20));
                        if (x > 0) TM.Prim(PrimitiveType.Cube, "Bridge", e, new Vector3(0, 0.02f, z + 0.01f), new Vector3(0.14f, 0.025f, 0.02f), TM.Hex("#2A2D3E"));
                        break;
                    case "smiling": // 彎彎笑眼（保母）
                        TM.MeshObj("Arc", e, TM.Torus(0.06f, 0.016f, 16, 6), new Vector3(x, -0.03f, z), TM.Ink, 0, new Vector3(90, 0, 0), new Vector3(1, 1, 1));
                        TM.Prim(PrimitiveType.Cube, "Cut", e, new Vector3(x, -0.075f, z + 0.012f), new Vector3(0.16f, 0.09f, 0.03f), TM.Body);
                        break;
                    default: // 圓眼
                        TM.Prim(PrimitiveType.Sphere, "Eye", e, new Vector3(x, 0f, z), new Vector3(0.11f, 0.15f, 0.03f), TM.Ink);
                        TM.Prim(PrimitiveType.Sphere, "Shine", e, new Vector3(x + 0.025f, 0.035f, z + 0.015f), new Vector3(0.035f, 0.035f, 0.01f), Color.white);
                        break;
                }
            }
        }
    }
}

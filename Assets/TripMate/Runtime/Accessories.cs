using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TripMate
{
    public class AccDef
    {
        public string id, name, slot, desc;
        public int price;
        public Color color;
    }

    /// <summary>
    /// 印章商店：旅途中撿到的印章會存起來（跨場次保留），在主選單的「趣趣商店」換趣趣的配件。
    /// 另外每站藏一枚紫色的「隱藏印章」，撿到就解鎖一個隱藏景點，排行程時可以放進去。
    /// 存檔用 PlayerPrefs（只存在這台電腦）。
    /// </summary>
    public static class Accessories
    {
        // 插槽：head 頭上、face 臉上、chest 胸前／脖子、back 背後、hand 右手、pet 跟寵
        public static readonly string[] SlotOrder = { "head", "face", "chest", "back", "hand", "pet" };
        public static readonly Dictionary<string, string> SlotName = new Dictionary<string, string>
        {
            { "head", "頭上" }, { "face", "臉上" }, { "chest", "脖子" }, { "back", "背後" }, { "hand", "手上" }, { "pet", "跟寵" },
        };

        public static readonly AccDef[] All =
        {
            new AccDef { id = "beanie",  slot = "head",  price = 3,  name = "紅色毛帽",   desc = "冬天必備，還有毛球", color = TM.Hex("#D8343A") },
            new AccDef { id = "bunny",   slot = "head",  price = 5,  name = "兔耳髮箍",   desc = "樂園限定的那種",     color = TM.Hex("#FFFFFF") },
            new AccDef { id = "kitsune", slot = "head",  price = 8,  name = "狐狸面具",   desc = "祭典風，斜戴在頭上", color = TM.Hex("#F4F1EC") },
            new AccDef { id = "crown",   slot = "head",  price = 14, name = "小皇冠",     desc = "印章大富翁專屬",     color = TM.Hex("#FFC93C") },
            new AccDef { id = "round",   slot = "face",  price = 4,  name = "圓框眼鏡",   desc = "看起來很會查資料",   color = TM.Hex("#8A5A3C") },
            new AccDef { id = "heart",   slot = "face",  price = 6,  name = "愛心墨鏡",   desc = "走到哪都在拍照",     color = TM.Hex("#FF5A8A") },
            new AccDef { id = "plaid",   slot = "chest", price = 3,  name = "格紋圍巾",   desc = "暖呼呼的紅格紋",     color = TM.Hex("#C8323A") },
            new AccDef { id = "camera",  slot = "chest", price = 6,  name = "拍立得",     desc = "掛在胸前隨時拍",     color = TM.Hex("#2A2D3E") },
            new AccDef { id = "pack",    slot = "back",  price = 4,  name = "小背包",     desc = "裝得下所有戰利品",   color = TM.Hex("#3F8F6A") },
            new AccDef { id = "wings",   slot = "back",  price = 10, name = "天使翅膀",   desc = "飛去下一站",         color = TM.Hex("#FFFFFF") },
            new AccDef { id = "umbrella",slot = "hand",  price = 5,  name = "紅雨傘",     desc = "道頓堀下雪也不怕",   color = TM.Hex("#D8343A") },
            new AccDef { id = "balloon", slot = "hand",  price = 4,  name = "趣趣氣球",   desc = "飄在頭上好顯眼",     color = TM.Hex("#FFB547") },
            new AccDef { id = "cat",     slot = "pet",   price = 10, name = "招財貓",     desc = "會跟著你走",         color = TM.Hex("#FFFFFF") },
            new AccDef { id = "deer",    slot = "pet",   price = 12, name = "奈良小鹿",   desc = "會跟著你走",         color = TM.Hex("#B07A4A") },
        };

        public static AccDef Get(string id) { return All.FirstOrDefault(a => a.id == id); }

        // ---------------- 存檔 ----------------
        const string KWallet = "tm_stamp_wallet", KOwned = "tm_owned", KEquip = "tm_equip_", KHidden = "tm_hidden";

        public static int Wallet
        {
            get { return PlayerPrefs.GetInt(KWallet, 0); }
            set { PlayerPrefs.SetInt(KWallet, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        static List<string> List(string key)
        {
            var s = PlayerPrefs.GetString(key, "");
            return string.IsNullOrEmpty(s) ? new List<string>() : s.Split(',').ToList();
        }

        static void SaveList(string key, List<string> l) { PlayerPrefs.SetString(key, string.Join(",", l.ToArray())); PlayerPrefs.Save(); }

        public static bool Owned(string id) { return List(KOwned).Contains(id); }

        public static bool Buy(string id)
        {
            var a = Get(id);
            if (a == null || Owned(id) || Wallet < a.price) return false;
            Wallet -= a.price;
            var l = List(KOwned); l.Add(id); SaveList(KOwned, l);
            return true;
        }

        public static string Equipped(string slot) { return PlayerPrefs.GetString(KEquip + slot, ""); }

        public static void Equip(string id)
        {
            var a = Get(id);
            if (a == null || !Owned(id)) return;
            PlayerPrefs.SetString(KEquip + a.slot, id);
            PlayerPrefs.Save();
        }

        public static void Unequip(string slot) { PlayerPrefs.SetString(KEquip + slot, ""); PlayerPrefs.Save(); }

        public static List<string> HiddenFound() { return List(KHidden); }

        /// <summary>解鎖隱藏景點；第一次撿到回傳 true。</summary>
        public static bool UnlockHidden(string poiId)
        {
            var l = List(KHidden);
            if (l.Contains(poiId)) return false;
            l.Add(poiId);
            SaveList(KHidden, l);
            return true;
        }

        // ---------------- 穿戴到趣趣身上 ----------------
        static GameObject pet;

        /// <summary>依存檔把配件掛到玩家的趣趣身上（被配件蓋住的人格裝飾會先藏起來）。</summary>
        public static void Apply(Ququ q)
        {
            if (q == null) return;
            foreach (var slot in SlotOrder)
            {
                if (slot == "pet") continue;
                var c = Container(q, slot);
                for (int i = c.childCount - 1; i >= 0; i--) TM.Kill(c.GetChild(i).gameObject);
                string id = Equipped(slot);
                var personaSlot = PersonaSlot(q, slot);
                if (personaSlot != null) personaSlot.gameObject.SetActive(string.IsNullOrEmpty(id));
                if (!string.IsNullOrEmpty(id)) Build(id, c);
            }
            if (pet != null) TM.Kill(pet);
            string pid = Equipped("pet");
            if (!string.IsNullOrEmpty(pid)) pet = BuildPet(pid, q.transform);
        }

        static Transform PersonaSlot(Ququ q, string slot)
        {
            switch (slot)
            {
                case "head": return q.slotHead;
                case "chest": return q.slotChest;
                case "back": return q.slotBack;
                case "hand": return q.slotHandR;
                default: return null; // 臉上的不藏（眼睛在那裡）
            }
        }

        static Transform Container(Ququ q, string slot)
        {
            Transform parent = slot == "hand" ? q.armR : q.visual;
            var name = "Acc_" + slot;
            var t = parent.Find(name);
            if (t != null) return t;
            Vector3 pos;
            switch (slot)
            {
                case "head": pos = new Vector3(0, Ququ.BodyTopY, 0); break;
                case "face": pos = new Vector3(0, Ququ.WheelH + 0.62f, Ququ.Front); break;
                case "chest": pos = new Vector3(0, Ququ.WheelH + 0.27f, Ququ.Front); break;
                case "back": pos = new Vector3(0, Ququ.BodyCenterY, -Ququ.Front); break;
                default: pos = new Vector3(0.08f, -0.1f, 0.12f); break;
            }
            return TM.Empty(name, parent, pos);
        }

        /// <summary>做出配件的造型（商店預覽也用這個）。</summary>
        public static void Build(string id, Transform c)
        {
            switch (id)
            {
                case "beanie":
                {
                    var red = TM.Hex("#D8343A");
                    TM.Prim(PrimitiveType.Sphere, "Cap", c, new Vector3(0, 0.03f, 0), new Vector3(0.92f, 0.46f, 0.7f), red);
                    TM.MeshObj("Cuff", c, TM.RoundedBox(new Vector3(1.04f, 0.14f, 0.72f), 0.06f, 4), new Vector3(0, 0.02f, 0), red * 0.85f);
                    TM.Prim(PrimitiveType.Sphere, "Pom", c, new Vector3(0, 0.3f, 0), Vector3.one * 0.24f, TM.Hex("#F4F1EC"));
                    break;
                }
                case "bunny":
                    TM.MeshObj("Band", c, TM.RoundedBox(new Vector3(0.9f, 0.06f, 0.1f), 0.03f, 3), new Vector3(0, 0.03f, 0), TM.Hex("#FF8AB0"));
                    foreach (var x in new[] { -0.2f, 0.2f })
                    {
                        TM.Prim(PrimitiveType.Capsule, "Ear", c, new Vector3(x, 0.36f, 0), new Vector3(0.15f, 0.3f, 0.08f), Color.white, 0, new Vector3(0, 0, x * -30));
                        TM.Prim(PrimitiveType.Capsule, "Inner", c, new Vector3(x, 0.36f, 0.035f), new Vector3(0.08f, 0.22f, 0.02f), TM.Hex("#FFB6C8"), 0, new Vector3(0, 0, x * -30));
                    }
                    break;
                case "kitsune":
                {
                    var m = TM.Empty("Mask", c, new Vector3(0.42f, 0.08f, 0.12f));
                    m.localEulerAngles = new Vector3(0, 55, -15);
                    TM.Prim(PrimitiveType.Sphere, "Face", m, Vector3.zero, new Vector3(0.36f, 0.32f, 0.14f), TM.Hex("#F7F2EA"));
                    foreach (var x in new[] { -0.1f, 0.1f })
                    {
                        TM.MeshObj("Ear", m, TM.Cone(0.06f, 0.16f, 4), new Vector3(x, 0.13f, 0), TM.Hex("#F7F2EA"));
                        TM.Prim(PrimitiveType.Cube, "Eye", m, new Vector3(x * 0.8f, 0.02f, 0.07f), new Vector3(0.08f, 0.02f, 0.01f), TM.Hex("#D8343A"), 0, new Vector3(0, 0, x > 0 ? 20 : -20));
                    }
                    TM.Prim(PrimitiveType.Sphere, "Nose", m, new Vector3(0, -0.04f, 0.075f), Vector3.one * 0.04f, TM.Ink);
                    break;
                }
                case "crown":
                {
                    var gold = TM.Hex("#FFC93C");
                    TM.Prim(PrimitiveType.Cylinder, "Ring", c, new Vector3(0, 0.3f, 0), new Vector3(0.42f, 0.06f, 0.42f), gold, 0.4f);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * 72f * Mathf.Deg2Rad;
                        TM.MeshObj("Spike", c, TM.Cone(0.06f, 0.18f, 6), new Vector3(Mathf.Cos(a) * 0.19f, 0.34f, Mathf.Sin(a) * 0.19f), gold, 0.4f);
                        TM.Prim(PrimitiveType.Sphere, "Gem", c, new Vector3(Mathf.Cos(a) * 0.21f, 0.3f, Mathf.Sin(a) * 0.21f), Vector3.one * 0.05f, i % 2 == 0 ? TM.Hex("#E2463B") : TM.Hex("#3E8EDE"), 0.6f);
                    }
                    break;
                }
                case "round":
                    foreach (var x in new[] { -0.17f, 0.17f })
                        TM.MeshObj("Rim", c, TM.Torus(0.1f, 0.016f, 20, 6), new Vector3(x, 0, 0.03f), TM.Hex("#8A5A3C"), 0, new Vector3(90, 0, 0));
                    TM.Prim(PrimitiveType.Cube, "Bridge", c, new Vector3(0, 0.01f, 0.03f), new Vector3(0.14f, 0.02f, 0.015f), TM.Hex("#8A5A3C"));
                    break;
                case "heart":
                    foreach (var x in new[] { -0.17f, 0.17f })
                    {
                        var pink = TM.Hex("#FF5A8A");
                        TM.Prim(PrimitiveType.Sphere, "L", c, new Vector3(x - 0.04f, 0.02f, 0.03f), new Vector3(0.11f, 0.11f, 0.03f), pink, 0.2f);
                        TM.Prim(PrimitiveType.Sphere, "R", c, new Vector3(x + 0.04f, 0.02f, 0.03f), new Vector3(0.11f, 0.11f, 0.03f), pink, 0.2f);
                        TM.Prim(PrimitiveType.Cube, "Tip", c, new Vector3(x, -0.035f, 0.03f), new Vector3(0.1f, 0.1f, 0.028f), pink, 0.2f, new Vector3(0, 0, 45));
                        TM.Prim(PrimitiveType.Cube, "Glint", c, new Vector3(x - 0.04f, 0.04f, 0.05f), new Vector3(0.04f, 0.015f, 0.005f), Color.white, 0.4f, new Vector3(0, 0, 25));
                    }
                    TM.Prim(PrimitiveType.Cube, "Bridge", c, new Vector3(0, 0.03f, 0.03f), new Vector3(0.1f, 0.02f, 0.015f), TM.Hex("#FF5A8A"));
                    break;
                case "plaid":
                {
                    Color red = TM.Hex("#C8323A"), dark = TM.Hex("#7A1E24");
                    float y = 0.5f, w = Ququ.W + 0.04f, d = Ququ.D + 0.04f;
                    TM.Prim(PrimitiveType.Cube, "Front", c, new Vector3(0, y, 0.015f), new Vector3(w, 0.16f, 0.03f), red);
                    TM.Prim(PrimitiveType.Cube, "Back", c, new Vector3(0, y, -d + 0.025f), new Vector3(w, 0.16f, 0.03f), red);
                    TM.Prim(PrimitiveType.Cube, "L", c, new Vector3(-w * 0.5f, y, -d * 0.5f + 0.02f), new Vector3(0.03f, 0.16f, d), red);
                    TM.Prim(PrimitiveType.Cube, "R", c, new Vector3(w * 0.5f, y, -d * 0.5f + 0.02f), new Vector3(0.03f, 0.16f, d), red);
                    for (int i = 0; i < 6; i++)
                        TM.Prim(PrimitiveType.Cube, "Check", c, new Vector3(-0.45f + i * 0.18f, y, 0.032f), new Vector3(0.03f, 0.16f, 0.005f), dark);
                    TM.Prim(PrimitiveType.Cube, "Line", c, new Vector3(0, y, 0.033f), new Vector3(w, 0.025f, 0.005f), TM.Hex("#FFC93C"));
                    TM.Prim(PrimitiveType.Cube, "Tail", c, new Vector3(0.28f, 0.28f, 0.04f), new Vector3(0.16f, 0.36f, 0.03f), red, 0, new Vector3(0, 0, 6));
                    break;
                }
                case "camera":
                    TM.MeshObj("Body", c, TM.RoundedBox(new Vector3(0.36f, 0.24f, 0.12f), 0.04f, 3), new Vector3(0, 0.14f, 0.07f), TM.Hex("#F4F1EC"));
                    TM.Prim(PrimitiveType.Cylinder, "Lens", c, new Vector3(0, 0.13f, 0.14f), new Vector3(0.14f, 0.03f, 0.14f), TM.Hex("#2A2D3E"), 0, new Vector3(90, 0, 0));
                    TM.Prim(PrimitiveType.Cylinder, "Glass", c, new Vector3(0, 0.13f, 0.17f), new Vector3(0.09f, 0.005f, 0.09f), TM.Hex("#7FB5FF"), 0.4f, new Vector3(90, 0, 0));
                    TM.Prim(PrimitiveType.Cube, "Stripe", c, new Vector3(0, 0.23f, 0.135f), new Vector3(0.3f, 0.03f, 0.005f), TM.Hex("#E2463B"));
                    TM.Prim(PrimitiveType.Cube, "StrapL", c, new Vector3(-0.22f, 0.4f, 0.02f), new Vector3(0.03f, 0.42f, 0.02f), TM.Ink, 0, new Vector3(0, 0, -25));
                    TM.Prim(PrimitiveType.Cube, "StrapR", c, new Vector3(0.22f, 0.4f, 0.02f), new Vector3(0.03f, 0.42f, 0.02f), TM.Ink, 0, new Vector3(0, 0, 25));
                    break;
                case "pack":
                    TM.MeshObj("Bag", c, TM.RoundedBox(new Vector3(0.62f, 0.6f, 0.26f), 0.1f, 4), new Vector3(0, 0, -0.13f), TM.Hex("#3F8F6A"));
                    TM.MeshObj("Pocket", c, TM.RoundedBox(new Vector3(0.4f, 0.24f, 0.08f), 0.05f, 3), new Vector3(0, -0.12f, -0.28f), TM.Hex("#2F7454"));
                    TM.Prim(PrimitiveType.Cube, "Flap", c, new Vector3(0, 0.22f, -0.27f), new Vector3(0.5f, 0.14f, 0.04f), TM.Hex("#2F7454"));
                    break;
                case "wings":
                    foreach (var x in new[] { -0.32f, 0.32f })
                    {
                        TM.Prim(PrimitiveType.Sphere, "Wing", c, new Vector3(x, 0.15f, -0.06f), new Vector3(0.48f, 0.72f, 0.08f), Color.white, 0.15f, new Vector3(0, 0, x > 0 ? -28 : 28));
                        TM.Prim(PrimitiveType.Sphere, "Feather", c, new Vector3(x * 1.25f, -0.05f, -0.07f), new Vector3(0.3f, 0.44f, 0.07f), TM.Hex("#F3F0FF"), 0.15f, new Vector3(0, 0, x > 0 ? -45 : 45));
                    }
                    c.gameObject.AddComponent<Bob>().amp = 0.03f;
                    break;
                case "umbrella":
                {
                    TM.Prim(PrimitiveType.Cylinder, "Shaft", c, new Vector3(0, 0.55f, 0), new Vector3(0.03f, 0.55f, 0.03f), TM.Hex("#3A2A20"));
                    var top = TM.Empty("Canopy", c, new Vector3(-0.25f, 1.12f, -0.05f));
                    top.localEulerAngles = new Vector3(0, 0, 12);
                    TM.MeshObj("Cloth", top, TM.Cone(0.8f, 0.34f, 10), Vector3.zero, TM.Hex("#D8343A"));
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * 72f * Mathf.Deg2Rad;
                        TM.Prim(PrimitiveType.Sphere, "SnowDot", top, new Vector3(Mathf.Cos(a) * 0.42f, 0.17f, Mathf.Sin(a) * 0.42f), Vector3.one * 0.06f, Color.white);
                    }
                    break;
                }
                case "balloon":
                {
                    TM.Prim(PrimitiveType.Cylinder, "String", c, new Vector3(0, 0.65f, 0), new Vector3(0.008f, 0.65f, 0.008f), TM.Ink);
                    var b = TM.Empty("Balloon", c, new Vector3(0, 1.45f, 0));
                    TM.MeshObj("Ball", b, TM.RoundedBox(new Vector3(0.5f, 0.46f, 0.32f), 0.15f, 5), Vector3.zero, TM.Hex("#FFB547"), 0.15f);
                    foreach (var x in new[] { -0.08f, 0.08f })
                        TM.Prim(PrimitiveType.Sphere, "Eye", b, new Vector3(x, 0.03f, 0.165f), new Vector3(0.05f, 0.07f, 0.02f), TM.Ink);
                    TM.Prim(PrimitiveType.Sphere, "Mouth", b, new Vector3(0, -0.06f, 0.165f), new Vector3(0.06f, 0.035f, 0.02f), TM.Ink);
                    b.gameObject.AddComponent<Bob>().amp = 0.08f;
                    break;
                }
                case "cat":
                case "deer":
                    BuildPetModel(id, c);
                    break;
            }
        }

        static void BuildPetModel(string id, Transform t)
        {
            if (id == "cat")
            {
                Color white = TM.Hex("#FFFDF8");
                TM.Prim(PrimitiveType.Sphere, "Body", t, new Vector3(0, 0.22f, -0.05f), new Vector3(0.4f, 0.38f, 0.44f), white);
                TM.Prim(PrimitiveType.Sphere, "Head", t, new Vector3(0, 0.5f, 0.08f), Vector3.one * 0.4f, white);
                foreach (var x in new[] { -0.12f, 0.12f })
                {
                    TM.MeshObj("Ear", t, TM.Cone(0.08f, 0.14f, 4), new Vector3(x, 0.64f, 0.08f), white);
                    TM.Prim(PrimitiveType.Sphere, "Eye", t, new Vector3(x * 0.7f, 0.52f, 0.27f), new Vector3(0.045f, 0.06f, 0.02f), TM.Ink);
                }
                TM.Prim(PrimitiveType.Sphere, "Nose", t, new Vector3(0, 0.47f, 0.28f), Vector3.one * 0.035f, TM.Hex("#FF8AA8"));
                TM.Prim(PrimitiveType.Sphere, "Spot", t, new Vector3(0.1f, 0.6f, 0.04f), new Vector3(0.14f, 0.1f, 0.14f), TM.Hex("#E8A23A"));
                TM.MeshObj("Collar", t, TM.Torus(0.13f, 0.025f, 16, 4), new Vector3(0, 0.33f, 0.08f), TM.Hex("#D8343A"));
                TM.Prim(PrimitiveType.Sphere, "Bell", t, new Vector3(0, 0.29f, 0.21f), Vector3.one * 0.07f, TM.Hex("#FFC93C"), 0.4f);
                var paw = TM.Empty("Paw", t, new Vector3(0.17f, 0.42f, 0.18f));
                TM.Prim(PrimitiveType.Capsule, "Arm", paw, new Vector3(0, 0.06f, 0), new Vector3(0.09f, 0.1f, 0.09f), white);
                paw.gameObject.AddComponent<Bob>().amp = 0.03f;
            }
            else
            {
                Color fur = TM.Hex("#B07A4A");
                TM.Prim(PrimitiveType.Capsule, "Body", t, new Vector3(0, 0.42f, -0.05f), new Vector3(0.3f, 0.28f, 0.3f), fur, 0, new Vector3(90, 0, 0));
                foreach (var x in new[] { -0.09f, 0.09f })
                    foreach (var z in new[] { -0.17f, 0.1f })
                        TM.Prim(PrimitiveType.Cylinder, "Leg", t, new Vector3(x, 0.15f, z), new Vector3(0.06f, 0.15f, 0.06f), fur * 0.85f);
                TM.Prim(PrimitiveType.Sphere, "Head", t, new Vector3(0, 0.66f, 0.18f), new Vector3(0.26f, 0.26f, 0.3f), fur);
                TM.Prim(PrimitiveType.Sphere, "Snout", t, new Vector3(0, 0.62f, 0.31f), new Vector3(0.12f, 0.1f, 0.1f), TM.Hex("#F4E3C8"));
                foreach (var x in new[] { -0.08f, 0.08f })
                {
                    TM.Prim(PrimitiveType.Sphere, "Eye", t, new Vector3(x, 0.7f, 0.29f), new Vector3(0.04f, 0.05f, 0.02f), TM.Ink);
                    TM.Prim(PrimitiveType.Sphere, "Ear", t, new Vector3(x * 2.2f, 0.74f, 0.14f), new Vector3(0.14f, 0.06f, 0.08f), fur);
                }
                for (int i = 0; i < 5; i++)
                    TM.Prim(PrimitiveType.Sphere, "Spot", t, new Vector3((i % 2 == 0 ? -0.1f : 0.1f), 0.53f, -0.2f + i * 0.07f), Vector3.one * 0.05f, TM.Hex("#FFF4E0"));
                TM.Prim(PrimitiveType.Sphere, "Tail", t, new Vector3(0, 0.5f, -0.24f), Vector3.one * 0.08f, TM.Hex("#FFF4E0"));
            }
        }

        static GameObject BuildPet(string id, Transform owner)
        {
            var go = new GameObject("Pet_" + id);
            go.transform.SetParent(owner.parent, false);
            go.transform.position = owner.position - owner.forward * 1.2f + owner.right * 0.8f;
            var model = TM.Empty("Model", go.transform, Vector3.zero);
            BuildPetModel(id, model);
            var f = go.AddComponent<PetFollow>();
            f.owner = owner;
            f.model = model;
            return go;
        }

        // ---------------- 隱藏景點（每站一枚紫色隱藏印章）----------------
        public class HiddenSpot { public string area, poiId; public Vector3 local; }

        public static readonly HiddenSpot[] HiddenSpots =
        {
            new HiddenSpot { area = "hotel",        poiId = "h_nakazaki", local = new Vector3(-24f, 0, -22f) },
            new HiddenSpot { area = "dotonbori",    poiId = "h_hozenji",  local = new Vector3(-30f, 0, 36f) },
            new HiddenSpot { area = "usj",          poiId = "h_tempozan", local = new Vector3(-20f, 0, -16f) },
            new HiddenSpot { area = "shinsaibashi", poiId = "h_amemura",  local = new Vector3(3.5f, 0, -27f) },
            new HiddenSpot { area = "castle",       poiId = "h_nishinomaru", local = new Vector3(-22f, 0, -20f) },
            new HiddenSpot { area = "kuromon",      poiId = "h_doguya",   local = new Vector3(2.5f, 0, -27f) },
            new HiddenSpot { area = "shinsekai",    poiId = "h_janjan",   local = new Vector3(7f, 0, -25f) },
            new HiddenSpot { area = "umeda",        poiId = "h_hepfive",  local = new Vector3(-21f, 0, -20f) },
            new HiddenSpot { area = "onsen",        poiId = "h_kaiyukan", local = new Vector3(18f, 0, -18f) },
        };
    }

    /// <summary>跟寵：跟在玩家右後方，走路時一跳一跳。</summary>
    public class PetFollow : MonoBehaviour
    {
        public Transform owner, model;
        float hop;

        void LateUpdate()
        {
            if (owner == null) return;
            Vector3 target = owner.position - owner.forward * 1.1f + owner.right * 0.75f;
            target.y = owner.position.y;
            Vector3 d = target - transform.position;
            if (d.magnitude > 8f) { transform.position = target; return; } // 換站被傳送時直接跟上
            Vector3 flat = d; flat.y = 0;
            float speed = Mathf.Clamp(flat.magnitude * 3f, 0f, 9f);
            if (flat.magnitude > 0.05f)
            {
                transform.position += flat.normalized * Mathf.Min(speed * Time.deltaTime, flat.magnitude);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat.normalized), Time.deltaTime * 8f);
                hop += Time.deltaTime * 14f;
            }
            var p = transform.position; p.y = Mathf.Lerp(p.y, owner.position.y, Time.deltaTime * 10f); transform.position = p;
            if (model) model.localPosition = Vector3.up * (speed > 0.3f ? Mathf.Abs(Mathf.Sin(hop)) * 0.12f : 0f);
        }
    }
}

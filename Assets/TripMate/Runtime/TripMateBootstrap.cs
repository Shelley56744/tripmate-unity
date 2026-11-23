using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 場景裡唯一需要的物件。按 Play 時用程式把整個遊戲搭起來：
    /// 小鎮（飯店、道頓堀）＋ 7 個站點場景、玩家、鏡頭、UI、站點、嚮導趣趣、旅遊印章，
    /// 再交給 LevelDirector 跑流程（主選單 → 快測 → 10 站 → 結算 → 排行程 → 結局）。
    /// </summary>
    public class TripMateBootstrap : MonoBehaviour
    {
        [Header("中文字型（選填）：中文顯示成方塊時，拖一個 Noto Sans TC 之類的 .ttf 進來")]
        public Font cjkFont;

        Transform player;

        void Awake()
        {
            if (cjkFont) TM.OverrideFont = cjkFont;
            Collectible.ResetAll();
            Interactable.ResetAll();
            Objective.Clear();
            World.Reset();
            Session.Reset();

            // 鏡頭
            var cam = Camera.main;
            if (cam == null)
            {
                var cg = new GameObject("Main Camera");
                cg.tag = "MainCamera";
                cam = cg.AddComponent<Camera>();
                cg.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 160f;

            // 場景裡如果原本有預設的 Directional Light，關掉，改用 World 的光線
#if UNITY_2023_1_OR_NEWER
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
#else
            foreach (var l in FindObjectsOfType<Light>())
#endif
                if (l.type == LightType.Directional) l.gameObject.SetActive(false);

            // 場景：小鎮＋各站
            TownBuilder.Build(transform);
            var townStamps = TM.Empty("TownStamps", transform, Vector3.zero);
            foreach (var p in TownBuilder.StampPoints) Collectible.Create(townStamps, p);
            foreach (var h in Accessories.HiddenSpots)
                if (h.area == "hotel" || h.area == "dotonbori") Collectible.CreateHidden(townStamps, h.local, h.poiId);
            AreaBuilder.BuildAll(transform);

            // 玩家（先是沒有裝扮的基本趣趣，快測完才換上人格裝扮）
            var pq = Ququ.Create("Player_趣趣", transform, TownBuilder.SpawnPoint, Persona.Base);
            player = pq.transform;
            var cc = pq.gameObject.AddComponent<CharacterController>();
            cc.height = 1.05f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0, 0.54f, 0);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 50f;
            var mover = pq.gameObject.AddComponent<PlayerMover>();
            mover.locked = true;
            Collectible.Player = player;
            Accessories.Apply(pq); // 商店買的配件

            var follow = cam.gameObject.GetComponent<FollowCamera>();
            if (follow == null) follow = cam.gameObject.AddComponent<FollowCamera>();
            follow.target = player;

            // UI
            var ui = new GameObject("DialogueUI").AddComponent<DialogueUI>();
            ui.transform.SetParent(transform, false);
            ui.player = mover;
            ui.cam = follow;

            // 站點（整趟共用一個，換站時搬過去）
            var zoneGo = new GameObject("QuestZone");
            zoneGo.transform.SetParent(transform, false);
            var zone = zoneGo.AddComponent<QuestZone>();
            zone.player = player;

            // 嚮導趣趣：每站站在站點後面出題
            var guide = Ququ.Create("Guide_趣趣", transform, new Vector3(-11.2f, 0, 2f), Persona.Base);
            guide.transform.localScale = Vector3.one * 1.15f;
            var star = TM.Empty("GuideStar", guide.transform, new Vector3(0, Ququ.BodyTopY + 0.75f, 0));
            TM.Prim(PrimitiveType.Cube, "S1", star, Vector3.zero, new Vector3(0.08f, 0.36f, 0.08f), TM.Hex("#FFC93C"), 1.2f);
            TM.Prim(PrimitiveType.Cube, "S2", star, Vector3.zero, new Vector3(0.36f, 0.08f, 0.08f), TM.Hex("#FFC93C"), 1.2f);
            star.gameObject.AddComponent<Spin>().speed = new Vector3(0, 90, 0);
            star.gameObject.AddComponent<Bob>().amp = 0.08f;
            var tag = TM.Text3D("趣趣", guide.transform, new Vector3(0, Ququ.BodyTopY + 1.15f, 0), 0.22f, TM.Hex("#FFC93C"));
            tag.gameObject.AddComponent<FaceCamera>();

            // 流程
            var dirGo = new GameObject("LevelDirector");
            dirGo.transform.SetParent(transform, false);
            var dir = dirGo.AddComponent<LevelDirector>();
            dir.player = mover;
            dir.playerQ = pq;
            dir.cam = follow;
            dir.ui = ui;
            dir.zone = zone;
            dir.guide = guide;
        }

        void Update()
        {
            // 萬一掉出地圖，送回這一站的出生點
            if (player && player.position.y < -4f)
            {
                var cc = player.GetComponent<CharacterController>();
                cc.enabled = false;
                var a = World.Current;
                player.position = a != null ? a.W(a.spawn) : TownBuilder.SpawnPoint;
                cc.enabled = true;
            }
        }
    }
}

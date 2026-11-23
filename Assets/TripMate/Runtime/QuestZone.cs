using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 每一站的站點：地上的光圈、頭上的「！」、遠遠看得到的光柱，以及從出生點一路亮過去的引導光點。
    /// 整趟旅程共用同一個，換站時用 Place() 搬過去。玩家走進光圈時 Triggered 變成 true。
    /// </summary>
    public class QuestZone : MonoBehaviour
    {
        public float radius = 2.6f;
        public Transform player;
        public bool active;
        public bool Triggered { get; private set; }

        GameObject marker, beacon, ring;
        Breadcrumbs guide;

        void Awake()
        {
            marker = new GameObject("QuestMarker");
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = new Vector3(0, 4.2f, 0);
            TM.Prim(PrimitiveType.Sphere, "Ball", marker.transform, Vector3.zero, Vector3.one * 1.1f, TM.Hex("#FFC93C"), 0.9f);
            TM.Text3D("!", marker.transform, new Vector3(0, -0.05f, -0.6f), 0.95f, TM.Ink);
            marker.AddComponent<FaceCamera>();
            marker.AddComponent<Bob>().amp = 0.25f;
            beacon = TM.Prim(PrimitiveType.Cylinder, "Beacon", transform, new Vector3(0, 14f, 0), new Vector3(0.45f, 14f, 0.45f), TM.Hex("#FFE27A"), 1.4f);
            ring = TM.MeshObj("Ring", transform, TM.Torus(radius * 0.85f, 0.06f, 40, 6), new Vector3(0, 0.06f, 0), TM.Hex("#FFC93C"), 1.2f);
            ring.AddComponent<Spin>().speed = new Vector3(0, 20, 0);
            SetVisible(false);
        }

        /// <summary>搬到新站點，並重新鋪一條從出生點過來的引導光點。</summary>
        public void Place(Vector3 world, Vector3 from)
        {
            transform.position = world;
            Triggered = false;
            if (guide) TM.Kill(guide.gameObject);
            Vector3 dir = world - from; dir.y = 0;
            Vector3 end = world - dir.normalized * (radius + 0.3f);
            guide = Breadcrumbs.Build(null, new[] { from + dir.normalized * 1.5f, end });
            SetVisible(active);
        }

        public void Activate(bool on)
        {
            active = on;
            if (on) Triggered = false;
            SetVisible(on);
        }

        void SetVisible(bool on)
        {
            if (marker) marker.SetActive(on);
            if (beacon) beacon.SetActive(on);
            if (ring) ring.SetActive(on);
            if (guide) guide.gameObject.SetActive(on);
        }

        void Update()
        {
            if (!active || player == null) return;
            if (DialogueUI.Instance != null && DialogueUI.Instance.Busy) return;
            Vector3 d = player.position - transform.position;
            d.y = 0;
            if (d.magnitude < radius)
            {
                active = false;
                Triggered = true;
                SetVisible(false);
            }
        }
    }
}

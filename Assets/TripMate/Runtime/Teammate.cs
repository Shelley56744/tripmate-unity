using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 旅伴（目前由電腦操控）：每個人有自己的「據點」和個性，平常在據點附近做自己的事，
    /// 被玩家找到之後自己走去集合點；投票時才圍到玩家身邊。
    /// 之後做多人連線時，把這個換成網路同步的玩家即可。
    /// </summary>
    public class Teammate : MonoBehaviour
    {
        public enum Mode { Wander, Travel }

        public Transform player;
        public int index;
        public string displayName = "旅伴";
        public Vector3 home;
        public float wanderRadius = 2f;
        public float walkSpeed = 2.5f;
        public float minPause = 1.5f, maxPause = 4f;
        public bool found;
        [HideInInspector] public Mode mode = Mode.Wander;
        [HideInInspector] public Vector3? gatherPoint;

        readonly List<Vector3> route = new List<Vector3>();
        Ququ q;
        Vector3 lastPos, wanderTarget;
        float pause;
        bool hopping;
        GameObject marker;

        void Awake()
        {
            q = GetComponent<Ququ>();
            lastPos = transform.position;
        }

        void Start()
        {
            wanderTarget = transform.position;
            pause = Random.Range(0.2f, 1.5f);
        }

        /// <summary>依人格決定個性：走多快、停多久、活動範圍。</summary>
        public void ApplyPersonality(Persona p)
        {
            switch (p)
            {
                case Persona.Soldier: walkSpeed = 4.2f; minPause = 0.4f; maxPause = 1.2f; wanderRadius = 2.2f; break; // 快步來回踱步
                case Persona.Capybara: walkSpeed = 1.3f; minPause = 4f; maxPause = 8f; wanderRadius = 1.2f; break;   // 慢、常停
                case Persona.Camera: walkSpeed = 3f; minPause = 1.5f; maxPause = 3f; wanderRadius = 3f; break;       // 到處找角度
                case Persona.Taxi: walkSpeed = 2f; minPause = 3f; maxPause = 6f; wanderRadius = 1f; break;          // 站著等車
            }
        }

        /// <summary>頭上的「！」：還沒被找到時顯示。</summary>
        public void ShowMarker(bool on)
        {
            if (marker == null && on)
            {
                marker = new GameObject("FindMarker");
                marker.transform.SetParent(transform, false);
                marker.transform.localPosition = new Vector3(0, Ququ.BodyTopY + 1.45f, 0);
                TM.Prim(PrimitiveType.Sphere, "Ball", marker.transform, Vector3.zero, Vector3.one * 0.5f, TM.Hex("#FFC93C"), 0.9f);
                TM.Text3D("!", marker.transform, new Vector3(0, -0.02f, -0.28f), 0.42f, TM.Ink);
                marker.AddComponent<FaceCamera>();
                marker.AddComponent<Bob>().amp = 0.12f;
            }
            if (marker) marker.SetActive(on);
        }

        /// <summary>直接傳送到某個位置（換站時用）。</summary>
        public void Teleport(Vector3 pos)
        {
            transform.position = pos;
            lastPos = pos;
            route.Clear();
            mode = Mode.Wander;
            home = pos;
            wanderTarget = pos;
            gatherPoint = null;
            pause = Random.Range(0.3f, 1.2f);
        }

        /// <summary>沿著路線走過去，到了之後在那裡附近活動。</summary>
        public void TravelTo(params Vector3[] points)
        {
            route.Clear();
            route.AddRange(points);
            mode = Mode.Travel;
        }

        void Update()
        {
            Vector3 goal = transform.position;
            float speed = walkSpeed;
            const float stopDist = 0.08f;

            if (gatherPoint.HasValue)
            {
                goal = gatherPoint.Value;
                speed = 5f;
            }
            else if (mode == Mode.Travel)
            {
                if (route.Count == 0) mode = Mode.Wander;
                else
                {
                    goal = route[0];
                    speed = Mathf.Max(walkSpeed, 3.2f);
                    Vector3 d = goal - transform.position; d.y = 0;
                    if (d.magnitude < 0.3f)
                    {
                        home = route[route.Count - 1];
                        route.RemoveAt(0);
                        if (route.Count == 0) { mode = Mode.Wander; wanderRadius = 1.2f; pause = 1f; wanderTarget = home; }
                    }
                }
            }
            else
            {
                if (pause > 0f)
                {
                    pause -= Time.deltaTime;
                }
                else
                {
                    goal = wanderTarget;
                    Vector3 d = goal - transform.position; d.y = 0;
                    if (d.magnitude < 0.15f)
                    {
                        pause = Random.Range(minPause, maxPause);
                        var c = Random.insideUnitCircle * wanderRadius;
                        wanderTarget = home + new Vector3(c.x, 0, c.y);
                        // 打卡機：停下來就「拍一張」
                        if (q != null && q.persona == Persona.Camera) Fx.Sparkle(transform.position + Vector3.up * 1.3f + transform.forward * 0.5f, 8);
                    }
                }
            }
            goal.y = 0f;

            Vector3 to = goal - transform.position;
            to.y = 0;
            if (to.magnitude > stopDist)
            {
                float sp = Mathf.Min(speed * Time.deltaTime, to.magnitude);
                transform.position += to.normalized * sp;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to.normalized), 540f * Time.deltaTime);
            }
            else if (player != null)
            {
                // 停下來時，玩家靠近就轉頭看玩家
                Vector3 look = player.position - transform.position; look.y = 0;
                if (look.sqrMagnitude < 25f && look.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), 360f * Time.deltaTime);
            }

            // 動畫：輪子、彈跳
            float moved = (transform.position - lastPos).magnitude;
            lastPos = transform.position;
            if (q != null)
            {
                foreach (var w in q.wheels) w.Rotate(Vector3.right, moved / 0.075f * Mathf.Rad2Deg, Space.Self);
                float amount = Mathf.Clamp01(moved / (speed * Time.deltaTime + 1e-4f));
                if (!hopping)
                    q.visual.localPosition = Vector3.up * (amount > 0.1f ? Mathf.Abs(Mathf.Sin(Time.time * 14f + index)) * 0.04f : Mathf.Sin(Time.time * 2.4f + index) * 0.012f);
            }
        }

        public IEnumerator Hop(int times = 2)
        {
            if (q == null) yield break;
            hopping = true;
            for (int i = 0; i < times; i++)
            {
                for (float t = 0; t < 1f; t += Time.deltaTime * 4f)
                {
                    q.visual.localPosition = Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.35f;
                    yield return null;
                }
            }
            hopping = false;
        }

        public IEnumerator Shake()
        {
            if (q == null) yield break;
            hopping = true;
            for (float t = 0; t < 0.8f; t += Time.deltaTime)
            {
                q.visual.localEulerAngles = new Vector3(0, 0, Mathf.Sin(t * 40f) * 10f);
                yield return null;
            }
            q.visual.localEulerAngles = Vector3.zero;
            hopping = false;
        }
    }
}

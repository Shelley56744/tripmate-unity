using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 第三人稱跟隨鏡頭：
    /// - 按住滑鼠（左鍵或右鍵）拖曳：左右自由轉、上下調整高低，連續平滑
    /// - 一段時間沒碰滑鼠、角色往前走時，鏡頭會慢慢轉到角色身後
    /// - 滾輪：遠近三段；Q：快速轉 45 度（E 是互動鍵）
    /// - 鏡頭被房子、樹擋住時會自動拉近，不會穿牆
    /// - 對話時自動拉近、壓低
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;

        [Header("角度")]
        public float pitch = 18f;          // 預設俯角：低一點才看得到前方和地平線
        public float minPitch = 4f;
        public float maxPitch = 60f;
        public float mouseSensitivity = 0.22f; // 每像素轉幾度
        public float stickSpeed = 140f;        // 手把右搖桿每秒轉幾度

        [Header("距離")]
        public float[] zoomSteps = { 6.5f, 9f, 13f };
        public int zoomIndex = 1;
        public float lookAhead = 2f;
        public float fieldOfView = 50f;

        [Header("自動轉到身後")]
        public bool autoAlign = true;
        public float autoAlignDelay = 1.5f;
        public float autoAlignSpeed = 50f;

        [Header("對話時")]
        public float closePitch = 14f;
        public float closeDistance = 6f;
        [HideInInspector] public bool close;
        /// <summary>主選單、排行程時：鏡頭慢慢繞著角色轉。</summary>
        [HideInInspector] public bool showcase;

        float yaw, targetYaw, curPitch, targetPitch, curDist, lastManual = -99f;
        Vector3 focus, focusVel;
        PlayerMover mover;

        void Start()
        {
            var cam = GetComponent<Camera>();
            if (cam) cam.fieldOfView = fieldOfView;
            Snap();
        }

        /// <summary>換站時：鏡頭直接跳到角色背後。</summary>
        public void SnapBehind()
        {
            if (!target) return;
            yaw = targetYaw = target.eulerAngles.y;
            targetPitch = pitch;
            focusVel = Vector3.zero;
            Snap();
        }

        public void Snap()
        {
            if (!target) return;
            mover = target.GetComponent<PlayerMover>();
            targetPitch = curPitch = pitch;
            curDist = zoomSteps[zoomIndex];
            focus = Focus();
            Apply(curDist);
        }

        Vector3 Focus()
        {
            var flatFwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            return target.position + Vector3.up * 1.1f + flatFwd * (close ? 0.4f : lookAhead);
        }

        void Apply(float dist)
        {
            var rot = Quaternion.Euler(curPitch, yaw, 0);
            transform.position = focus - rot * Vector3.forward * dist;
            transform.rotation = rot;
        }

        void LateUpdate()
        {
            if (!target) return;
            if (mover == null) mover = target.GetComponent<PlayerMover>();
            bool busy = DialogueUI.Instance != null && DialogueUI.Instance.Busy;
            bool control = !close && !busy && !showcase;
            if (showcase)
            {
                targetYaw += 7f * Time.deltaTime;
                targetPitch = 20f;
            }

            if (control)
            {
                // 滑鼠拖曳／右搖桿：自由轉
                Vector2 d = InputCompat.MouseDrag();
                Vector2 stick = InputCompat.LookStick();
                if (stick.sqrMagnitude > 0.04f) d += stick * stickSpeed * Time.deltaTime / mouseSensitivity;
                if (d.sqrMagnitude > 0.01f)
                {
                    targetYaw += d.x * mouseSensitivity;
                    targetPitch = Mathf.Clamp(targetPitch - d.y * mouseSensitivity, minPitch, maxPitch);
                    lastManual = Time.time;
                }
                if (InputCompat.Down("q")) { targetYaw -= 45f; lastManual = Time.time; }

                float sc = InputCompat.Scroll();
                if (sc > 0.1f) zoomIndex = Mathf.Max(0, zoomIndex - 1);
                if (sc < -0.1f) zoomIndex = Mathf.Min(zoomSteps.Length - 1, zoomIndex + 1);

                // 往前走一陣子沒動滑鼠：慢慢轉到角色身後（側走、後退不會觸發，避免繞圈）
                if (autoAlign && mover != null && Time.time - lastManual > autoAlignDelay)
                {
                    Vector3 v = mover.Velocity;
                    v.y = 0;
                    if (v.magnitude > 1.5f)
                    {
                        float moveYaw = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                        float diff = Mathf.DeltaAngle(targetYaw, moveYaw);
                        if (Mathf.Abs(diff) < 60f)
                            targetYaw = Mathf.MoveTowardsAngle(targetYaw, moveYaw, autoAlignSpeed * Time.deltaTime * (Mathf.Abs(diff) / 30f));
                    }
                }
            }

            // 平滑跟上（滑鼠轉動要跟手，所以這裡用比較快的係數）
            float kFast = 1f - Mathf.Exp(-Time.deltaTime * 14f);
            float kSlow = 1f - Mathf.Exp(-Time.deltaTime * 4f);
            yaw = Mathf.LerpAngle(yaw, targetYaw, kFast);
            curPitch = Mathf.Lerp(curPitch, close ? closePitch : targetPitch, close ? kSlow : kFast);
            curDist = Mathf.Lerp(curDist, close ? closeDistance : showcase ? 14f : zoomSteps[zoomIndex], kSlow);
            focus = Vector3.SmoothDamp(focus, Focus(), ref focusVel, 0.12f);

            // 防穿牆：從焦點往鏡頭方向打一條球形射線，撞到東西就拉近
            float dist = curDist;
            var rot = Quaternion.Euler(curPitch, yaw, 0);
            Vector3 back = -(rot * Vector3.forward);
            var hits = Physics.SphereCastAll(focus, 0.3f, back, curDist);
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (h.collider.transform == target || h.collider.transform.IsChildOf(target)) continue;
                if (h.distance > 0.01f) dist = Mathf.Min(dist, Mathf.Max(1.2f, h.distance - 0.15f));
            }
            Apply(dist);
        }
    }
}

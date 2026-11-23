using System.Collections;
using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 玩家操控：WASD 移動（依鏡頭方向）、Shift 跑、空白鍵跳。人格由開場測驗決定，遊戲中不能換。
    /// 手感：起步加速、輪子跟著轉、身體前傾、跳起拉長、落地壓扁、雪地揚塵、音效。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMover : MonoBehaviour
    {
        public float walkSpeed = 4.5f;
        public float runSpeed = 7.5f;
        public float accel = 18f;
        public float turnSpeed = 900f;
        public float jumpSpeed = 5.2f;
        public float gravity = -16f;
        [HideInInspector] public bool locked;
        /// <summary>累積實際走動的秒數（操作提示用）。</summary>
        [HideInInspector] public float movedSeconds;

        CharacterController cc;
        Ququ q;
        Vector3 vel;     // 水平速度
        /// <summary>目前水平速度（鏡頭自動轉到身後用）。</summary>
        public Vector3 Velocity => vel;
        float vy;
        float tilt;
        float squash;    // >0 壓扁，<0 拉長
        bool wasGrounded = true;
        float airTime;
        float dustTimer;
        bool hopping;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            q = GetComponent<Ququ>();
        }

        void Update()
        {
            Vector2 input = locked ? Vector2.zero : InputCompat.Move();
            Transform cam = Camera.main != null ? Camera.main.transform : null;
            Vector3 fwd = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = cam ? Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized : Vector3.right;
            Vector3 dir = fwd * input.y + right * input.x;
            bool running = InputCompat.Held("shift");
            float maxSpeed = running ? runSpeed : walkSpeed;

            // 起步／停下有一點慣性
            vel = Vector3.MoveTowards(vel, dir * maxSpeed, accel * Time.deltaTime);

            bool grounded = cc.isGrounded;
            if (grounded)
            {
                if (!wasGrounded && airTime > 0.15f)
                {
                    squash = Mathf.Clamp(airTime * 2.2f, 0.4f, 1f);
                    Sfx.Land();
                    Fx.Dust(transform.position + Vector3.up * 0.05f, 8);
                }
                airTime = 0;
                vy = -1f;
                if (!locked && InputCompat.Down("space"))
                {
                    vy = jumpSpeed;
                    squash = -0.7f;
                    Sfx.Jump();
                    Fx.Dust(transform.position + Vector3.up * 0.05f, 4);
                }
            }
            else airTime += Time.deltaTime;
            wasGrounded = grounded;
            vy += gravity * Time.deltaTime;

            Vector3 move = vel;
            move.y = vy;
            cc.Move(move * Time.deltaTime);

            if (dir.sqrMagnitude > 0.001f)
            {
                var target = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
                movedSeconds += Time.deltaTime;
            }

            // 跑步時腳下揚起雪
            float speed01 = vel.magnitude / runSpeed;
            if (grounded && vel.magnitude > walkSpeed * 0.9f)
            {
                dustTimer -= Time.deltaTime;
                if (dustTimer <= 0) { Fx.Dust(transform.position - transform.forward * 0.3f + Vector3.up * 0.05f, 2); dustTimer = 0.08f; }
            }

            Animate(speed01, grounded);
        }

        void Animate(float speed01, bool grounded)
        {
            if (q == null) return;
            float dist = vel.magnitude * Time.deltaTime;
            foreach (var w in q.wheels) w.Rotate(Vector3.right, dist / 0.075f * Mathf.Rad2Deg, Space.Self);

            tilt = Mathf.Lerp(tilt, speed01 * 14f, Time.deltaTime * 8f);
            float bounceFreq = 10f + speed01 * 8f;
            float bounce = (grounded && speed01 > 0.05f) ? Mathf.Abs(Mathf.Sin(Time.time * bounceFreq)) * (0.03f + speed01 * 0.04f)
                                                         : Mathf.Sin(Time.time * 2.4f) * 0.012f;
            if (!hopping) q.visual.localPosition = new Vector3(0, bounce, 0);
            q.visual.localEulerAngles = new Vector3(tilt, 0, 0);

            // 壓扁／拉長，慢慢彈回
            squash = Mathf.Lerp(squash, 0f, Time.deltaTime * 9f);
            float sx = 1f + squash * 0.18f, sy = 1f - squash * 0.2f;
            q.visual.localScale = new Vector3(sx, sy, sx);

            float swing = speed01 > 0.05f ? Mathf.Sin(Time.time * bounceFreq) * (20f + speed01 * 25f) : 0f;
            q.armL.localEulerAngles = new Vector3(swing, 0, 0);
            q.armR.localEulerAngles = new Vector3(-swing, 0, 0);
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
    }
}

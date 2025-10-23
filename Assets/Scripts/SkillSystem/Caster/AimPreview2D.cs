using UnityEngine;

namespace RPG
{
    /// <summary>
    /// 2D 瞄準預覽：
    /// - 方向只讀 AimSource2D。
    /// - 可選是否在預覽時用敵人/障礙物遮罩卡終點（線與圈會停在第一個命中點）。
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class AimPreview2D : MonoBehaviour
    {
        [Header("引用")]
        public SkillCaster caster;
        public MainPointComponent main;
        public AimSource2D aimSource;

        [Header("預覽遮罩")]
        [Tooltip("是否使用敵人/障礙物遮罩讓終點卡在命中點")]
        public bool useMaskCollision = true;
        public LayerMask enemyMask = 0;
        public LayerMask obstacleMask = 0;

        [Header("顯示參數")]
        public Color lineColor = new Color(1f, 0.9f, 0.2f, 1f);
        public Color circleColor = new Color(0.2f, 0.8f, 1f, 0.9f);
        public float lineWidth = 0.035f;
        public float circleWidth = 0.03f;
        public int circleSegments = 48;
        public float dotRadius = 0.25f;

        LineRenderer line, circle;
        static Material s_lineMat;

        void Awake()
        {
            SetupLR(ref line, "AimLine2D", lineColor, lineWidth);
            SetupLR(ref circle, "AimCircle2D", circleColor, circleWidth, true, circleSegments + 1);
            if (!caster) caster = GetComponent<SkillCaster>();
            if (!main) main = GetComponent<MainPointComponent>();
            if (!aimSource) aimSource = GetComponent<AimSource2D>();
        }

        void OnDestroy()
        {
            if (line) Destroy(line.gameObject);
            if (circle) Destroy(circle.gameObject);
        }

        void SetupLR(ref LineRenderer lr, string name, Color color, float width, bool loop = false, int posCount = 2)
        {
            if (s_lineMat == null)
                s_lineMat = new Material(Shader.Find("Sprites/Default"));

            lr = new GameObject(name).AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.material = s_lineMat;
            lr.positionCount = posCount;
            lr.widthMultiplier = width;
            lr.startColor = lr.endColor = color;
            lr.loop = loop;
            lr.numCornerVertices = 4;
            lr.numCapVertices = 4;
        }

        void LateUpdate()
        {
            if (!caster || caster.Skills == null || caster.Skills.Count == 0) return;

            var data = caster.Skills[Mathf.Clamp(caster.currentSkillIndex, 0, caster.Skills.Count - 1)];
            if (!data) return;

            var comp = SkillCalculator.Compute(data, main ? main.MP : MainPoint.Zero);
            Vector3 origin = caster.firePoint ? (Vector3)caster.firePoint.position : transform.position;

            // ✅ 方向只讀 AimSource2D（滑鼠優先、鍵盤備援）
            Vector2 dir = (aimSource && aimSource.AimDir.sqrMagnitude > 0.0001f)
                            ? aimSource.AimDir
                            : Vector2.right;

            float dist = Mathf.Max(0.1f, caster.rayDistance);

            // 計算終點（是否用遮罩卡住）
            Vector3 end = origin + (Vector3)(dir * dist);
            if (useMaskCollision)
            {
                int mask = enemyMask | obstacleMask;
                RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, mask);
                if (hit.collider != null) end = hit.point;
            }

            // 畫線
            line.enabled = true;
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, end);

            // 畫終點圈
            float r = (data.HitType == HitType.Area)
                        ? Mathf.Max(0.05f, comp.AreaRadius)
                        : Mathf.Max(0.02f, dotRadius);
            DrawCircle(circle, end, r);
        }

        void DrawCircle(LineRenderer lr, Vector3 center, float radius)
        {
            lr.enabled = true;
            if (lr.positionCount != circleSegments + 1)
                lr.positionCount = circleSegments + 1;

            float step = 2f * Mathf.PI / Mathf.Max(6, circleSegments);
            for (int i = 0; i <= circleSegments; i++)
            {
                float a = i * step;
                Vector3 p = new Vector3(
                    center.x + Mathf.Cos(a) * radius,
                    center.y + Mathf.Sin(a) * radius,
                    0f
                );
                lr.SetPosition(i, p);
            }
        }

        void OnDisable()
        {
            if (line) line.enabled = false;
            if (circle) circle.enabled = false;
        }
    }
}

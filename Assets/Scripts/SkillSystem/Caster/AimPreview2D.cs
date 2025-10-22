using UnityEngine;

namespace RPG
{
    /// <summary>2D 瞄準預覽：一直畫出射線與終點圈，可切換使用滑鼠或 AimSource2D。</summary>
    [DefaultExecutionOrder(10)]
    public class AimPreview2D : MonoBehaviour
    {
        [Header("引用")]
        public SkillCaster caster;
        public MainPointComponent main;
        public AimSource2D aimSource;

        [Header("方向來源")]
        [Tooltip("預覽線是否使用滑鼠方向（關閉則用 AimSource2D）")]
        public bool useMouseForPreview = true;

        [Header("顯示參數")]
        public Color lineColor = new Color(1f, 0.9f, 0.2f, 1f);
        public Color circleColor = new Color(0.2f, 0.8f, 1f, 0.9f);
        public float lineWidth = 0.035f;
        public float circleWidth = 0.03f;
        public int circleSegments = 48;
        public bool snapToHitPoint = true;
        public bool useEnemyMask = false;
        public LayerMask enemyMask = ~0;
        public float dotRadius = 0.25f;

        LineRenderer line, circle;

        void Awake()
        {
            SetupLR(ref line, "AimLine2D", lineColor, lineWidth);
            SetupLR(ref circle, "AimCircle2D", circleColor, circleWidth, true, circleSegments + 1);
            if (!caster) caster = GetComponent<SkillCaster>();
            if (!main) main = GetComponent<MainPointComponent>();
            if (!aimSource) aimSource = GetComponent<AimSource2D>();
        }

        void SetupLR(ref LineRenderer lr, string name, Color color, float width, bool loop = false, int posCount = 2)
        {
            lr = new GameObject(name).AddComponent<LineRenderer>();
            lr.transform.SetParent(null);
            lr.useWorldSpace = true;
            lr.material = new Material(Shader.Find("Sprites/Default"));
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

            var comp = SkillCalculator.Compute(data, main ? main.MP : new MainPoint());
            Vector3 origin = caster.firePoint ? (Vector3)caster.firePoint.position : transform.position;

            Vector2 dir = GetPreviewDir(origin);
            float dist = Mathf.Max(0.1f, caster.rayDistance);
            int mask = useEnemyMask ? enemyMask : ~0;

            Vector3 end = origin + (Vector3)(dir * dist);
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, mask);
            if (snapToHitPoint && hit.collider != null) end = hit.point;

            // 畫線
            line.enabled = true;
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, end);

            // 畫終點圈
            float r = (data.HitType == HitType.Area) ? Mathf.Max(0.05f, comp.AreaRadius)
                                                     : Mathf.Max(0.02f, dotRadius);
            DrawCircle(circle, end, r);
        }

        Vector2 GetPreviewDir(Vector3 origin)
        {
            if (useMouseForPreview && Camera.main != null)
            {
                Vector3 m = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                m.z = 0f;
                Vector2 v = (Vector2)(m - origin);
                if (v.sqrMagnitude > 0.0001f) return v.normalized;
            }
            if (aimSource && aimSource.AimDir.sqrMagnitude > 0.0001f)
                return aimSource.AimDir;

            return Vector2.right;
        }

        void DrawCircle(LineRenderer lr, Vector3 center, float radius)
        {
            lr.enabled = true;
            if (lr.positionCount != circleSegments + 1)
                lr.positionCount = circleSegments + 1;

            float step = 2f * Mathf.PI / circleSegments;
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

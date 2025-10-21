using UnityEngine;

namespace RPG
{
    /// <summary>在 Inspector 即時查看公式結果的測試組件</summary>
    public class MainPointDebug : MonoBehaviour
    {
        public MainPoint MP = new MainPoint();
        public float TestBaseDamage = 100f;
        public float TestBaseSpeed = 5f;

        void OnValidate()
        {
            float dmg = MP.CalcOutgoingDamage(TestBaseDamage);
            float cd = MP.CooldownMul();
            float mp = MP.MpCostMul();
            float ar = MP.AreaScale();
            float spd = MP.MoveSpeed(TestBaseSpeed);

            Debug.Log($"[MainPointDebug] ATK={MP.Attack}, DEF={MP.Defense}, AGI={MP.Agility}, TEC={MP.Technique} " +
                      $"→ Dmg={dmg:F1}, CDx={cd:F3}, MPx={mp:F3}, AreaX={ar:F3}, Move={spd:F2}");
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            float r = 2.5f * MP.AreaScale();
            Gizmos.DrawWireSphere(transform.position, r);
        }
    }
}

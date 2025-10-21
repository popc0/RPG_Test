using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RPG
{
    /// <summary>
    /// 技能施放主控：讀取 SkillData + MainPoint 計算數值，
    /// 用 PlayerStats 扣 MP / 觸發 HUD，並以 Raycast 簡化命中流程。
    /// </summary>
    public class SkillCaster : MonoBehaviour
    {
        [Header("引用")]
        public MainPoint mainPoint;     // 屬性公式來源（攻/防/敏/技）
        public PlayerStats playerStats; // 狀態資源（CurrentHP/CurrentMP），HUD 應綁這個

        [Header("技能")]
        public List<SkillData> Skills = new List<SkillData>();
        public int currentSkillIndex = 0;

        [Header("設定")]
        public float rayDistance = 12f;     // 簡易命中距離
        public bool autoCastOnStart = false;

        private float[] cooldownTimers;
        private bool isCasting;

        void OnEnable()
        {
            EnsureCooldownArray();
        }

        void Start()
        {
            if (autoCastOnStart) Invoke(nameof(TryCastCurrentSkill), 0.2f);
        }

        void Update()
        {
            // 冷卻倒數
            if (cooldownTimers != null)
            {
                for (int i = 0; i < cooldownTimers.Length; i++)
                    if (cooldownTimers[i] > 0f) cooldownTimers[i] -= Time.deltaTime;
            }

            // 測試鍵：空白鍵施放
            if (Input.GetKeyDown(KeyCode.Space))
                TryCastCurrentSkill();
        }

        private void EnsureCooldownArray()
        {
            int n = Mathf.Max(1, Skills?.Count ?? 0);
            if (cooldownTimers == null || cooldownTimers.Length != n)
                cooldownTimers = new float[n];
        }

        public void SetCurrentSkillIndex(int index)
        {
            currentSkillIndex = Mathf.Clamp(index, 0, Mathf.Max(0, (Skills?.Count ?? 1) - 1));
        }

        /// <summary>對當前索引的技能嘗試施放</summary>
        public void TryCastCurrentSkill()
        {
            if (isCasting) return;
            if (mainPoint == null || playerStats == null) { Debug.LogWarning("[SkillCaster] 缺少 mainPoint 或 playerStats"); return; }
            if (Skills == null || Skills.Count == 0) { Debug.LogWarning("[SkillCaster] Skills 為空"); return; }
            if (currentSkillIndex < 0 || currentSkillIndex >= Skills.Count) { Debug.LogWarning("[SkillCaster] currentSkillIndex 超出範圍"); return; }

            var data = Skills[currentSkillIndex];
            if (data == null) { Debug.LogWarning("[SkillCaster] SkillData 為 null"); return; }

            // 冷卻
            if (cooldownTimers[currentSkillIndex] > 0f)
            {
                Debug.Log($"{data.SkillName} 冷卻中 ({cooldownTimers[currentSkillIndex]:F1}s)");
                return;
            }

            // 取得條件
            if (!SkillCalculator.PassAcquireCheck(data, mainPoint))
            {
                Debug.Log($"{data.SkillName} 條件不足，無法施放");
                return;
            }

            // 計算核心數字
            SkillComputed comp = SkillCalculator.Compute(data, mainPoint);

            // MP 檢查與扣除（走狀態層，HUD 會更新）
            if (playerStats.CurrentMP < comp.MpCost)
            {
                Debug.Log($"MP不足 ({playerStats.CurrentMP:F1}/{comp.MpCost:F1})");
                return;
            }
            playerStats.UseMP(comp.MpCost);

            // 冷卻啟動
            cooldownTimers[currentSkillIndex] = comp.Cooldown;

            // 施放流程
            StartCoroutine(CastRoutine(comp));
        }

        private IEnumerator CastRoutine(SkillComputed comp)
        {
            isCasting = true;
            if (comp.CastTime > 0f) yield return new WaitForSeconds(comp.CastTime);

            // 命中解析（簡化：前方 Raycast）
            DoHit(comp);

            isCasting = false;
        }

        private void DoHit(SkillComputed comp)
        {
            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 dir = transform.forward;

            Debug.DrawRay(origin, dir * rayDistance, Color.yellow, 1.5f);

            if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, ~0, QueryTriggerInteraction.Collide))
            {
                var target = hit.collider.GetComponent<EffectApplier>();
                if (target != null)
                {
                    // 這裡示範「在受擊端再算防禦」
                    target.ApplyIncomingRaw(comp.Damage, mainPoint);
                    return;
                }

                Debug.Log($"命中 {hit.collider.name}（無 EffectApplier）");
            }
            else
            {
                Debug.Log("未命中任何目標");
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : BaseController
{
    public override bool isPlayer => false;
    public override bool isDead =>data.isDead;

    public HpBar hpbarPrefab;
    private HpBar hpBarInstance;

    [SerializeField]
    private Canvas worldUICanvas;
    private bool _uiInitialized;

    public bool IsBossPhaseTwo { get; private set; }
    public bool IsBossCharging { get; private set; }

    public bool HasBossPhaseTwoPattern => data != null
        && data.Team == Team.Enemy
        && data.enemyAiType == EnemyAiType.Boss
        && data.bossPhaseTwoAoeSkill != null
        && data.bossPhaseTwoAoeSkill.skillType == SkillType.Damage
        && data.bossPhaseTwoAoeSkill.targetType == SkillTargetType.EnemyAll;

    public override void Init(Character character)
    {
        base.Init(character);
        IsBossPhaseTwo = false;
        ClearBossCharge();
    }

    public bool TryEnterBossPhaseTwo()
    {
        if (!HasBossPhaseTwoPattern || IsBossPhaseTwo || data.isDead
            || data.Hp <= 0 || data.MaxHp <= 0)
            return false;

        if (data.Hp > data.MaxHp * Mathf.Clamp01(data.bossPhaseTwoHpRatio))
            return false;

        IsBossPhaseTwo = true;
        return true;
    }

    public bool TryBeginBossCharge()
    {
        if (!HasBossPhaseTwoPattern || !IsBossPhaseTwo || IsBossCharging
            || data.isDead || data.Hp <= 0 || data.Mp < data.bossPhaseTwoAoeSkill.mpCost)
            return false;

        IsBossCharging = true;
        SetBossIntent("Charging: " + data.bossPhaseTwoAoeSkill.skillName);
        return true;
    }

    public void ClearBossCharge()
    {
        IsBossCharging = false;
        SetBossIntent(null);
    }

    private void SetBossIntent(string message)
    {
        hpBarInstance?.SetIntent(message);
        if (BattleManager.Instance != null
            && BattleManager.Instance.TimeLineIcons.TryGetValue(this, out TimeLineIcon icon)
            && icon != null)
            icon.SetIntent(message);
    }

    //Spawner 后面instantiate 后调用这个
   public void InjectWorldUICanvas(Canvas canvas)
    {
        worldUICanvas = canvas;
    }
    public void EnsureHpBarInitialized()
    {
        if (_uiInitialized) return;
        if (worldUICanvas == null)
        {
            return;
        }
        InitialzeHpBar();
        _uiInitialized = true;
    }
    private void InitialzeHpBar()
    {
        if (hpbarPrefab != null)
        {         
            hpBarInstance = Instantiate(hpbarPrefab, Vector3.zero, Quaternion.identity);
            hpBarInstance.transform.SetParent(worldUICanvas.transform, false);
            hpBarInstance.Bind(this); 
            hpBarInstance.transform.position = transform.position + Vector3.up * 1.5f;
            hpBarInstance.UpdateHp();
        }
    }
    public override void TakeDamage(int damage)
    {
        // BaseController owns death handling, including lethal DOT ticks.
        base.TakeDamage(damage);
        if (hpBarInstance != null)
            hpBarInstance.UpdateHp();
    }
    protected override void OnDeath()
    {
        ClearBossCharge();
        if(hpBarInstance!=null)
        Destroy(hpBarInstance.gameObject); 
        base.OnDeath();
    }
}

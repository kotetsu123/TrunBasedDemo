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
    private void Start()
    {
        BatteleManager.Instance.RigisterCharacter(this);
        InitialzeHpBar();
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
        Debug.Log($"[Enemy] 受伤前hp={data.Hp}");
        data.Hp -= damage;
        Debug.Log($"[Enemy] 受伤后hp={data.Hp}");
        if (data.Hp < 0)
        {
            data.Hp = 0;
        }
        if (hpBarInstance == null)
        {
            Debug.Log($"[hpbarInstance] 是null , 无法更新血条！");
        }
        hpBarInstance.UpdateHp();
        if (data.Hp == 0)
        {
            data.isDead = true;
            hpBarInstance.Hide();
        }
    }
}

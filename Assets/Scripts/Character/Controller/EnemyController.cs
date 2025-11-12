using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    
    //[Header("角色数据")]
    [HideInInspector]
    private Character data;
    public Character Data => data;

    public HpBar hpbarPrefab;
    private HpBar hpBarInstance;

    [SerializeField]
    private Canvas worldUICanvas;
    private void Start()
    {
        InitialzeHpBar();

        if (data == null)
        {
            data = new Character("Enemy", 150, 55, 10, 200f);
            data.Team = Character.TeamType.Enemy;
            data.isPlayer = false;
            BatteleManager.Instance.RigisterCharacter(data);
            Debug.Log($"[EnemyController]Awake over:{data != null}");
        }

        //Debug.Log("EnemyController has rasing ");
      
    }
    private void InitialzeHpBar()
    {
        if (hpbarPrefab != null)
        {
            /* hpBarInstance = Instantiate(hpbarPrefab, transform.position + Vector3.up * 2.0f, Quaternion.identity);
             hpbarPrefab.Bind(data);
             hpBarInstance.transform.SetParent(worldUICanvas.transform, true);
            */

            hpBarInstance = Instantiate(hpbarPrefab, Vector3.zero, Quaternion.identity);
            hpbarPrefab.Bind(data);
            hpBarInstance.transform.SetParent(worldUICanvas.transform, false);
            hpBarInstance.transform.position = transform.position + Vector3.up * 1.5f;
        }
    }
    public void TakeDamage(int dmg)
    {
        data.Hp -= dmg;
        hpBarInstance?.UpdateHp();
        if (data.Hp <= 0)
        {
            data.isDead = true;
            hpBarInstance?.Hide();
        }
    }


}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Game/Data/Enemy DataBase")]
public class EnemyDataBase : ScriptableObject
{
    [SerializeField]private List<EnemyFieldData> enemies=new List<EnemyFieldData>();

    public EnemyFieldData FindById(string enemyId)
    {
        if (string.IsNullOrWhiteSpace(enemyId))
        {
            Debug.LogWarning("[EnemyDataBase] EnemyId is empty.");
            return null;
        }

        foreach(var enemy in enemies)
        {
            if (enemy == null) continue;

            if (enemy.EnemyId == enemyId)
                return enemy;
        }

        Debug.LogWarning($"[EnemyDataBase] EnemyId not found:{enemyId}");

        return null;
    }
}

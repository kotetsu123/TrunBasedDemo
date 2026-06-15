using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Enemy Character DataBase")]
public class EnemyCharacterDataBase : ScriptableObject
{
    [SerializeField] private List<Character> enemies = new List<Character>();

    public Character FindById(string enemyId)
    {
        if (string.IsNullOrWhiteSpace(enemyId))
        {
            Debug.LogWarning("[EnemyCharacterDataBase] EnemyId is empty.");
            return null;
        }

        foreach (Character enemy in enemies)
        {
            if (enemy == null)
                continue;

            if (enemy.characterId == enemyId)
            {
                Debug.Log($"[EnemyCharacterDataBase] Enemy found. enemyId={enemyId}, enemyName={enemy.Name}");
                return enemy;
            }
        }

        Debug.LogWarning($"[EnemyCharacterDataBase] Enemy not found. enemyId={enemyId}");
        return null;
    }
}

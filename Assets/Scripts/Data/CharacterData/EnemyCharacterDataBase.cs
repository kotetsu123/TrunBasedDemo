using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Enemy Character DataBase")]
public class EnemyCharacterDataBase : ScriptableObject
{
    [SerializeField] private List<Character> enemies = new List<Character>();

    public Character FindByEnemyId(string enemyId)
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

            // Enemy table ids are stored on Character.characterId because Character is shared by players and enemies.
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

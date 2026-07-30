using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleIntroCameraType
{
    Normal,
    Boss
}

[System.Serializable]
public class EncounterItemDrop
{
    // One possible item reward for this encounter.
    // Example: Potion, 1-2 count, 50% chance.
    [SerializeField] private ItemData item;
    [SerializeField] private int minCount = 1;
    [SerializeField] private int maxCount = 1;
    // Inspector slider from 0 to 1. 1 means 100% drop chance, 0.5 means 50%, and 0 means never drops.
    [SerializeField, Range(0f, 1f)] private float dropChance = 1f;

    public bool HasItem => item != null;

    public bool TryRoll(out InitialItemStack reward)
    {
        reward = null;

        // No item means this drop row is not valid.
        if (item == null)
            return false;

        // Random.value returns 0-1. If it is higher than dropChance, this item does not drop.
        float roll = Random.value;
        if (roll > dropChance)
        {
            Debug.Log($"[EncounterReward] Drop miss. item={item.itemName}, chance={dropChance:0.##}, roll={roll:0.##}");
            return false;
        }

        // Keep the configured count safe even if Inspector values are set incorrectly.
        // Guard against invalid Inspector values such as 0, negative counts, or max < min.
        int safeMin = Mathf.Max(1, minCount);
        int safeMax = Mathf.Max(safeMin, maxCount);

        // InitialItemStack is reused here because InventoryRuntimeState.AddItem already accepts item/count pairs.
        reward = new InitialItemStack
        {
            item = item,
            count = Random.Range(safeMin, safeMax + 1)
        };

        Debug.Log($"[EncounterReward] Drop success. item={item.itemName}, chance={dropChance:0.##}, roll={roll:0.##}, count={reward.count}");
        return true;
    }
}

[System.Serializable]
public class EncounterEnemyEntry
{
    // This enemyId matches Character.characterId in EnemyCharacterDataBase.
    // EncounterData only describes the battle team, so it uses enemyId here.
    [SerializeField] private string enemyId;
    [SerializeField] private int count = 1;

    public string EnemyId => enemyId;
    public int Count => Mathf.Max(1, count);
    public bool IsValid => !string.IsNullOrWhiteSpace(enemyId) && count > 0;
}

[CreateAssetMenu(menuName ="Game/Data/Encounter Data")]
public class EncounterData : ScriptableObject
{
    [SerializeField] private string encounterId;
    [SerializeField] private List<EncounterEnemyEntry> enemyEntries = new List<EncounterEnemyEntry>();
    // Legacy fallback data from the old encounter setup.
    // Prefer enemyEntries for new encounters.
    [SerializeField] private List<Character> enemyChatacters = new List<Character>();

    // Reward service returns this result package to BattleManager after reward calculation.
    [SerializeField] private int rewardExp = 120;
    [SerializeField] private List<EncounterItemDrop> itemDrops = new List<EncounterItemDrop>();
    [Header("Battle Presentation")]
    [SerializeField] private BattleIntroCameraType introCameraType = BattleIntroCameraType.Normal;

    public string EncounterId => encounterId;
    public IReadOnlyList<EncounterEnemyEntry> EnemyEntries => enemyEntries;
    public List<Character> LegacyEnemyCharacters => enemyChatacters;
    public int RewardExp => rewardExp;
    public IReadOnlyList<EncounterItemDrop> ItemDrops => itemDrops;
    public BattleIntroCameraType IntroCameraType => introCameraType;
    public bool HasEnemyEntries => enemyEntries != null && enemyEntries.Count > 0;

    public bool ValidateConfig()
    {
        bool isValid = true;

        if (string.IsNullOrWhiteSpace(encounterId))
        {
            Debug.LogWarning($"[EncounterData] EncounterId is empty. asset={name}");
            isValid = false;
        }

        if (HasEnemyEntries)
        {
            for (int i = 0; i < enemyEntries.Count; i++)
            {
                EncounterEnemyEntry enemyEntry = enemyEntries[i];
                if (enemyEntry != null && enemyEntry.IsValid)
                    continue;

                Debug.LogWarning($"[EncounterData] Enemy entry is invalid. encounterId={encounterId}, index={i}, asset={name}");
                isValid = false;
            }
        }
        else if (enemyChatacters == null || enemyChatacters.Count == 0)
        {
            Debug.LogWarning($"[EncounterData] Enemy list is empty. encounterId={encounterId}, asset={name}");
            isValid = false;
        }
        else
        {
            for (int i = 0; i < enemyChatacters.Count; i++)
            {
                if (enemyChatacters[i] != null)
                    continue;

                Debug.LogWarning($"[EncounterData] Enemy entry is null. encounterId={encounterId}, index={i}, asset={name}");
                isValid = false;
            }
        }

        if (itemDrops != null)
        {
            for (int i = 0; i < itemDrops.Count; i++)
            {
                EncounterItemDrop drop = itemDrops[i];
                if (drop == null)
                {
                    Debug.LogWarning($"[EncounterData] Item drop entry is null. encounterId={encounterId}, index={i}, asset={name}");
                    isValid = false;
                    continue;
                }

                if (!drop.HasItem)
                {
                    Debug.LogWarning($"[EncounterData] Item drop has no item. encounterId={encounterId}, index={i}, asset={name}");
                    isValid = false;
                }
            }
        }

        return isValid;
    }
}

// Reward service returns this result package to BattleManager after reward calculation.
public class EncounterRewardResult
{
    // Reward service returns this result package to BattleManager after reward calculation.
    public int exp;
    public List<LevelUpResult> levelUpResults = new();
    public List<InitialItemStack> itemRewards = new();
}

public static class EncounterRewardService
{
    public static EncounterRewardResult GrantRewards(
        EncounterData encounterData,
        IEnumerable<BaseController> controllers,
        int fallbackExp)
    {
        List<EncounterData> encounterDatas = new List<EncounterData>();
        if (encounterData != null)
            encounterDatas.Add(encounterData);

        return GrantRewards(encounterDatas, controllers, fallbackExp);
    }

    public static EncounterRewardResult GrantRewards(
        IEnumerable<EncounterData> encounterDatas,
        IEnumerable<BaseController> controllers,
        int fallbackExp)
    {
        EncounterRewardResult result = new EncounterRewardResult();

        List<EncounterData> validEncounterDatas = new List<EncounterData>();
        if (encounterDatas != null)
        {
            foreach (EncounterData encounterData in encounterDatas)
            {
                if (encounterData == null)
                    continue;

                validEncounterDatas.Add(encounterData);
            }
        }

        int rewardExp = validEncounterDatas.Count > 0
            ? SumRewardExp(validEncounterDatas)
            : fallbackExp;
        result.exp = Mathf.Max(0, rewardExp);

        // EXP is still awarded before PartyRuntimeState.UpdateFromBattleController,
        // so the result panel and saved party state can see the updated level/EXP.
        result.levelUpResults.AddRange(AwardPartyExp(controllers, result.exp));

        // Roll every configured item drop independently, then write successful drops into runtime inventory.
        foreach (InitialItemStack itemReward in RollItemRewards(validEncounterDatas))
        {
            InventoryRuntimeState.AddItem(itemReward.item, itemReward.count);
            result.itemRewards.Add(itemReward);
        }

        LogRewardResult(result);
        return result;
    }

    private static int SumRewardExp(IEnumerable<EncounterData> encounterDatas)
    {
        int totalExp = 0;

        foreach (EncounterData encounterData in encounterDatas)
        {
            if (encounterData == null)
                continue;

            totalExp += Mathf.Max(0, encounterData.RewardExp);
        }

        return totalExp;
    }

    private static List<LevelUpResult> AwardPartyExp(IEnumerable<BaseController> controllers, int amount)
    {
        List<LevelUpResult> results = new List<LevelUpResult>();

        if (controllers == null || amount <= 0)
            return results;

        // Battle controllers hold the current battle copies of party members.
        // Reward service returns this result package to BattleManager after reward calculation.
        foreach (BaseController controller in controllers)
        {
            if (controller == null || controller.data == null)
                continue;
            if (controller.data.Team != Team.Player)
                continue;

            int beforeLevel = controller.data.Level;
            controller.data.GainExp(amount);
            int afterLevel = controller.data.Level;

            // Reward service returns this result package to BattleManager after reward calculation.
            // so this preserves the existing level-up popup flow.
            results.Add(new LevelUpResult(controller.data.Name, beforeLevel, afterLevel));
        }

        return results;
    }

    private static List<InitialItemStack> RollItemRewards(EncounterData encounterData)
    {
        List<EncounterData> encounterDatas = new List<EncounterData>();
        if (encounterData != null)
            encounterDatas.Add(encounterData);

        return RollItemRewards(encounterDatas);
    }

    private static List<InitialItemStack> RollItemRewards(IEnumerable<EncounterData> encounterDatas)
    {
        List<InitialItemStack> rewards = new List<InitialItemStack>();

        if (encounterDatas == null)
            return rewards;

        foreach (EncounterData encounterData in encounterDatas)
        {
            // No EncounterData means this battle uses fallback enemies, so there are no configured item drops.
            if (encounterData == null || encounterData.ItemDrops == null)
                continue;

            // Each drop row rolls separately. Multiple rows can drop at the same time.
            foreach (EncounterItemDrop drop in encounterData.ItemDrops)
            {
                if (drop == null)
                    continue;
                if (drop.TryRoll(out InitialItemStack reward))
                    rewards.Add(reward);
            }
        }

        return rewards;
    }

    private static void LogRewardResult(EncounterRewardResult result)
    {
        if (result == null)
            return;

        Debug.Log($"[EncounterReward] EXP +{result.exp}, itemDrops={result.itemRewards.Count}");

        foreach (InitialItemStack itemReward in result.itemRewards)
        {
            if (itemReward == null || itemReward.item == null)
                continue;

            Debug.Log($"[EncounterReward] Item +{itemReward.item.itemName} x{itemReward.count}");
        }
    }
}

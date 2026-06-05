using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

public class RewardPopController : BasePanel
{
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private float visibleDuration = 1.2f;

    private Coroutine _playRoutine;

    protected override void Awake()
    {
        base.Awake();
        HideImmediate();
    }

    public bool HasReward(EncounterRewardResult rewardResult)
    {
        if (rewardResult == null)
            return false;

        return rewardResult.exp > 0 || (rewardResult.itemRewards != null && rewardResult.itemRewards.Count > 0);
    }

    public void Play(EncounterRewardResult rewardResult)
    {
        if (!HasReward(rewardResult))
            return;

        if (_playRoutine != null)
            StopCoroutine(_playRoutine);

        _playRoutine = StartCoroutine(PlayRoutine(rewardResult));
    }

    private IEnumerator PlayRoutine(EncounterRewardResult rewardResult)
    {
        if (rewardText != null)
            rewardText.text = BuildRewardText(rewardResult);

        base.Show();
        yield return new WaitForSeconds(fadeDuration + visibleDuration);

        base.Hide();
        yield return new WaitForSeconds(fadeDuration);

        _playRoutine = null;
    }

    public float GetTotalDuration()
    {
        return fadeDuration + visibleDuration + fadeDuration;
    }

    private string BuildRewardText(EncounterRewardResult rewardResult)
    {
        StringBuilder builder = new StringBuilder();

        if (rewardResult.exp > 0)
            builder.AppendLine($"EXP +{rewardResult.exp}");

        if (rewardResult.itemRewards != null)
        {
            foreach (InitialItemStack itemReward in rewardResult.itemRewards)
            {
                if (itemReward == null || itemReward.item == null)
                    continue;

                builder.AppendLine($"{itemReward.item.itemName} x{itemReward.count}");
            }
        }

        return builder.ToString().TrimEnd();
    }
}

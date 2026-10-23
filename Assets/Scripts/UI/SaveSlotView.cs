using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SaveSlotView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private GameObject selectionMark;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text locationText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private TMP_Text actionText;
    [SerializeField] private Image[] portraits;
    [SerializeField] private TMP_Text[] levels;
    private Action<int> selected;
    private int slot;
    private void Awake() { button.onClick.AddListener(Select); }
    private void OnDestroy() { button.onClick.RemoveListener(Select); }
    private void Select() { selected?.Invoke(slot); }

    public void Bind(SaveSlotInfo info, CharacterDataBase characters, bool saving, bool isSelected, Action<int> callback)
    {
        slot = info.Slot;
        selected = callback;
        numberText.text = $"{slot + 1:00}";
        button.interactable = saving || info.CanLoad;
        background.color = isSelected ? new Color(.15f, .25f, .22f) : new Color(.075f, .115f, .105f);
        selectionMark.SetActive(isSelected);
        locationText.text = !info.Exists ? "Empty slot" : !info.CanLoad ? "Unavailable save" : AreaName(info.Data.field.sceneName);
        detailText.text = !info.Exists ? "-" : !info.CanLoad ? info.Error :
            info.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd  HH:mm") +
            (info.UsesBackup ? "   /   Backup" : info.IsLegacy ? "   /   Legacy save" : "");
        actionText.text = saving ? "Save" : info.CanLoad ? "Load" : "-";
        for (int i = 0; i < portraits.Length; i++)
        {
            var members = info.Data?.party?.members;
            var member = members != null && i < members.Count ? members[i] : null;
            Character template = member != null && characters != null && !string.IsNullOrWhiteSpace(member.characterId)
                ? characters.FindById(member.characterId) : null;
            portraits[i].sprite = template?.Portrait;
            portraits[i].enabled = portraits[i].sprite != null;
            levels[i].text = member != null && !string.IsNullOrWhiteSpace(member.characterId) ? $"Lv. {member.level}" : "";
        }
    }

    private static string AreaName(string scene)
    {
        if (scene == "FildScene") return "Volcanic Cave";
        if (scene == "Boss_FieldScene") return "Volcano Arena";
        return string.IsNullOrWhiteSpace(scene) ? "Unknown area" : scene;
    }
}

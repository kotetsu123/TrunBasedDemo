using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillPanelController : MonoBehaviour
{
    public event Action<SkillData> OnSkillSelected;
    public event Action OnCancel;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private SkillData testSkill;

    public bool IsOpen => canvasGroup != null && canvasGroup.alpha > 0.99f;
    private void Awake()
    {
        HideImmediate();
    }
    public void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }
    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
    public  void HideImmediate()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
    public void OnClickSkill()
    {
        OnSkillSelected?.Invoke(testSkill);
    }
    public void OnClickBack()
    {
        OnCancel?.Invoke();
    }
}

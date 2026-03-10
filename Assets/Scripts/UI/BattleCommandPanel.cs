using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleCommandPanel : MonoBehaviour
{
    public event Action<CommandType> OnCommandSelected;

    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        HideImmediate();
    }
    public void Show()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    private void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    public void OnClickAttack()
    {
        OnCommandSelected?.Invoke(CommandType.Attack);
    }
    public void OnClickSkill()
    {
        OnCommandSelected?.Invoke(CommandType.Skill);
    }
    public void OnClickItem()
    {
        OnCommandSelected?.Invoke(CommandType.Item);
    }
    public void OnClickRun()
    {
        OnCommandSelected?.Invoke(CommandType.Run);
    }
   
}

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
        SelectCommand(CommandType.Attack);
    }
    public void OnClickSkill()
    {
        SelectCommand(CommandType.Skill);
    }
    public void OnClickItem()
    {
        SelectCommand(CommandType.Item);
    }
    public void OnClickRun()
    {
        SelectCommand(CommandType.Run);
    }

    private void SelectCommand(CommandType command)
    {
        if (TutorialPanelController.IsTutorialActive)
            return;

        OnCommandSelected?.Invoke(command);
    }
   
}

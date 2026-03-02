using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

public class BattleEndPanelController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BattleManager battle;
    [SerializeField] private CanvasGroup panelGourp;
    [SerializeField] private TMP_Text resultTxt;

    [Header("Tween")]
    [SerializeField] private float panelFadeTime = 0.25f;
    [SerializeField] private float textFadeTime = 0.6f;
    [SerializeField] private float textDelay = 0.2f;

    public event Action OnClosed;

    private Tween _panelTween;
    private Tween _txtTween;

    private void Awake()
    {
        HideImmediate();
    }
    private void OnEnable()
    {
        if (battle != null)
            battle.OnBattleEnded += HandleBattleEnded;
    }
    private void OnDisable()
    {
        if (battle != null)
            battle.OnBattleEnded -= HandleBattleEnded;
    }
    private void HideImmediate()
    {
        if (panelGourp != null)
        {
            panelGourp.alpha = 0f;
            panelGourp.interactable = false;
            panelGourp.blocksRaycasts = false;
        }
        if (resultTxt != null)
        {
            var c = resultTxt.color;
            c.a = 0f;
            resultTxt.color = c;
        }
    }
    private void HandleBattleEnded(BattleResultPayload payload)
    {
        //设置文本
        if (resultTxt != null)
        {
            resultTxt.text = (payload.Result == BattleResult.Win) ? "You Win!" : "You Lose!";
            var c=resultTxt.color;
            c.a = 0f;
            resultTxt.color = c;
        }

        //显示面板
        if (panelGourp != null)
        {
            panelGourp.alpha = 0f;
            panelGourp.interactable = true;
            panelGourp.blocksRaycasts = true;

            _panelTween?.Kill();
            _panelTween = panelGourp.DOFade(1f, panelFadeTime);
        }

        //文本慢慢浮现
        if (resultTxt != null)
        {
            _txtTween?.Kill();
            _txtTween = resultTxt.DOFade(1f, textFadeTime).SetDelay(textDelay);
        }
    }
   
    public void Close()
    {
        panelGourp.alpha = 0f;
        panelGourp.blocksRaycasts= false;
        panelGourp.interactable= false;

        OnClosed?.Invoke();
    }

}


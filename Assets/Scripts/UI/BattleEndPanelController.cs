using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System;
using System.Runtime.CompilerServices;


public class BattleEndPanelController : MonoBehaviour
{
    private enum PanelState { Hidden,Animating,Shown,Closing}

    [Header("Refs")]
    [SerializeField] private BattleManager battle;
    [SerializeField] private CanvasGroup panelGourp;
    [SerializeField] private TMP_Text resultTxt;

    [Header("Tween")]
    [SerializeField] private float panelFadeTime = 0.25f;
    [SerializeField] private float textFadeTime = 0.6f;
    [SerializeField] private float textDelay = 0.2f;

    public event Action<BattleResultPayload> OnClosed;

    private Tween _panelTween;
    private Tween _txtTween;
    private PanelState _state=PanelState.Hidden;

    private BattleResultPayload _lastPayload;

    [SerializeField] private bool clickToSkipAndClose = true;
    [SerializeField] private float closeFadeTime = 0.2f;

    private Tween _closeTween;



    private void Awake()
    {
        HideImmediate();
    }
    private void Update()
    {
        ClickSkipClose();
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
        Debug.Log("[BattleEndPanel] HandleBattleEnded fired");
        _lastPayload =payload;
        _state = PanelState.Animating;
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
            _txtTween = resultTxt.DOFade(1f, textFadeTime)
                .SetDelay(textDelay)
                .OnComplete(() => {
                    if (_state == PanelState.Animating)
                        _state = PanelState.Shown;
                });
        }
    }
   
    public void Close()
    {
        panelGourp.alpha = 0f;
        panelGourp.blocksRaycasts= false;
        panelGourp.interactable= false;

        _panelTween?.Kill();
        _txtTween?.Kill();

        OnClosed?.Invoke(_lastPayload);
    }
    private void ClickSkipClose()
    {
        if (!clickToSkipAndClose) return;
        if (_state == PanelState.Hidden) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (_state == PanelState.Animating)
            {
                FastForward();
            }else if(_state ==PanelState.Shown)
            {
                CloseAnimated();
            }
        }
    }
    private void FastForward()
    {
        //直接把动画推进到最终状态
        _panelTween?.Complete();
        _txtTween?.Complete();

        //确保最终状态正确
        if (panelGourp != null)
        {
            panelGourp.alpha = 1f;
            panelGourp.interactable = true;
            panelGourp.blocksRaycasts = true;
        }
        if (resultTxt != null)
        {
            var c = resultTxt.color;
            c.a = 1f;
            resultTxt.color = c;
        }
        _state = PanelState.Shown;
    }
    private void CloseAnimated()
    {
        if (_state == PanelState.Closing || _state == PanelState.Hidden) return;
        _state=PanelState.Closing;

        _panelTween?.Kill();
        _txtTween?.Kill();
        _closeTween?.Kill();

        if(panelGourp == null)
        {
            CloseImmediate();
            return;
        }

        panelGourp.blocksRaycasts = false;
        panelGourp.interactable=false;

        _closeTween = panelGourp.DOFade(0f, closeFadeTime)
            .OnComplete(() =>
            {_state= PanelState.Hidden;
                OnClosed?.Invoke(_lastPayload);
            });

    }
    public void CloseImmediate()
    {
        _panelTween?.Kill();
        _txtTween?.Kill();
        _closeTween?.Kill();

        if (panelGourp != null)
        {
            panelGourp.alpha = 0f;
            panelGourp.blocksRaycasts=false;
            panelGourp.interactable = false;
        }
        _state=PanelState.Hidden;
        OnClosed?.Invoke(_lastPayload);
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartyRecruitDebugTester : MonoBehaviour
{
    [Header("Recruit Test")]
    [SerializeField] private CharacterDataBase characterDataBase;
    [SerializeField] private string characterId = "Argo_002";

    [Header("Optional UI")]
    [SerializeField] private FieldPartyHudController partyHudController;

    //can be called from the Unity Editor context menu and from the Inspector button
    [ContextMenu("Recruit Character")]

    public void RecruitCharacter()
    {
        if (characterDataBase == null)
        {
            Debug.LogError("CharacterDataBase is not assigned.");
            return;
        }
        // 根据角色 ID，从数据库中取得作为模板的角色数据。
        Character characterTemplate =characterDataBase.FindById(characterId);

        if (characterTemplate == null)
        {
            Debug.LogError($"Character with ID '{characterId}' not found in CharacterDataBase.");
            return;
        }

        //TryRecruitMember 会复制角色数据，并防止同一角色重复入队。
        if(!PartyRuntimeState.TryRecruitMember(characterTemplate,
            out Character recruitedMember))
        {
            Debug.LogWarning($"[PartyRecruitDebug] Failed to recruit. characterId={characterId}");
            return;
        }
        // 更新 Field 场景中的队伍 HUD。
        if(partyHudController==null)
            partyHudController= FindObjectOfType<FieldPartyHudController>();


        partyHudController?.Refresh();

         
        Debug.Log(
            $"[PartyRecruitDebug] Recruited characterId={recruitedMember.characterId}");
    }

}

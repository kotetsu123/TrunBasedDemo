using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Character DataBase")]
public class CharacterDataBase : ScriptableObject
{
    [SerializeField] private PartyInitialData partyInitialData;

    // Save data only stores characterId, so loading needs this lookup to restore Character data.
    public Character FindById(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        if (partyInitialData == null || partyInitialData.Members == null)
        {
            Debug.LogWarning("[CharacterDataBase] PartyInitialData is missing.");
            return null;
        }

        foreach (var character in partyInitialData.Members)
        {
            if (character == null)
                continue;

            if (character.characterId == characterId)
                return character;
        }

        Debug.LogWarning($"[CharacterDataBase] Character not found. characterId={characterId}");
        return null;
    }
}

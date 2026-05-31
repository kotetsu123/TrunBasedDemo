using UnityEngine;
using UnityEngine.SceneManagement;

public class FieldSaveContext : MonoBehaviour
{
    public static FieldSaveContext Current { get; private set; }

    [SerializeField] private Transform player;

    private void Awake()
    {
        Current = this;
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }

    public bool TryFillFieldSaveData(FieldSaveData saveData)
    {
        if (saveData == null || player == null)
            return false;

        // Save the current Field scene name for future multi-map loading.
        saveData.sceneName = SceneManager.GetActiveScene().name;

        // Save the player's current Field position and rotation.
        saveData.playerPos = player.position;
        saveData.playerRotEuler = player.rotation.eulerAngles;
        saveData.hasPlayerTransform = true;

        return true;
    }

    public bool TryApplySavedPlayerTransform()
    {
        if (player == null)
            return false;
        if (!FieldBattleContext.HasSavedPlayerTransform)
            return false;

        // FieldCreator only runs when the scene loads. This lets in-scene Load tests apply the saved position too.
        FieldPlayerTransformUtility.Teleport(
            player,
            FieldBattleContext.SavedPlayerPos,
            FieldBattleContext.SavedPlayerRot);
        FieldBattleContext.ClearSavedPlayerTransform();

        return true;
    }
}

public static class FieldPlayerTransformUtility
{
    public static void Teleport(Transform player, Vector3 position, Quaternion rotation)
    {
        if (player == null)
            return;

        // Field player movement uses Rigidbody, so update both Rigidbody and Transform when teleporting.
        if (player.TryGetComponent(out Rigidbody rb))
        {
            rb.position = position;
            rb.rotation = rotation;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        player.SetPositionAndRotation(position, rotation);
        Physics.SyncTransforms();
    }
}

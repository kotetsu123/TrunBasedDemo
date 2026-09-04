using UnityEngine;

public class FieldSceneTransitionTrigger : MonoBehaviour
{
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnPointId;
    [SerializeField] private SceneTransitionController transitionController;
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    private void Awake()
    {
        if (transitionController == null)
            transitionController = FindObjectOfType<SceneTransitionController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning($"[FieldSceneTransitionTrigger] Target scene name is empty. trigger={name}");
            return;
        }

        if (transitionController == null)
        {
            Debug.LogWarning($"[FieldSceneTransitionTrigger] SceneTransitionController is missing. trigger={name}");
            return;
        }

        hasTriggered = true;
        FieldSceneTransitionContext.SetPendingSpawnPoint(targetSpawnPointId);
        transitionController.StartSceneTransition(targetSceneName);
    }
}

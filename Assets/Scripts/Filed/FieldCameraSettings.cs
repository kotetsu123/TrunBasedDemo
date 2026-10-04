using UnityEngine;

//
[CreateAssetMenu(fileName ="FieldCameraSettings_Default",menuName ="Game/Field Camera Settings")]
public class FieldCameraSettings : ScriptableObject
{
    [Header("Follow")]
    [SerializeField, Min(0.1f)] private float distance = 6f;
    [SerializeField] private float height = 3f;
    [SerializeField,Min(0.01f)]private float smoothTime = 0.05f;

    [Header("Rotation")]
    //初始化角度
    [SerializeField] private float defaultPitch = -12.5f;
    [SerializeField, Min(0f)] private float rotateSpeed=3f;
    [SerializeField]private float minPitch = -15f;
    [SerializeField]private float maxPitch = 60f;

    //对外只提供读取入口，避免被其他类修改
    public float Distance => distance;
    public float Height => height;
    public float SmoothTime => smoothTime;
    public float DefaultPitch => defaultPitch;
    public float RotateSpeed => rotateSpeed;
        
    public float MinPitch => minPitch;
    public float MaxPitch => maxPitch;
}

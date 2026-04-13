using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleCameraAnchor : MonoBehaviour
{
    [SerializeField] private Transform trunCameraPoint;
    [SerializeField] private Transform hitCameraPoint;

    public Transform TrunCameraPoint => trunCameraPoint;
    public Transform HitCameraPoint => hitCameraPoint;
}

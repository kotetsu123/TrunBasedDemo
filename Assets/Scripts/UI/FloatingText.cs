using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float lifeTime = 0.8f;

    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;   
    }
   public void SetUp(string message,Color color)
    {
        if (text != null)
        {
            text.text = message;  
            text.color = color;
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        if (mainCam != null)
        {
            transform.forward = mainCam.transform.forward;
        }
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0)
        {
            Destroy(gameObject);
        }
    }
}

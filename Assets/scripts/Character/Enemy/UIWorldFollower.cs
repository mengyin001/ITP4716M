using UnityEngine;

public class UIWorldFollower : MonoBehaviour
{
    private Transform mainCamera;

    private void Start()
    {
        mainCamera = Camera.main.transform;
    }

    private void LateUpdate()
    {
        // 让UI始终面向摄像机
        transform.LookAt(transform.position + mainCamera.forward);
    }
}
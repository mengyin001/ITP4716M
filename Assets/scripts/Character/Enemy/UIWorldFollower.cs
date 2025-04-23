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
        // ��UIʼ�����������
        transform.LookAt(transform.position + mainCamera.forward);
    }
}
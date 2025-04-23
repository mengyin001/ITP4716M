using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportationCircle : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // ������Ϣ�ҳ���
            SceneManager.LoadScene("SafeHouse");
        }
    }
}
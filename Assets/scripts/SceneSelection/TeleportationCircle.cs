using UnityEngine;

public class TeleportationCircle : MonoBehaviour
{
    public SceneSelectionUI sceneSelectionUI; // �Ϸ� SceneSelectionUI ���

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            sceneSelectionUI.ShowSelectionPanel(); // ��ʾѡ�����
        }
    }
}
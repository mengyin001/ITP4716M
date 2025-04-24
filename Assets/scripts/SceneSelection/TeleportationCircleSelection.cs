using UnityEngine;

public class TeleportationCircleSelection : MonoBehaviour
{
    public SceneSelectionUI sceneSelectionUI; // 拖放 SceneSelectionUI 组件

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            sceneSelectionUI.ShowSelectionPanel(); // 显示选择面板
        }
    }
}
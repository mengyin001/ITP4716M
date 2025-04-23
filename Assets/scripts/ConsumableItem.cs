using UnityEngine;


public class ConsumableItem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float healthRestoreAmount = 10f;
    [SerializeField] private float doubleClickThreshold = 0.3f;
    [SerializeField] private Color selectedColor = Color.yellow;

    private static GameObject selectedItem;
    private float lastClickTime;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    void OnMouseDown()
    {
        if (selectedItem == null)
        {
            SelectItem();
        }
        else if (selectedItem == gameObject)
        {
            if (Time.time - lastClickTime < doubleClickThreshold)
            {
                UseItem();
            }
        }
        else
        {
            DeselectPrevious();
            SelectItem();
        }

        lastClickTime = Time.time;
    }

    void SelectItem()
    {
        selectedItem = gameObject;
        spriteRenderer.color = selectedColor;
    }

    void DeselectPrevious()
    {
        if (selectedItem != null)
        {
            selectedItem.GetComponent<SpriteRenderer>().color = originalColor;
            selectedItem = null;
        }
    }

    void UseItem()
    {
        HealthSystem healthSystem = FindObjectOfType<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.Heal(healthRestoreAmount);
        }

        DeselectPrevious();
        Destroy(gameObject);
    }
}
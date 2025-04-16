using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialTaskSystem : MonoBehaviour
{
    [System.Serializable]
    public class Task
    {
        public string description;      // 任务描述
        public KeyCode triggerKey;      // 触发按键
        public bool showProgressBar;    // 是否显示进度条
        public float requiredHoldTime = 0; //数值代表时间
    }

    [Header("任务设置")]
    [SerializeField] private List<Task> tasks = new List<Task>();
    private int currentTaskIndex = 0;

    [Header("UI组件")]
    [SerializeField] private TextMeshProUGUI taskDescriptionText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private GameObject completedEffect;
    [SerializeField] private TextMeshProUGUI underTip;

    private float keyHoldTimer = 0;

    void Start()
    {
        UpdateTaskDisplay();
        progressSlider.gameObject.SetActive(false);
    }

    void Update()
    {
        if (currentTaskIndex >= tasks.Count) return;

        Task currentTask = tasks[currentTaskIndex];

        // 长按任务检测
        if (currentTask.requiredHoldTime > 0)
        {
            if (Input.GetKey(currentTask.triggerKey))
            {
                keyHoldTimer += Time.deltaTime;
                UpdateProgressBar(keyHoldTimer / currentTask.requiredHoldTime);

                if (keyHoldTimer >= currentTask.requiredHoldTime)
                {
                    CompleteCurrentTask();
                    keyHoldTimer = 0; 
                }
            }
            else
            {
                keyHoldTimer = 0;
                UpdateProgressBar(0);
            }
        }
        // 单击任务检测
        else if (Input.GetKeyDown(currentTask.triggerKey))
        {
            CompleteCurrentTask();
        }
    }

    void CompleteCurrentTask()
    {
        StartCoroutine(ShowCompletionEffect());
        currentTaskIndex++;

        if (currentTaskIndex < tasks.Count)
        {
            UpdateTaskDisplay();
        }
        else
        {
            Debug.Log("Tutorial completed!");
            taskDescriptionText.text = "Tutorial completed!";
            underTip.gameObject.SetActive(false);
            progressSlider.gameObject.SetActive(false);
        }
    }

    void UpdateTaskDisplay()
    {
        Task task = tasks[currentTaskIndex];
        taskDescriptionText.text = task.description;
        underTip.text = task.description;

        if (task.showProgressBar)
        {
            progressSlider.gameObject.SetActive(true);
            progressSlider.value = 0;
        }
        else
        {
            progressSlider.gameObject.SetActive(false);
        }
    }

    void UpdateProgressBar(float progress)
    {
        progressSlider.value = progress;
    }

    System.Collections.IEnumerator ShowCompletionEffect()
    {
        completedEffect.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        completedEffect.SetActive(false);
    }

    // 示例任务数据初始化（可在Inspector中编辑）
    private void Reset()
    {
        tasks = new List<Task>{
            new Task{
                description = "按下 A 键完成任务",
                triggerKey = KeyCode.A,
                showProgressBar = false
            },
            new Task{
                description = "长按 W 键2秒前进",
                triggerKey = KeyCode.W,
                showProgressBar = true
            }
        };
    }
}
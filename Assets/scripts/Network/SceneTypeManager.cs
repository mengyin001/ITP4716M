using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTypeManager
{
    public enum SceneType { SafeHouse, GameLevel }

    public static SceneType CurrentSceneType { get; private set; }

    [RuntimeInitializeOnLoadMethod]
    static void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        DetermineSceneType(SceneManager.GetActiveScene().name);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DetermineSceneType(scene.name);
    }

    private static void DetermineSceneType(string sceneName)
    {
        // 根据场景名称判断场景类型
        CurrentSceneType = sceneName.ToLower().Contains("safehouse") ?
            SceneType.SafeHouse : SceneType.GameLevel;

        Debug.Log($"Scene type set to: {CurrentSceneType} for scene: {sceneName}");
    }
}
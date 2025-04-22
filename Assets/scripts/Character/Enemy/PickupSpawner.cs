using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupSpawner : MonoBehaviour
{
    public PropPrefab[] propPrefabs; 

    public void DropItems()
    {
        foreach (var propPrefab in propPrefabs)
        {
            if (Random.Range(0f, 100f) <= propPrefab.dropPercentage)
            {
                Instantiate(propPrefab.prefab, transform.position, Quaternion.identity);
            }
        }
    }
}

[System.Serializable]
public class PropPrefab
{
    public GameObject prefab; 

    [Range(0f, 100f)] public float dropPercentage; 
}

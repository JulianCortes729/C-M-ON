using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;
    public int poolSize = 20;

    private List<GameObject> pool;

    void Awake()
    {
        pool = new List<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Add(obj);
        }
    }

    public GameObject GetObjectAt(Vector3 position, Quaternion rotation)
    {
        foreach (var obj in pool)
        {
            if (!obj.activeInHierarchy)
            {
                // primero lo movemos A DONDE VA A SPAWNEAR
                obj.transform.position = position;
                obj.transform.rotation = rotation;

                // luego lo activamos (sin moverse desde otro lado)
                obj.SetActive(true);
                return obj;
            }
        }

        // si necesita expandirse el pool
        GameObject extra = Instantiate(prefab);
        extra.transform.position = position;
        extra.transform.rotation = rotation;

        pool.Add(extra);

        extra.SetActive(true);
        return extra;
    }
}

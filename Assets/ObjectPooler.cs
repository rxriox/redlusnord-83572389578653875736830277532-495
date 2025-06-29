using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public static ObjectPooler Instance;

    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }
            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    /// <summary>
    /// Pide un objeto del pool, lo activa y lo devuelve listo para usar.
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("El Pool con el tag " + tag + " no existe.");
            return null;
        }

        if (poolDictionary[tag].Count == 0)
        {
            Debug.LogWarning("El Pool con el tag " + tag + " se ha quedado sin objetos. Considera aumentar su tamaño inicial.");
            // Opcional: podrías crear un objeto nuevo aquí si quieres que el pool sea expandible.
            return null;
        }

        // Saca un objeto de la cola ("pila").
        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        return objectToSpawn;
    }

    /// <summary>
    /// Desactiva un objeto y lo devuelve a la piscina para ser reutilizado.
    /// </summary>
    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("El Pool con el tag " + tag + " no existe. El objeto será destruido.");
            Destroy(objectToReturn);
            return;
        }

        objectToReturn.SetActive(false);
        // Devuelve el objeto a la cola ("pila").
        poolDictionary[tag].Enqueue(objectToReturn);
    }
    
    public void ResetAllPools()
{
    // Recorremos cada tipo de pool que hemos definido (ej. "Proyectil")
    foreach (var pool in pools)
    {
        // Usamos FindGameObjectsWithTag para encontrar todos los objetos activos con ese tag.
        // ¡Esto es crucial! Solo funciona si el prefab del proyectil tiene el tag correcto.
        GameObject[] activeObjects = GameObject.FindGameObjectsWithTag(pool.tag);

        // Forzamos a cada objeto activo encontrado a volver a la piscina.
        foreach (var obj in activeObjects)
        {
            ReturnToPool(pool.tag, obj);
        }
    }
}
}
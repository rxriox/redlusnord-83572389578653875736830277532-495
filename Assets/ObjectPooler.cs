using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    // Clase para configurar cada tipo de objeto que queremos poolear desde el Inspector.
    [System.Serializable]
    public class Pool
    {
        public string tag; // Un nombre para identificar el tipo de objeto (ej. "Proyectil")
        public GameObject prefab; // El prefab que queremos instanciar
        public int size; // Cuántos objetos de este tipo creamos al inicio
    }

    // Singleton para acceder fácilmente al pooler desde cualquier script.
    public static ObjectPooler Instance;

    public List<Pool> pools; // La lista de todos nuestros "almacenes" de objetos.
    // El diccionario que realmente guardará nuestros objetos listos para usar.
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        // Al iniciar el juego, recorremos cada "almacén" que hemos definido...
        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();
            // ...y creamos la cantidad inicial de objetos, dejándolos "dormidos".
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

        // Sacamos un objeto de la "pila".
        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        // Lo activamos, lo posicionamos y lo devolvemos.
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Lo volvemos a poner al final de la cola para tener un ciclo infinito de objetos.
        poolDictionary[tag].Enqueue(objectToSpawn);

        return objectToSpawn;
    }
}
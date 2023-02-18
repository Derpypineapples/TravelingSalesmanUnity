using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class LocationSpawner : MonoBehaviour
{
    [SerializeField] private GameObject locationPref;
    [SerializeField] private LocationConnector connector;

    private List<GameObject> locations = new List<GameObject>();
    private List<GameObject> joins = new List<GameObject>();

    private List<GameObject> objects = new List<GameObject>();

    [Header("Spawner Settings")]
    public int spawnCount;
    public Vector2 spawnSize;

    void Start()
    {
        SpawnLocations();
    }

    public void AddJoin(GameObject join)
    {
        joins.Add(join);
        objects.Add(join);
    }
    public void AddObject(GameObject obj)
    {
        objects.Add(obj);
    }

    public void SpawnLocations()
    {
        if (objects.Count > 0)
        {
            foreach (GameObject o in objects) Destroy(o);

            locations = new List<GameObject>();
            joins = new List<GameObject>();
        }

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = new Vector3(Random.Range(-spawnSize.x / 2, spawnSize.x / 2), Random.Range(-spawnSize.y / 2, spawnSize.y / 2), 0);
            GameObject l = Instantiate(locationPref, spawnPos, Quaternion.identity);
            l.name = i.ToString(); 
            locations.Add(l);
            objects.Add(l);
        }

        connector.ConnectLocations(new List<GameObject>(locations));
    }
}
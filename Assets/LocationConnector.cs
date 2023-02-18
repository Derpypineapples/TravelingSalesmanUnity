using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocationConnector : MonoBehaviour
{
    [SerializeField] private GameObject connectionPref;
    [SerializeField] private GameObject centerPref;
    [SerializeField] private LocationSpawner spawner;

    private List<GameObject> joins = new List<GameObject>();

    private void Connect(GameObject one, GameObject two)
    {
        Debug.Log("Connecting: " + one.name + "-" + two.name);

        one.GetComponent<LocationData>().connections.Add(two);
        two.GetComponent<LocationData>().connections.Add(one);

        float distance = Vector3.Distance(one.transform.position, two.transform.position);
        float angle = Mathf.Rad2Deg * (Mathf.Atan2(one.transform.position.y - two.transform.position.y, one.transform.position.x - two.transform.position.x));
        Vector3 spawnLocation = Vector3.Lerp(one.transform.position, two.transform.position, 0.5f);

        GameObject newJoin = Instantiate(connectionPref, spawnLocation, Quaternion.Euler(0, 0, angle));
        newJoin.transform.localScale = new Vector3(distance, 0.1f, 1);
        newJoin.name = one.name + "-" + two.name;
        spawner.AddJoin(newJoin);
        joins.Add(newJoin);
    }

    //Searches for the closest point and joins them together
    private void GreedySearch(List<GameObject> l)
    {
        GameObject start = l.ElementAt(0);
        GameObject pointer = start;
        List<GameObject> temp = new List<GameObject>(l);
        temp.RemoveAt(0);

        //Loop while more locations can be connected
        while (temp.Count > 0)
        {
            //Temp variables
            GameObject closestLocation = null;
            float closestDistance = float.MaxValue, t;

            //Find closest location to join
            foreach (GameObject local in temp)
            {
                if ((t = Vector3.Distance(local.transform.position, pointer.transform.position)) < closestDistance)
                {
                    closestLocation = local;
                    closestDistance = t;
                }
            }

            //Join closest location and remove previous from list, cant be join with anything else
            Connect(pointer, closestLocation);
            pointer = closestLocation;
            temp.Remove(pointer);
        }
        //Join last location with first location, only one possible
        Connect(pointer, start);
    }
    
    //Cool maybe?
    private void NewSearch(List<GameObject> locations)
    {
        //Find center and add object for clarity
        Vector3 center = Vector3.zero;
        foreach (GameObject l in locations)
            center += l.transform.position;
        center /= locations.Count;
        GameObject centerObject = Instantiate(centerPref, center, Quaternion.identity);
        spawner.AddObject(centerObject);

        //Search for farthest location from center. Save as start & pointer
        float furthest = 0;
        GameObject furthestLocation = null;
        foreach (GameObject l in locations)
        {
            if (Vector3.Distance(l.transform.position, center) > furthest)
            {
                furthest = Vector3.Distance(l.transform.position, center);
                furthestLocation = l;
            }
        }
        GameObject start = furthestLocation;
        GameObject pointer = start;

        //Loop 1: Connect outer most locations
        List<GameObject> tempLocations = new List<GameObject>(locations);
        for (int sData = start.GetComponent<LocationData>().connections.Count; sData < 2; )
        {
            float closest = 2;
            GameObject closestLocation = null;
            Debug.Log("~Pointer: " + pointer.name);

            if (pointer.name != start.name) tempLocations.Remove(pointer);

            string s = "Pointer List: ";
            foreach (GameObject l in pointer.GetComponent<LocationData>().connections)
                s += l.name + " ";
            Debug.Log(s);

            Transform t1 = pointer.transform;
            foreach (GameObject l in tempLocations)
            {
                if (l.GetComponent<LocationData>().connections.Count < 2 && l.name != pointer.name /* && pointer.GetComponent<LocationData>().connections.ElementAt(0).name == l.name*/)
                {
                    Transform t2 = l.transform;

                    float t = Vector3.Dot(Vector3.Normalize(t1.position - t2.position), Vector3.Normalize(t1.position - center));
                    if (t < closest)
                    {
                        closestLocation = l;
                        Debug.Log("New Closest: " + closestLocation.name);
                        closest = t;
                    }
                }
            }

            Connect(pointer, closestLocation);
            if (pointer.name != start.name) tempLocations.Remove(pointer);
            pointer = closestLocation;
            Debug.Log("New Pointer: " + pointer.name);
        }

        //Loop 2: Attempt to join via closest location to any edge until no locations left
    }

    public void ConnectLocations(List<GameObject> l)
    {
        //GreedySearch(l);
        NewSearch(l);
    }
}
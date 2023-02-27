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
        newJoin.GetComponent<JointData>().locationOne = one;
        newJoin.GetComponent<JointData>().locationTwo = two;
        spawner.AddJoin(newJoin);
        joins.Add(newJoin);
    }

    //Disconnect by connections
    private void Disconnect(GameObject one, GameObject two)
    {
        GameObject searchJoin = GameObject.Find(one.name + "-" + two.name);
        if (searchJoin == null) GameObject.Find(two.name + "-" + one.name);
        if (searchJoin == null) return;

        joins.Remove(searchJoin);
        one.GetComponent<LocationData>().connections.Remove(two);
        two.GetComponent<LocationData>().connections.Remove(one);
        Destroy(searchJoin);
    }

    //Disconnect by join
    private void Disconnect(GameObject join)
    {
        GameObject one = GameObject.Find(join.name.Split('-')[0]);
        GameObject two = GameObject.Find(join.name.Split('-')[0]);

        joins.Remove(join);
        one.GetComponent<LocationData>().connections.Remove(two);
        two.GetComponent<LocationData>().connections.Remove(one);
        Destroy(join);
    }
    
    List<GameObject> leftoverLocations = new List<GameObject>();
    public void LoopTwo()
    {
        float intDistance = float.MaxValue;
        GameObject intJoint = null;
        GameObject left = null;

        //Get shortest distances for all locations
        foreach (GameObject l in leftoverLocations)
        {
            Vector3 posLeft = l.transform.position;

            foreach (GameObject j in joins)
            {
                float angle = j.transform.rotation.z;
                Vector3 posOne = j.GetComponent<JointData>().locationOne.transform.position;
                Vector3 posTwo = j.GetComponent<JointData>().locationTwo.transform.position;

                float slope = (posOne.y - posTwo.y) / (posOne.x - posTwo.x);
                float slopei = -1 / slope;

                float b = posTwo.y - (slope * posTwo.x);
                float bi = posLeft.y - (slopei * posLeft.x);

                float x = (bi - b) / (slope + (1 / slope));
                float y = (slope * x) + b;
                Vector3 pos = new Vector3(x, y, 0);

                if (x < posOne.x && x < posTwo.x)
                {
                    if (posOne.x < posTwo.x) { pos = posOne; Debug.Log("Pos = posOne"); }
                    else { pos = posTwo; Debug.Log("Pos = posOne"); }
                }
                else if (x > posOne.x && x > posTwo.x)
                {
                    if (posOne.x > posTwo.x) { pos = posOne; Debug.Log("Pos = posOne"); }
                    else { pos = posTwo; Debug.Log("Pos = posOne"); }
                }

                float dist = Vector3.Distance(l.transform.position, pos);
                if (intDistance > dist)
                {
                    intDistance = dist;
                    intJoint = j;
                    left = l;
                }
            }
        }

        Debug.Log(intJoint.name);
        GameObject joinOne = intJoint.GetComponent<JointData>().locationOne;
        GameObject joinTwo = intJoint.GetComponent<JointData>().locationTwo;

        Disconnect(intJoint);
        Connect(joinOne, left);
        Connect(left, joinTwo);

        leftoverLocations.Remove(left);
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
        leftoverLocations = locations;
        joins.Clear();

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

        //Pointers to important locations
        GameObject start = furthestLocation;
        GameObject pointer = start;
        GameObject previous = null;

        //Loop 0.5
        //Find and connect second point to first via highest angle from center. Init previous variable
        List<GameObject> tempLocations = new List<GameObject>(locations);
        //List<GameObject> leftoverLocations = new List<GameObject>(locations);

        float closestStart = 0;
        GameObject pointerClosest = null;
        Transform pointerTransform = pointer.transform;
        foreach (GameObject l in tempLocations)
        {
            if (l.name != pointer.name)
            {
                Transform t2 = l.transform;

                Vector3 v1 = t2.position - pointerTransform.position;
                Vector3 v2 = center - pointerTransform.position;

                float m1 = Vector3.Magnitude(v1);
                float m2 = Vector3.Magnitude(v2);

                float t = Mathf.Acos(Vector3.Dot(v1, v2) / (m1 * m2));

                if (t > closestStart)
                {
                    pointerClosest = l;
                    closestStart = t;
                }
            }
        }

        Connect(pointer, pointerClosest);
        if (pointer.name != start.name)
        {
            tempLocations.Remove(pointer);
            leftoverLocations.Remove(pointer);
        }
        previous = pointer;
        pointer = pointerClosest;
        //End Loop 0.5

        //Loop 1: Connect outer most locations
        for (int sData = start.GetComponent<LocationData>().connections.Count; sData < 2; sData = start.GetComponent<LocationData>().connections.Count)
        {
            float closest = 0;
            GameObject closestLocation = null;
            tempLocations.Remove(pointer);
            leftoverLocations.Remove(pointer);

            string s = "Pointer List: ";
            foreach (GameObject l in pointer.GetComponent<LocationData>().connections)
                s += l.name + " ";
            Debug.Log(s);

            Vector3 v1 = previous.transform.position - pointer.transform.position;
            foreach (GameObject l in tempLocations)
            {
                if (l.GetComponent<LocationData>().connections.Count < 2 && l.name != pointer.name && (pointer == start ? true : pointer.GetComponent<LocationData>().connections.ElementAt(0).name != l.name))
                {
                    //Debug.Log(l.name + " | " + pointer.name);
                    Vector3 v2 = l.transform.position - pointer.transform.position;

                    float m1 = Vector3.Magnitude(v1);
                    float m2 = Vector3.Magnitude(v2);

                    float t = Mathf.Acos(Vector3.Dot(v1, v2) / (m1 * m2));
                    //Debug.Log(t1.name + " to " + t2.name + " Centered: " + t);

                    if (t > closest)
                    {
                        closestLocation = l;
                        //Debug.Log("New Closest: " + closestLocation.name);
                        closest = t;
                    }
                }
            }

            Connect(pointer, closestLocation);
            if (pointer.name != start.name)
            {
                tempLocations.Remove(pointer);
                leftoverLocations.Remove(pointer);
            }
            previous = pointer;
            pointer = closestLocation;
            //Debug.Log("New Pointer: " + pointer.name);
        }
        leftoverLocations.Remove(start);
        //End Loop 1
        /*
        //Loop 2: Attempt to join via closest location to any edge until no locations left
        while (leftoverLocations.Count > 0)
        //for (int i = 0; i < 2; i++)
        {
            float intDistance = float.MaxValue;
            GameObject intJoint = null;
            GameObject left = null;

            //Get shortest distances for all locations
            foreach (GameObject l in leftoverLocations)
            {
                Vector3 posLeft = l.transform.position;

                foreach (GameObject j in joins)
                {
                    float angle = j.transform.rotation.z;
                    Vector3 posOne = j.GetComponent<JointData>().locationOne.transform.position;
                    Vector3 posTwo = j.GetComponent<JointData>().locationTwo.transform.position;

                    float slope = (posOne.y - posTwo.y) / (posOne.x - posTwo.x);
                    float slopei = -1 / slope;

                    float b = posTwo.y - (slope * posTwo.x);
                    float bi = posLeft.y - (slopei * posLeft.x);

                    float x = (bi - b) / (slope + (1 / slope));
                    float y = (slope * x) + b;
                    Vector3 pos = new Vector3(x, y, 0);
                    
                    if (x < posOne.x && x < posTwo.x)
                    {
                        if (posOne.x < posTwo.x) { pos = posOne; Debug.Log("Pos = posOne"); }
                        else { pos = posTwo; Debug.Log("Pos = posOne"); }
                    }
                    else if (x > posOne.x && x > posTwo.x)
                    {
                        if (posOne.x > posTwo.x) { pos = posOne; Debug.Log("Pos = posOne"); }
                        else { pos = posTwo; Debug.Log("Pos = posOne"); }
                    }

                    float dist = Vector3.Distance(l.transform.position, pos);
                    if (intDistance > dist)
                    {
                        intDistance = dist;
                        intJoint = j;
                        left = l;
                    }
                }
            }

            Debug.Log(intJoint.name);
            GameObject joinOne = intJoint.GetComponent<JointData>().locationOne;
            GameObject joinTwo = intJoint.GetComponent<JointData>().locationTwo;

            Disconnect(intJoint);
            Connect(joinOne, left);
            Connect(left, joinTwo);

            leftoverLocations.Remove(left);
        }
        */
    }

    public void ConnectLocations(List<GameObject> l)
    {
        //GreedySearch(l);
        NewSearch(l);
    }
}
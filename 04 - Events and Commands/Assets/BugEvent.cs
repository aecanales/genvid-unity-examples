using UnityEngine;
using System;
using GenvidSDKCSharp;

public class BugEvent : MonoBehaviour 
{
    public GameObject BugContainer;
    
    private Bug[] bugs;

    void Start()
    {
        bugs = BugContainer.GetComponentsInChildren<Bug>();
    }

    Bug GetBugById(int id)
    {
        foreach(Bug bug in bugs) 
        {
            if (bug.Id == id)
                return bug;
        }

        Debug.LogError($"Could not find bug with ID {id}.");
        return new Bug();
    }
    
    public void OnClickEvent(string eventId, GenvidSDK.EventResult[] results, int numResult, IntPtr userData)
    {
        // We get the ID of the bug the player just clicked on and disable it if active. 
        int bugId = int.Parse (results[0].key.fields[0]);

        Bug clickedBug = GetBugById(bugId);

        if (clickedBug.gameObject.activeSelf)
        {
            clickedBug.gameObject.SetActive(false);
        }
        
        // We won't be using it in this example, but if many player clicked on the same bug in a small period of time,
        // we can use this value to get the amount of clicks.
        int clicks = (int) results[0].values[0].value;
    }   
}
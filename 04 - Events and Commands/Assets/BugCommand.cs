using UnityEngine;
using System;

public class BugCommand : MonoBehaviour 
{
    public GameObject BugContainer;
    
    private Bug[] bugs;

    void Start()
    {
        bugs = BugContainer.GetComponentsInChildren<Bug>();
    }
    
    public void OnCommandRestart(string commandId, string value, IntPtr uniqueId) 
    {
        foreach(Bug bug in bugs)
        {
            bug.gameObject.SetActive(true);
        }
    }    
}
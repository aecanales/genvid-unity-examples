using UnityEngine;
using System;

// Recieves the restart game command that reactivates all bugs.
public class BugCommand : MonoBehaviour 
{
    public GameObject BugContainer;
    
    private Bug[] bugs;

    void Start()
    {
        bugs = BugContainer.GetComponentsInChildren<Bug>();
    }
    
    // Called by the GenvidCommmands game object when a "RestartMatch" command is recieved.
    public void OnCommandRestart(string commandId, string value, IntPtr uniqueId) 
    {
        foreach(Bug bug in bugs)
        {
            bug.gameObject.SetActive(true);
        }
    }    
}
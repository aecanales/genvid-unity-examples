using UnityEngine;
using System;
using GenvidSDKCSharp;

// Recieves the event when the player click on the screen and creates a tree.
public class TreeEvent : MonoBehaviour
{
    public GameObject TreeContainer;
    public GameObject TreePrefab;

    // Called by the GenvidEvents game object when a "click" event is recieved.
    public void OnClickEvent(string eventId, GenvidSDK.EventResult[] results, int numResult, IntPtr userData)
    {
        // We recieve the values in a "x,y" format and must process them.
        string[] values = results[0].key.fields[0].Split(',');

        int x = int.Parse(values[0]);

        // Unity places the Y origin on the bottom of the screen, while the web view places it on the top, so we inverse the value.
        int y = Screen.height - int.Parse(values[1]);

        // We convert screen values to world values to be able to place the tree.
        Vector3 position = Camera.main.ScreenToWorldPoint(new Vector3(x, y));

        // ScreenToWorldPoint will return a Vector3 with the Z position of the camera, but we need it to be at
        // zero to be visible.
        position = new Vector3(position.x, position.y, 0);

        GameObject.Instantiate(TreePrefab, position, Quaternion.identity, TreeContainer.transform);
    }
}
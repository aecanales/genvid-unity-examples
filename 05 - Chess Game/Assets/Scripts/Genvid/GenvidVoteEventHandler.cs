using UnityEngine;

using System;
using System.Collections.Generic;

using GenvidSDKCSharp;

// Recieves the event when a player clicks on a bug and deactivates the corresponding bug.
public class GenvidVoteEventHandler : MonoBehaviour 
{
    // Holds the votes for the current move in a <move, votes> format.
    private Dictionary<string, int> votes = new Dictionary<string, int>();

    public void ResetVoteCount()
    {
        votes = new Dictionary<string, int>();
    }

    public string GetMostVotedMove()
    {
        string move = "";
        int value = -1;
        
        foreach(KeyValuePair<string, int> entry in votes)
        {
            if (entry.Value > value)
            {
                move = entry.Key;
                value = entry.Value;
            }
        }

        return move;
    }
    
    public void OnVoteEvent(string eventId, GenvidSDK.EventResult[] results, int numResult, IntPtr userData)
    {
        string move = results[0].key.fields[0];

        if (votes.ContainsKey(move))
        {
            votes[move] += (int) results[0].values[0].value;
        }
        else
        {
            votes[move] = (int) results[0].values[0].value;
        }
    }
}
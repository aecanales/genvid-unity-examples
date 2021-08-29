using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using GenvidSDKCSharp;
using System.Collections.Generic;
using UnityEngine.Events;

public class GenvidCommands : MonoBehaviour
{
    [Serializable]
    public class CommandEvent : UnityEvent<string, string, IntPtr>
    {
    }

    [Serializable]
    public class CommandElement
    {
        public string Id;
        public CommandEvent OnCommandTriggered;
    }

    private class CommandDataFunction
    {
        public CommandEvent Callback;
        public GenvidSDK.CommandResult Result;
        public IntPtr UserData;
    }

    public CommandElement[] Commands;

    // Variables excluded to prevent warnings
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
    private GenvidSDK.CommandCallback m_CommandCallback = null;
    private Dictionary<string, IntPtr> m_CommandData = null;
    private Stack<CommandDataFunction> m_CommandPool = null;
    private bool m_IsCreated = false;
#endif

    public void Create()
    {
    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (GenvidSessionManager.Instance.ActivateSDK && !m_IsCreated && GenvidSessionManager.IsInitialized)
        {
            m_CommandData = new Dictionary<string, IntPtr>();
            m_CommandPool = new Stack<CommandDataFunction>();
            m_CommandCallback = new GenvidSDK.CommandCallback(CommandCallbackFunction);

            foreach (var cmd in Commands)
            {
                var userData = new IntPtr(m_CommandData.Count);
                var status = GenvidSDK.SubscribeCommand(cmd.Id, m_CommandCallback, userData);
                if (GenvidSDK.StatusFailed(status))
                {
                    Debug.LogError("Error while Subscribing the " + cmd + " command: " + GenvidSDK.StatusToString(status));
                }
                else
                {
                    if (GenvidSessionManager.Instance.ActivateDebugLog)
                    {
                        Debug.Log("Genvid Subscribing command named " + cmd.Id + " performed correctly.");
                    }
                    m_CommandData.Add(cmd.Id, userData);
                }
            }
            m_IsCreated = true;
        }
    #endif
    }

    public void Destroy()
    {
    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (GenvidSessionManager.Instance.ActivateSDK && m_IsCreated)
        {
            foreach (var cmd in m_CommandData)
            {
                var status = GenvidSDK.UnsubscribeCommand(cmd.Key, m_CommandCallback, cmd.Value);
                if (GenvidSDK.StatusFailed(status))
                {
                    Debug.LogError("Error while unsubscribing the " + cmd.Key + " command: " + GenvidSDK.StatusToString(status));
                }
                else if (GenvidSessionManager.Instance.ActivateDebugLog)
                {
                    Debug.Log("Genvid Unsubscribing command named " + cmd.Key + " performed correctly.");
                }
            }

            m_CommandData.Clear();
            m_CommandPool.Clear();
            m_CommandPool = null;
            m_CommandData = null;
            m_IsCreated = false;
        }
    #endif
    }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
    void CommandCallbackFunction(GenvidSDK.CommandResult commandResult, IntPtr userData)
    {
        if (m_CommandPool == null)
        {
            Debug.Log("Command '" + commandResult.id + "' has been ignored because the Command Pool is null: ");
        }

        foreach (var cmd in Commands)
        {
            if (cmd.Id == commandResult.id)
            {
                if(cmd.OnCommandTriggered != null)
                {
                    try
                    {
                        CommandDataFunction dataEvent = new CommandDataFunction();
                        dataEvent.Callback = cmd.OnCommandTriggered;
                        dataEvent.Result = commandResult;
                        dataEvent.UserData = userData;
                        m_CommandPool.Push(dataEvent);
                    }
                    catch (OutOfMemoryException ex)
                    {
                        Debug.LogError(ex.Message);
                    }
                }
                break;
            }
        }
    }

    private void FixedUpdate()
    {
        if(m_CommandPool != null && m_IsCreated)
        {
            while (m_CommandPool.Count > 0)
            {
                var dataEvent = m_CommandPool.Pop();
                dataEvent.Callback.Invoke(dataEvent.Result.id, dataEvent.Result.value, dataEvent.UserData);
            }
        }
    }
#endif
}

using GenvidSDKCSharp;
using UnityEngine;
using System;
using System.Collections;
using System.Text;

public abstract class GenvidStreamBase : MonoBehaviour
{
    public static GenvidSDK.Status SetParameter(object streamID, string paramKey, int paramValue)
    {
        return GenvidSDK.SetParameter(streamID, paramKey, paramValue);
    }

    public static GenvidSDK.Status SetParameter(object streamID, string paramKey, float paramValue)
    {
        return GenvidSDK.SetParameter(streamID, paramKey, paramValue);
    }
	
	public float GetFrameRate(String streamName)
	{
		float floatParamReceived = 0.0f;
		var gvStatus = GenvidSDK.GetParameter(streamName, "framerate", ref floatParamReceived);
		if (GenvidSDK.StatusFailed(gvStatus))
        {
			Debug.LogError("Error while setting the float parameter for " + streamName + " : " + GenvidSDK.StatusToString(gvStatus));
			return float.NaN;
		}
        else if (GenvidSessionManager.Instance.ActivateDebugLog)
        {
            Debug.Log("Genvid Get Frame rate performed correctly.");
        }
        return floatParamReceived;
	}

	public bool SetFrameRate(String streamName, float framerate)
	{
		var gvStatus = GenvidSDK.SetParameter(streamName, "framerate", framerate);
		if (GenvidSDK.StatusFailed(gvStatus))
        {
			Debug.LogError("Error while setting the float parameter for " + streamName + " : " + GenvidSDK.StatusToString(gvStatus));
			return false;
		}
        else if (GenvidSessionManager.Instance.ActivateDebugLog)
        {
            Debug.Log("Genvid Set Framerate performed correctly.");
        }
        return true;
	}

    public float GetGranularity(String streamName)
    {
        float floatParamReceived = 0.0f;
        var gvStatus = GenvidSDK.GetParameter(streamName, "granularity", ref floatParamReceived);
        if (GenvidSDK.StatusFailed(gvStatus))
        {
            Debug.LogError("Error while getting the float parameter granularity for " + streamName + " : " + GenvidSDK.StatusToString(gvStatus));
            return float.NaN;
        }
        else if (GenvidSessionManager.Instance.ActivateDebugLog)
        {
            Debug.Log("Genvid Get Frame rate performed correctly.");
        }
        return floatParamReceived;
    }

    public bool SetGranularity(String streamName, float granularity)
    {
        var gvStatus = GenvidSDK.SetParameter(streamName, "granularity", granularity);
        if (GenvidSDK.StatusFailed(gvStatus))
        {
            Debug.LogError("Error while setting the float parameter granularity for " + streamName + " : " + GenvidSDK.StatusToString(gvStatus));
            return false;
        }
        else if (GenvidSessionManager.Instance.ActivateDebugLog)
        {
            Debug.Log("Genvid Set Granularity performed correctly.");
        }
        return true;
    }

    public void Create()
    {

    }

    public void Destroy()
    {

    }
}

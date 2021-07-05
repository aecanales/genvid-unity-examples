using UnityEngine;
using System;
using GenvidSDKCSharp;
using System.Collections;

public class GenvidAudio : GenvidStreamBase
{
    public enum AudioMode
    {
        None,
        WASAPI,
        Unity
    }

    [SerializeField]
    string m_StreamName;
    [SerializeField]
    GenvidSDK.AudioFormat m_AudioFormat = GenvidSDK.AudioFormat.S16LE;
    [SerializeField]
    AudioMode m_AudioMode = AudioMode.Unity;

	int m_outputRate;
    AudioListener m_AudioListener;
    AudioStreamFilter m_AudioStreamFilter;

    private bool m_ChannelNumberSet = false;

    public string StreamName
    {
        get { return m_StreamName; }
        private set { m_StreamName = value; }
    }

    public GenvidSDK.AudioFormat AudioFormat
    {
        get
        {
            int paramReceived = 0;
            var gvStatus = GenvidSDK.GetParameter(m_StreamName, "audio.format", ref paramReceived);
            if (GenvidSDK.StatusFailed(gvStatus))
            {
                Debug.LogError("Error while trying to get the audio format parameter : " + GenvidSDK.StatusToString(gvStatus));
                return GenvidSDK.AudioFormat.Unknown;
            }
            else if (GenvidSessionManager.Instance.ActivateDebugLog)
            {
                Debug.Log("Genvid Get Parameter audio format performed correctly.");
            }
            return (GenvidSDK.AudioFormat)paramReceived;
        }
        set
        {
            var gvStatus = GenvidSDK.SetParameter(m_StreamName, "audio.format", (int)value);
            if (GenvidSDK.StatusFailed(gvStatus))
            {
                Debug.LogError("Error while trying to set the audio format parameter : " + GenvidSDK.StatusToString(gvStatus));
            }
            else if (GenvidSessionManager.Instance.ActivateDebugLog)
            {
                Debug.Log("Genvid Set Parameter audio format performed correctly.");
            }
        }
    }

    public int AudioChannels
    {
        get
        {
            int paramReceived = 0;
            var gvStatus = GenvidSDK.GetParameter(m_StreamName, "audio.channels", ref paramReceived);
            if (GenvidSDK.StatusFailed(gvStatus))
            {
                Debug.LogError("Error while trying to get the audio channels parameter : " + GenvidSDK.StatusToString(gvStatus));
                return -1;
            }
            else if (GenvidSessionManager.Instance.ActivateDebugLog)
            {
                Debug.Log("Genvid Get Parameter audio channel performed correctly.");
            }
            return paramReceived;
        }
        set
        {
            var gvStatus = GenvidSDK.SetParameter(m_StreamName, "audio.channels", value);
            if (GenvidSDK.StatusFailed(gvStatus))
            {
                Debug.LogError("Error while trying to set the audio channels parameter : " + GenvidSDK.StatusToString(gvStatus));
            }
            else if (GenvidSessionManager.Instance.ActivateDebugLog)
            {
                Debug.Log("Genvid Set Parameter audio channel performed correctly.");
            }
        }
    }

    public int AudioRate
    {
        get
        {
            int paramReceived = 0;
            var gvStatus = GenvidSDK.GetParameter(m_StreamName, "audio.rate", ref paramReceived);
            if (GenvidSDK.StatusFailed(gvStatus))
            {
                Debug.LogError("Error while trying to get the audio rate parameter : " + GenvidSDK.StatusToString(gvStatus));
                return -1;
            }
            else if (GenvidSessionManager.Instance.ActivateDebugLog)
            {
                Debug.Log("Genvid Get Parameter audio rate performed correctly.");
            }
            return paramReceived;
        }
        set
        {
            var gvStatus = GenvidSDK.SetParameter(m_StreamName, "audio.rate", value);
            if (GenvidSDK.StatusFailed(gvStatus))
            {
                Debug.LogError("Error while trying to set the audio rate parameter : " + GenvidSDK.StatusToString(gvStatus));
            }
            else if (GenvidSessionManager.Instance.ActivateDebugLog)
            {
                Debug.Log("Genvid Set Parameter audio rate performed correctly.");
            }
        }
    }

    public int AudioGranularity
    {
        get
        {
            int paramReceived = 0;
            var gvStatus = GenvidSDK.GetParameter(m_StreamName, "granularity", ref paramReceived);
            if (GenvidSDK.StatusFailed(gvStatus))
            {
                Debug.LogError("Error while trying to get the audio granularity parameter : " + GenvidSDK.StatusToString(gvStatus));
                return -1;
            }
            else if (GenvidSessionManager.Instance.ActivateDebugLog)
            {
                Debug.Log("Genvid Get Parameter audio granularity performed correctly.");
            }
            return paramReceived;
        }
        set
        {
            var gvStatus = GenvidSDK.SetParameter(m_StreamName, "granularity", value);
            if (GenvidSDK.StatusFailed(gvStatus))
            {
                Debug.LogError("Error while trying to set the audio granularity parameter : " + GenvidSDK.StatusToString(gvStatus));
            }
            else if (GenvidSessionManager.Instance.ActivateDebugLog)
            {
                Debug.Log("Genvid Set Parameter audio granularity performed correctly.");
            }
        }
    }

    public new bool Create()
    {
    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (GenvidSessionManager.Instance.ActivateSDK && GenvidSessionManager.IsInitialized)
        {
            var status = GenvidSDK.CreateStream(m_StreamName);
            if (GenvidSDK.StatusFailed(status))
            {
                Debug.LogError("Error while creating the " + m_StreamName + " stream: " + GenvidSDK.StatusToString(status));
                return false;
            }
            else if (GenvidSessionManager.Instance.ActivateDebugLog)
            {
                Debug.Log("Genvid create audio stream named " + m_StreamName + " performed correctly.");
            }

            var config = AudioSettings.GetConfiguration();
            m_outputRate = config.sampleRate;

            if (m_AudioMode == AudioMode.WASAPI)
			{
                if(GenvidSessionManager.Instance.Session.VideoStream == null)
                {
                    Debug.LogError("Error while Accessing Video framerate from " + m_StreamName + " stream.");
                    return false;
                }

                // Set the frame rate before using wasapi.
                SetFrameRate(m_StreamName, GenvidSessionManager.Instance.Session.VideoStream.Framerate);

                status = GenvidSDK.SetParameter(m_StreamName, "Audio.Source.WASAPI", 1);
				if (GenvidSDK.StatusFailed(status))
                {
					Debug.LogError("Error while setting the parameter for the audio : " + GenvidSDK.StatusToString(status));
                    return false;
				}
                else if (GenvidSessionManager.Instance.ActivateDebugLog)
                {
                    Debug.Log("Genvid Set Parameter Wasapi performed correctly.");
                }
            }
            else if (m_AudioMode == AudioMode.Unity)
            {
                m_AudioListener = FindObjectOfType<AudioListener>();
                if (m_AudioListener == null)
                {
                    Debug.LogWarning("No AudioListener was found. Adding AudioListener...");
                    m_AudioListener = gameObject.AddComponent<AudioListener>();
                }

                m_AudioStreamFilter = m_AudioListener.GetComponent<AudioStreamFilter>();
                if (m_AudioStreamFilter == null)
                {
                    m_AudioStreamFilter = m_AudioListener.gameObject.AddComponent<AudioStreamFilter>();
                }

                AudioRate = m_outputRate;
                AudioFormat = m_AudioFormat;

                m_AudioStreamFilter.OnAudioReceivedDataCallback += OnAudioReceivedDataCallback;
            }
		}
    #endif
        return true;
    }

	public new void Destroy()
    {
    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if(GenvidSessionManager.Instance.ActivateSDK)
        {
            if (m_AudioStreamFilter != null)
            {
                m_AudioStreamFilter.OnAudioReceivedDataCallback -= OnAudioReceivedDataCallback;
            }

            var status = GenvidSDK.DestroyStream(m_StreamName);
            if (GenvidSDK.StatusFailed(status))
            {
               Debug.LogError("Error while destroying the " + m_StreamName + " stream: " + GenvidSDK.StatusToString(status));
            }
            else if (GenvidSessionManager.Instance.ActivateDebugLog)
            {
               Debug.Log("Genvid Destroy audio stream named " + m_StreamName + " performed correctly.");
            }
        }
    #endif
    }

    private void OnAudioReceivedDataCallback(float[] data, int channels)
    {
        AudioFilterRead(data, channels);
    }

    void AudioFilterRead(float[] data, int channels)
    {
        if (!m_ChannelNumberSet)
        {
            AudioChannels = channels;
            m_ChannelNumberSet = true;
        }

        if (m_AudioMode == AudioMode.Unity)
        {
            long tc = GenvidSDK.GetCurrentTimecode();

            if (m_AudioFormat == GenvidSDK.AudioFormat.S16LE)
            {
                var status = GenvidSDK.SubmitAudioData(tc, m_StreamName, ConvertTo16Bits(data));
                if (GenvidSDK.StatusFailed(status))
                {
                    Debug.LogError("Error while submitting the audio data to the " + m_StreamName + " stream: " + GenvidSDK.StatusToString(status));
                }
                else if (GenvidSessionManager.Instance.ActivateDebugLog)
                {
                    Debug.Log("Genvid Submit audio data performed correctly.");
                }
            }
            else if (m_AudioFormat == GenvidSDK.AudioFormat.F32LE)
            {
                var status = GenvidSDK.SubmitAudioData(tc, m_StreamName, data);
                if (GenvidSDK.StatusFailed(status))
                {
                    Debug.LogError("Error while submitting the audio data to the " + m_StreamName + " stream: " + GenvidSDK.StatusToString(status));
                }
                else if (GenvidSessionManager.Instance.ActivateDebugLog)
                {
                    Debug.Log("Genvid Submit audio data performed correctly.");
                }
            }
            else
            {
                Debug.LogError("Unknown Audio format.");
            }
        }
    }

    short[] ConvertTo16Bits(float[] dataSource)
	{
		short[] dataDest = new short[dataSource.Length];

		for (int i = 0; i < dataSource.Length; i++)
		{
			int value = (int)(dataSource[i] * 32768.0f);
			// Clamp the value to avoid audio glitch
			dataDest[i] = (short)Math.Min(Math.Max(value, -32768), 32767);
		}

		return dataDest;
	}
}

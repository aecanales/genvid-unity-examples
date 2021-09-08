using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using GenvidSDKCSharp;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class GenvidVideo : GenvidStreamBase
{
    // GENVID - Start DLL import
    [DllImport("GenvidPlugin")]
    private static extern GenvidSDK.Status GetVideoInitStatus();
 
    [DllImport("GenvidPlugin")]
    private static extern GenvidSDK.Status GetVideoSubmitDataStatus();

    [DllImport("GenvidPlugin")]
    public static extern void SetupVideoChannel(string streamID);

    [DllImport("GenvidPlugin")]
    public static extern void CleanUp();

    [DllImport("GenvidPlugin")]
    public static extern IntPtr GetRenderEventFunc();
    // GENVID - Stop DLL import

    public enum eCaptureType
	{
		Automatic,
		Texture
	}

	[SerializeField]
    [Tooltip("Video Stream Name")]
    private string m_StreamName;

    [SerializeField]
    [Range(30.0f, 60.0f)]
    [Tooltip("Video Stream framerate")]
    private float m_Framerate = 30.0f;

    [SerializeField]
    [Tooltip("Video Capture Type")]
    private eCaptureType m_CaptureType;

	[SerializeField]
    [Tooltip("Video Source (Texture or Camera)")]
    private UnityEngine.Object m_VideoSource;
	
	public string StreamName
	{
        get { return m_StreamName; }
        private set { m_StreamName = value; }
	}

	public float Framerate
	{
        get { return m_Framerate; }
        private set { m_Framerate = value; }
	}
	
	public eCaptureType CaptureType
	{
		get { return m_CaptureType;  }
		set { m_CaptureType = value; }
	}

	public UnityEngine.Object VideoSource
	{
		get { return m_VideoSource;  }
		set { m_VideoSource = value; }
	}

	public bool IsCreated	
	{
		get { return m_IsCreated; }
        private set { m_IsCreated = value; }
    }

	private UnityEngine.Object m_CurrentVideoSource;
	private RenderTexture m_RenderTexture;
	private IntPtr m_TexturePtr;

	private bool m_TerminateCoroutine = false;
	private bool m_ProcessComplete = true;
	private bool m_QuitProcess = false;
	private bool m_IsCreated = false;
    
    private CommandBuffer m_commandBuffer;
    private const string commandBufferName = "MultipleCapture";

	// GENVID - On start begin
	public new bool Create()
	{
    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
		if(GenvidSessionManager.Instance.ActivateSDK && !m_IsCreated && GenvidSessionManager.IsInitialized)
		{
            var status = GenvidSDK.CreateStream(m_StreamName);
            if (GenvidSDK.StatusFailed(status))
            {
				Debug.LogError("Error while creating the " + m_StreamName + " stream: " + GenvidSDK.StatusToString(status));
				return false;
			}
            else if (GenvidSessionManager.Instance.ActivateDebugLog)
            {
                Debug.Log("Genvid Create video stream named " + m_StreamName + " performed correctly.");
            }

            int width = 0;
            int height = 0;
            status = GenvidSDK.GetParameter(m_StreamName, "genvid/encode/input/width", ref width);
            if (status == GenvidSDK.Status.Success)
            {
                status = GenvidSDK.GetParameter(m_StreamName, "genvid/encode/input/height", ref height);
                if (status == GenvidSDK.Status.Success)
                {
                    // Force Resolution
                    Screen.SetResolution(width, height, false);
                }
            }

            // Force windowed mode
            if (Screen.fullScreen)
            {
                Screen.fullScreen = false;
            }

            SetFrameRate(m_StreamName, Framerate);            			
            SetupVideoChannel(m_StreamName);
            StartCoroutine(CallPluginAtEndOfFrames());
            m_IsCreated = true;
		}
    #endif
        return true;
    }
	// GENVID - On start end

	private void CleanupResources()
	{
    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
		var status = GenvidSDK.DestroyStream(m_StreamName);
		if (GenvidSDK.StatusFailed(status))
        {
			Debug.LogError("Error while destroying the " + m_StreamName + " stream: " + GenvidSDK.StatusToString(status));
		}
        else if (GenvidSessionManager.Instance.ActivateDebugLog)
        {
            Debug.Log("Genvid Destroy video stream named " + m_StreamName + " performed correctly.");
        }

        if (GetCameraFromSource(m_CurrentVideoSource) != null)
        {
            CleanupCameraCommandBuffer(m_CurrentVideoSource);

            if(m_commandBuffer != null)
            {
                m_commandBuffer.Clear();
            }
            DestroyTexture();
        }

        CleanUp();
		m_IsCreated = false;
    #endif
	}

    public new void Destroy()
    {
        if (GenvidSessionManager.Instance.ActivateSDK && m_IsCreated)
        {
            m_QuitProcess = true;
            StartCoroutine(DestroyVideo());
        }
    }


    private IEnumerator DestroyVideo()
	{
		if (m_IsCreated)
        {
            // Wait only if the application does not quit.
            if (!GenvidSessionManager.IsDestroying)
            {
                yield return new WaitUntil(() => m_TerminateCoroutine == true);
            }
            CleanupResources();
        }
	}

	private void DestroyTexture()
	{
		if(m_RenderTexture != null)
		{
			m_RenderTexture.Release();
			Destroy(m_RenderTexture);
		}
	}

	private void CaptureCamera(Camera camera)
	{
		DestroyTexture();

        if (camera.targetTexture != null)
        {
            m_TexturePtr = camera.targetTexture.GetNativeTexturePtr();

            var gvStatus = GenvidSDK.SetParameter(m_StreamName, "video.useopenglconvention", 1);
            if (GenvidSDK.StatusFailed(gvStatus))
            {
                Debug.LogError("Error while setting the opengl convention: " + GenvidSDK.StatusToString(gvStatus));
            }
            else if (GenvidSessionManager.Instance.ActivateDebugLog)
            {
                Debug.Log("Genvid Set Parameter openGL performed correctly.");
            }
            Debug.Log("Your camera is using a render texture, we are using it instead of your camera for your video stream.");
        }
        else
        {
            m_RenderTexture = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24, RenderTextureFormat.ARGB32);
            if (m_RenderTexture.Create())
            {
                m_TexturePtr = m_RenderTexture.GetNativeTexturePtr();

                m_commandBuffer = new CommandBuffer();
                m_commandBuffer.name = commandBufferName;
                camera.AddCommandBuffer(CameraEvent.AfterEverything, m_commandBuffer);
                m_commandBuffer.Blit(BuiltinRenderTextureType.CurrentActive, m_RenderTexture);
            }
            else
            {
                Debug.LogError("Failed to create the Render Texture.");
            }
        }
	}

	// GENVID - Video capture start
	private IEnumerator CallPluginAtEndOfFrames()
	{
		// GetRenderEventFunc param
		System.IntPtr renderingFunction = GetRenderEventFunc();
		var waitForEndOfFrame = new WaitForEndOfFrame();
		GenvidSDK.Status status = GenvidSDK.Status.Success;

		while (true)
		{
			// Wait until all frame rendering is done
			yield return waitForEndOfFrame;

			if(m_QuitProcess == false)
			{ 
				if (m_ProcessComplete)
				{
					if(OnRenderEventInit())
					{
                        if (m_CaptureType != eCaptureType.Automatic)
                        {
                            status = GenvidSDK.SetParameterPointer(m_StreamName, "video.source.id3d11texture2d", m_TexturePtr);
                            if (GenvidSDK.StatusFailed(status))
                            {
                                Debug.LogError("Error while initializing texture for capture: " + GenvidSDK.StatusToString(status));
                                if (status == GenvidSDK.Status.InvalidParameter)
                                {
                                    Debug.LogError("Please ensure you are using a D3D11 graphic driver.");
                                }
                            }
                            else if (GenvidSessionManager.Instance.ActivateDebugLog)
                            {
                                Debug.Log("Genvid Set Parameter Pointer for texture2d performed correctly.");
                            }
                        }
                        else
                        {
                            GL.IssuePluginEvent(renderingFunction, 0);
                            status = GetVideoInitStatus();
                            if (GenvidSDK.StatusFailed(status))
                            {
                                Debug.LogError("Error while starting the video stream : " + GenvidSDK.StatusToString(status));
                                if (status == GenvidSDK.Status.InvalidParameter)
                                {
                                    Debug.LogError("Please ensure you are using a D3D11 graphic driver.");
                                }
                            }
                            else if (GenvidSessionManager.Instance.ActivateDebugLog)
                            {
                                Debug.Log("Genvid GetVideoInitStatus performed correctly.");
                            }
                            status = GenvidSDK.Status.ConnectionInProgress;
                        }
					}
					else
					{
						GL.IssuePluginEvent(renderingFunction, 0);
						status = GetVideoInitStatus();
						if (GenvidSDK.StatusFailed(status))
                        {
							Debug.LogError("Error while starting the video stream : " + GenvidSDK.StatusToString(status));
							if (status == GenvidSDK.Status.InvalidParameter)
							{
								Debug.LogError("Please ensure you are using a D3D11 graphic driver.");
							}
						}
                        else if (GenvidSessionManager.Instance.ActivateDebugLog)
                        {
                            Debug.Log("Genvid GetVideoInitStatus performed correctly.");
                        }
                        status = GenvidSDK.Status.ConnectionInProgress;
					}
					m_ProcessComplete = false;
				}
				else
				{
					if(OnRenderEventUpdate())
					{
                        if (m_CaptureType != eCaptureType.Automatic)
                        {
                            status = GenvidSDK.SetParameterPointer(m_StreamName, "video.source.id3d11texture2d", m_TexturePtr);
                            if (GenvidSDK.StatusFailed(status))
                            {
                                Debug.LogError("Error while assigning new texture for capture: " + GenvidSDK.StatusToString(status));
                                if (status == GenvidSDK.Status.InvalidParameter)
                                {
                                    Debug.LogError("Please ensure you are using D3D11 graphic driver.");
                                }
                            }
                            else if (GenvidSessionManager.Instance.ActivateDebugLog)
                            {
                                Debug.Log("Genvid Set Parameter Pointer for texture2d performed correctly.");
                            }
                        }
					}
					GL.IssuePluginEvent(renderingFunction, 1);
					status = GetVideoSubmitDataStatus();
					if (GenvidSDK.StatusFailed(status))
                    {
						Debug.LogError("Error while sending video data : " + GenvidSDK.StatusToString(status));
					}
                    else if (GenvidSessionManager.Instance.ActivateDebugLog)
                    {
                        Debug.Log("Genvid GetVideoSubmitDataStatus performed correctly.");
                    }
                }
			}
			else
			{
				m_TerminateCoroutine = true;
				yield break;
			}
		}
	}
	// GENVID - Video capture end

	bool OnRenderEventInit()
	{
		if (m_CurrentVideoSource != m_VideoSource)
		{
			UpdateCaptureType();
			return true;
		}
		return false;
	}

	bool OnRenderEventUpdate()
	{
		if(m_CurrentVideoSource != m_VideoSource)
		{
			UpdateCaptureType();
			return true;
		}
		return false;
	}

    private Camera GetCameraFromSource(UnityEngine.Object Source)
    {
        Camera camera = null;

        if (!(Source is Camera))
        {
            var go = Source as GameObject;
            if (go != null)
            {
                camera = go.GetComponent<Camera>();
            }
        }
        else
        {
            camera = (Camera)Source;
        }
        return camera;
    }

    private void CleanupCameraCommandBuffer(UnityEngine.Object Source)
	{
		if(Source != null)
		{
			Camera camera = GetCameraFromSource(Source);

            var listCommandBuffers = camera.GetCommandBuffers(CameraEvent.AfterEverything);
            var findBuffer = false;

            foreach (CommandBuffer x in listCommandBuffers)
            {
                if (x.name.Equals(commandBufferName))
                {
                    findBuffer = true;
                    break;
                }
            }

            if (findBuffer)
            {
                camera.RemoveCommandBuffer(CameraEvent.AfterEverything, m_commandBuffer);
            }
		}
	}

	void UpdateCaptureType()
	{
		var oldVideoSource = m_CurrentVideoSource;
		m_CurrentVideoSource = m_VideoSource;

		if(m_CaptureType != eCaptureType.Automatic)
		{
            if (GetCameraFromSource(oldVideoSource) != null)
            {
                CleanupCameraCommandBuffer(oldVideoSource);
            }

            if (m_CaptureType == eCaptureType.Texture)
            {
                if (m_CurrentVideoSource is Texture)
                {
                    m_TexturePtr = ((Texture)m_CurrentVideoSource).GetNativeTexturePtr();

                    var gvStatus = GenvidSDK.SetParameter(m_StreamName, "video.useopenglconvention", 1);
                    if (GenvidSDK.StatusFailed(gvStatus))
                    {
                        Debug.LogError("Error while setting the opengl convention: " + GenvidSDK.StatusToString(gvStatus));
                    }
                    else if (GenvidSessionManager.Instance.ActivateDebugLog)
                    {
                        Debug.Log("Genvid Set Parameter useOpenGL performed correctly.");
                    }
                }
                else
                {
                    var gvStatus = GenvidSDK.SetParameter(m_StreamName, "video.useopenglconvention", 0);
                    if (GenvidSDK.StatusFailed(gvStatus))
                    {
                        Debug.LogError("Error while setting the opengl convention: " + GenvidSDK.StatusToString(gvStatus));
                    }
                    else if (GenvidSessionManager.Instance.ActivateDebugLog)
                    {
                        Debug.Log("Genvid Set Parameter useOpenGL performed correctly.");
                    }

                    Camera camera = GetCameraFromSource(m_CurrentVideoSource);
                    if (camera != null)
                    {
                        CaptureCamera(camera);
                    }
                    else
                    {
                        Debug.LogError("Cannot cast the current video source object: " + m_CurrentVideoSource.name);
                    }
                }
            }
        }
	}
}

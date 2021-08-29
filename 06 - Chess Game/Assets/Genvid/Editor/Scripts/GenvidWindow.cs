//C# Example
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using System.Reflection;
using Microsoft.Win32;
using System.IO;
using Genvid.Consul;

public class GenvidWindow : EditorWindow
{
    private readonly object safeGUI = new object();

    bool[] jobsStatus;
    List<Genvid.Model.Cluster.IJob> jobsList;
    bool getJobsOperation = false;
    List<Genvid.Model.Cluster.ILink> linkList;
    bool getLinksOperation = false;
    Genvid.Model.Cluster.Settings newSettings;
    bool getSettingsOperation = false;
    bool getNewSettings = false;
    bool reloadSettingsOperation = false;
    bool resetGuiFocus = false;
    Genvid.Model.Cluster.Settings oldSettingsLoaded;
    PropertyInfo[] settingsToSend;
    PropertyInfo[] oldSettingsToSendLoaded;
    const string statusRunning = "running";
    const float maxTimeToJob = 2.0f;    // Update jobs periodically (unit time is in second)

    bool projectAvailable = false;
    
    bool initFoldout = true;
    GUIStyle styleFoldout;
    Vector2 scrollPosition = Vector2.zero;

    bool jobsShowPosition = true;
    string jobsSectionName = "Jobs";

    bool linksShowPosition = true;
    string linksSectionName = "Links";

    bool logsShowPosition = true;
    string logsSectionName = "Logs";

    bool settingsShowPosition = true;
    string settingsSectionName = "Settings";

    bool[] jobsFoldout;
    bool initJobs = true;
    Dictionary<string, bool> settingsFoldout = new Dictionary<string, bool>();

    bool healthCheck = true;
    string healthCheckName = "Health Check";
    Genvid.Model.Cluster.HealthCheckOutput currentHealthCheckOutput = null;
    List<Genvid.Model.Cluster.HealthCheck> HealthCheckList = null;
    bool getHealthCheck = false;

    string activeClusterName = "";
    List<string> optionsCluster = new List<string> { "< Select a Cluster >"};
    int optionsClusterCount = 0;
    bool initClusterSelection = false;
    public List<Genvid.Model.Bastion.ClusterOperator> listClusterOperator;
    bool getClustersOperation = false;
    List<Genvid.Model.Bastion.ClusterOperator> oldListClusterOperator;
    string clusterSectionName = "Clusters";

    float oldTimeStartup;
    bool gameRunning = false;

    Color errorColor = Color.red;
    Color temporaryColor = Color.yellow;
    Color successColor = Color.green;

    const int buttonWidth = 100;
    const int maxHorizontalWidth = 500;
    const int maxTextFieldWidth = 400;
    const int taskLabelWidth = 34;
    readonly int[] taskMargin = { 200, 0, 0, 0 };
    const int startJob = 0;
    const int stopJob = 1;
    const int initWaitIndex = 0;
    const int healthCheckSelection = 0;
    const float spaceClipping = 15f;

    string nameFound = "";
    string heightFound = "";
    string widthFound = "";
    string audioFound = "";

    bool startStopAllJobsDone = true;
    string versionLoaded = "";

    [MenuItem("Window/Genvid")]
    public static void ShowWindow()
    {
        EditorWindow editorWindow = GetWindow(typeof(GenvidWindow), false, "Genvid", true);
        editorWindow.autoRepaintOnSceneChange = false;
        editorWindow.Show();
    }
    
    void Awake()
    {
        texture = AssetDatabase.LoadAssetAtPath("Assets/Genvid/Editor/Ressources/Textures/genvid-logo.png", typeof(Texture2D)) as Texture2D;
    }
    
    void OnEnable()
    {
        oldTimeStartup = Time.realtimeSinceStartup;
        EditorPrefs.SetString("currentSelectedClusterName", activeClusterName);
    }

    // GENVID - On ConsulClient start
    private static Genvid.Consul.Client _client = null;

    private static Genvid.Consul.Client Consul
    {
        get
        {
            if (_client == null)
            {
                _client = new Genvid.Consul.Client();
            }
            return _client;
        }
    }
    // GENVID - On ConsulClient stop

    private static string wrapIPv6Address(string addressSent)
    {
            if(addressSent.Contains(":") && !addressSent.Contains("["))
            {
                return "[" + addressSent + "]";
            }
            return addressSent;
    }

	// GENVID - GetClusterURL start
	
    private string GetClusterURL()
    {
        if (BastionUrl == "" || activeClusterName == "" || activeClusterName == "< Select a Cluster >")
            return "";

        return string.Format("{0}/proxy/{1}/cluster-api/v1", BastionUrl, activeClusterName);
    }
	
	// GENVID - GetClusterURL stop

    private string ClusterUIUrl
    {
        get
        {
            lock (safeGUI)
            {
                var url = GetClusterURL();
                // Remove /v1 from the end of the url.
                if (url.EndsWith("/v1"))
                {
                    return url.Substring(0, url.Length - 3);
                }
                return url;
            }
        }
    }

    private string BastionUrl = "";

    // GENVID - RequestBastionUrl start
    private void RequestBastionUrl()
    {
        var callback = new Genvid.Rest.ApiClient.apiCallDelegate(BastionUrlCallback);
        Consul.health.GetServiceEntries("bastion-api", callback);
    }
    // GENVID - RequestBastionUrl stop

    void BastionUrlCallback(RestSharp.IRestResponse apiCallResponse)
    {
        if (apiCallResponse.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var json = apiCallResponse.Content;
            var entries = JsonConvert.DeserializeObject<Health.ServiceEntry[]>(json);
            UnityEngine.Debug.Log("GetBastionUrl executed properly.");

            var services = entries.GetEnumerator();
            if (services.MoveNext())
            {
                var service = services.Current as Health.ServiceEntry;
                BastionUrl = string.Format("http://{0}:{1}/v1", wrapIPv6Address(service.Service.Address), service.Service.Port);
                projectAvailable = true;
            }
        }
        else
        {
            projectAvailable = false;
            string errorMessage = ", message: ";
            if (apiCallResponse.ErrorMessage != null && apiCallResponse.ErrorMessage != "")
            {
                errorMessage += apiCallResponse.ErrorMessage;
            }
            else
            {
                errorMessage += apiCallResponse.StatusCode.ToString();
            }
            UnityEngine.Debug.LogError("Error calling GetBastionUrl, " + " error code: " + (int)apiCallResponse.StatusCode + errorMessage);
            BastionUrl = "";
        }
    }

    private void foldoutInit()
    {
        if (initFoldout)
        {
            styleFoldout = EditorStyles.foldout;
            initFoldout = false;

            styleFoldout.fontStyle = FontStyle.Bold;
        }
    }

    private int GetClusterIndex(string clusterName, List<string> clusters)
    {
        for (int i = 0; i < clusters.Count; ++i)
        {
            if (clusters[i] == clusterName)
                return i;
        }
        UnityEngine.Debug.Log("Couldn't find the requested cluster in the provided cluster array.");
        return 0;
    }

    private string GetClusterName(int index, List<string> clusters)
    {
        if (index < clusters.Count)
            return clusters[index];
        return "";
    }

    private void clustersListDisplay()
    {
        lock(safeGUI)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(maxHorizontalWidth));
            var clusterShowPosition = EditorGUILayout.Foldout(true, clusterSectionName, styleFoldout);
            GUILayout.Space(spaceClipping);
            GUILayout.Label("(" + optionsClusterCount + ")", EditorStyles.label);
            GUILayout.FlexibleSpace();
            if (clusterShowPosition)
            {
                int currentClusterIndex = GetClusterIndex(EditorPrefs.GetString("currentSelectedClusterName"), optionsCluster);
                activeClusterName = GetClusterName(EditorGUILayout.Popup(currentClusterIndex, optionsCluster.ToArray()), optionsCluster);
                EditorPrefs.SetString("currentSelectedClusterName", activeClusterName);

                if (activeClusterName != EditorPrefs.GetString("previousSelectedClusterName"))
                {
                    EditorPrefs.SetString("previousSelectedClusterName", activeClusterName);
                    initClusterSelection |= (activeClusterName != "< Select a Cluster >");
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void healthCheckDisplay()
    {
        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(maxHorizontalWidth));
        healthCheck = EditorGUILayout.Foldout(healthCheck, healthCheckName, styleFoldout);
        GUILayout.FlexibleSpace();
        if (currentHealthCheckOutput != null)
        {
            int numberStream = 0;
            bool connectionStatus = true;
            foreach (PropertyInfo property in currentHealthCheckOutput.State.GetType().GetProperties())
            {
                var propertyValue = property.GetValue(currentHealthCheckOutput.State, null);
                if (property.PropertyType == typeof(Nullable<Boolean>))
                {
                    if(Convert.ToBoolean(propertyValue) == false)
                    {
                        connectionStatus = false;
                    }
                }
                else if (property.PropertyType == typeof(Nullable<Int32>) || property.PropertyType == typeof(string))
                {
                    numberStream = int.Parse(propertyValue.ToString());
                }
            }
            GUILayout.Label(numberStream + " Stream(s) | Connected:" + ((connectionStatus) ? "Yes" : "No"), EditorStyles.label);
        }
        else
        {
            GUILayout.Label("0 Stream(s) | Connected: No", EditorStyles.label);
        }
        
        EditorGUILayout.EndHorizontal();
        if (healthCheck)
        {
            if (currentHealthCheckOutput != null)
            {
                foreach (PropertyInfo property in currentHealthCheckOutput.State.GetType().GetProperties())
                {
                    var propertyValue = property.GetValue(currentHealthCheckOutput.State, null);
                    if (property.PropertyType == typeof(Nullable<Boolean>))
                    {
                        EditorGUILayout.Toggle(property.Name, Convert.ToBoolean(propertyValue));
                    }
                    else if (property.PropertyType == typeof(Nullable<Int32>) || property.PropertyType == typeof(string))
                    {
                        EditorGUILayout.TextField(property.Name, propertyValue.ToString());
                    }
                }
            }
            else
            {
                GUILayout.Label("No project currently running", EditorStyles.label);
            }
        }
    }

    private string numberJobsRunning(Genvid.Model.Cluster.IJob job, out bool joobActive)
    {
        int? jobRun = 0;
        int? jobTotal = 0;

        for(int j = 0; j < job.taskGroups.Count; j++)
        {
            jobTotal += job.taskGroups[j].count;
            jobRun += job.taskGroups[j].summary.running;
        }

        if(jobRun == jobTotal)
        {
            joobActive = true;
        }
        else
        {
            joobActive = false;
        }

        return jobRun + "/" + jobTotal;
    }

    private void jobsDisplay()
    {
        UpdateStatuses();
        UpdateFoldouts();
        lock (safeGUI)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(maxHorizontalWidth));
            jobsShowPosition = EditorGUILayout.Foldout(jobsShowPosition, jobsSectionName, styleFoldout);
            var oldColor = GUI.backgroundColor;
            if (jobsList != null)
            {
                int jobActive = 0;
                if (jobsStatus != null)
                {
                    for (int i = 0; i < jobsStatus.Length; i++)
                    {
                        if (jobsStatus[i] == true)
                        {
                            jobActive++;
                        }
                    }

                    EditorGUILayout.LabelField("(" + jobActive + "/" + jobsStatus.Length + ")");

                    if (jobsList.Count != 0)
                    {
                        GUILayout.FlexibleSpace();

                        GUI.backgroundColor = successColor;
                        int countJobList = 0;
                        string contentButton = "Stop All";
                        for (int i = 0; i < jobsList.Count; i++)
                        {
                            if (jobsList[i].status != statusRunning)
                            {
                                countJobList++;
                                GUI.backgroundColor = temporaryColor;
                                contentButton = "Start All";
                            }

                            if (countJobList == jobsList.Count)
                            {
                                GUI.backgroundColor = oldColor;
                                contentButton = "Start All";
                            }
                        }

                        if (GUILayout.Button(contentButton, EditorStyles.miniButton))
                        {
                            if (startStopAllJobsDone)
                            {
                                startStopAllJobsDone = false;
                                startStopAllJobs();
                            }

                        }

                        GUI.backgroundColor = oldColor;
                    }

                }
            }

            EditorGUILayout.EndHorizontal();
            if (jobsShowPosition)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical();
                if (jobsList != null && jobsFoldout != null && jobsList.Count != 0)
                {
                    if (jobsFoldout.Length == jobsList.Count)
                    {
                        for (int i = 0; i < jobsList.Count; i++)
                        {
                            var statusJobs = false;
                            EditorGUILayout.BeginHorizontal();
                            jobsFoldout[i] = EditorGUILayout.Foldout(jobsFoldout[i], jobsList[i].name + " (" + jobsList[i].status + ": " + numberJobsRunning(jobsList[i], out statusJobs) + ")");
                            {
                                var styleJob = new GUIStyle(EditorStyles.miniButton);
                                styleJob.margin = new RectOffset(taskMargin[0], taskMargin[1], taskMargin[2], taskMargin[3]);
                                oldColor = GUI.backgroundColor;

                                if (jobsStatus[i] == false)
                                {
                                    GUI.backgroundColor = errorColor;
                                }
                                else
                                {
                                    if (statusJobs)
                                    {
                                        GUI.backgroundColor = successColor;
                                    }
                                    else
                                    {
                                        GUI.backgroundColor = temporaryColor;
                                    }
                                }

                                if (GUILayout.Button("On/Off", styleJob))
                                {
                                    if (jobsStatus[i] == false)
                                    {
                                        connectToWebJobs(startJob, jobsList[i].name);
                                    }
                                    else
                                    {
                                        connectToWebJobs(stopJob, jobsList[i].name);
                                    }
                                }
                                GUI.backgroundColor = oldColor;
                                EditorGUILayout.EndHorizontal();

                                if (jobsFoldout[i])
                                {
                                    if (jobsList[i].taskGroups.Count != 0)
                                    {
                                        var oldLabelWidth = EditorGUIUtility.labelWidth;
                                        for (int j = 0; j < jobsList[i].taskGroups.Count; j++)
                                        {
                                            EditorGUIUtility.labelWidth = taskLabelWidth;
                                            EditorGUILayout.BeginHorizontal();
                                            EditorGUILayout.LabelField(jobsList[i].taskGroups[j].name);
                                            EditorGUILayout.LabelField("count(" + jobsList[i].taskGroups[j].count + ")");
                                            EditorGUILayout.LabelField("starting(" + jobsList[i].taskGroups[j].summary.starting + ")");
                                            EditorGUILayout.LabelField("running(" + jobsList[i].taskGroups[j].summary.running + ")");
                                            EditorGUILayout.EndHorizontal();
                                        }
                                        EditorGUIUtility.labelWidth = oldLabelWidth;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("Foldout and jobs don't have the same number of elements. Possibility of out of bounds errors");
                    }
                }
                else
                {
                    GUILayout.Label("No job available", EditorStyles.label);
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
        }
    }

    // GENVID - On job start and stop callback start
    void startJobCallback(RestSharp.IRestResponse apiCallResponse)
    {
        if (((int)apiCallResponse.StatusCode) >= 400)
        {
            UnityEngine.Debug.LogError("Error calling startJob: " + apiCallResponse.Content + "with error code: " + (int)apiCallResponse.StatusCode + ".");
        }
        else if (((int)apiCallResponse.StatusCode) == 0)
        {
            UnityEngine.Debug.LogError("Error calling startJob: " + apiCallResponse.Content + "with error code: " + (int)apiCallResponse.StatusCode + ".");
        }
        else
        {
            UnityEngine.Debug.Log("startJob executed properly.");
        }
    }

    void stopJobCallback(RestSharp.IRestResponse apiCallResponse)
    {
        if (((int)apiCallResponse.StatusCode) >= 400)
        {
            UnityEngine.Debug.LogError("Error calling stopJob: " + apiCallResponse.Content + "with error code: " + (int)apiCallResponse.StatusCode + ".");
        }
        else if (((int)apiCallResponse.StatusCode) == 0)
        {
            UnityEngine.Debug.LogError("Error calling stopJob: " + apiCallResponse.Content + "with error code: " + (int)apiCallResponse.StatusCode + ".");
        }
        else
        {
            UnityEngine.Debug.Log("stopJob executed properly.");
        }
    }
    // GENVID - On job start and stop callback stop

    // GENVID - On job connection start
    public void connectToWebJobs(int typeExecution, string jobName)
    {
        var api = new Genvid.Api.JobsApi(GetClusterURL());

        if (typeExecution == startJob)
        {
            var startJobCallbackObject = new Genvid.Rest.ApiClient.apiCallDelegate(startJobCallback);
            api.startJob(jobName, startJobCallbackObject);
        }
        else if (typeExecution == stopJob)
        {
            var stopJobCallbackObject = new Genvid.Rest.ApiClient.apiCallDelegate(stopJobCallback);
            api.stopJob(jobName, stopJobCallbackObject);
        }
    }
    // GENVID - On job connection stop

    void startAllJobCallback(RestSharp.IRestResponse apiCallResponse)
    {
        if (((int)apiCallResponse.StatusCode) >= 400)
        {
            UnityEngine.Debug.LogError("Error calling startAllJob: " + apiCallResponse.Content + "with error code: " + (int)apiCallResponse.StatusCode + ".");
        }
        else if (((int)apiCallResponse.StatusCode) == 0)
        {
            UnityEngine.Debug.LogError("Error calling startAllJob: " + apiCallResponse.Content + "with error code: " + (int)apiCallResponse.StatusCode + ".");
        }
        else
        {
            UnityEngine.Debug.Log("startAllJob executed properly.");
        }
    }

    void stopAllJobCallback(RestSharp.IRestResponse apiCallResponse)
    {
        if (((int)apiCallResponse.StatusCode) >= 400)
        {
            UnityEngine.Debug.LogError("Error calling stopAllJob: " + apiCallResponse.Content + "with error code: " + (int)apiCallResponse.StatusCode + ".");
        }
        else if (((int)apiCallResponse.StatusCode) == 0)
        {
            UnityEngine.Debug.LogError("Error calling stopAllJob: " + apiCallResponse.Content + "with error code: " + (int)apiCallResponse.StatusCode + ".");
        }
        else
        {
            UnityEngine.Debug.Log("stopAllJob executed properly.");
        }
    }

    private void startStopAllJobs()
    {
        lock (safeGUI)
        {
            bool typeExecution = false;
            for (int i = 0; i < jobsStatus.Length; i++)
            {
                if (jobsStatus[i] == false && jobsList[i].autostart == true)
                {
                    typeExecution = true;
                }
            }

            var api = new Genvid.Api.JobsApi(GetClusterURL());

            if(typeExecution)
            {
                var startAllJobCallbackObject = new Genvid.Rest.ApiClient.apiCallDelegate(startAllJobCallback);
                api.startAllJob(startAllJobCallbackObject);
            }
            else
            {
                var stopAllJobCallbackObject = new Genvid.Rest.ApiClient.apiCallDelegate(stopAllJobCallback);
                api.stopAllJob(stopAllJobCallbackObject);
            }

            startStopAllJobsDone = true;
        }
    }

    private void linksDisplay()
    {
        lock (safeGUI)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(maxHorizontalWidth));
            linksShowPosition = EditorGUILayout.Foldout(linksShowPosition, linksSectionName, styleFoldout);
            GUILayout.FlexibleSpace();            
            if (linkList != null)
            {
                int numberErrors = 0;
                for (int i = 0; i < linkList.Count; i++)
                {
                    if (linkList[i].error != null)
                    {
                        numberErrors++;
                    }
                }
                GUILayout.Label(numberErrors + " Error(s)", EditorStyles.label);
            }
            EditorGUILayout.EndHorizontal();

            if (linksShowPosition)
            {
                if (linkList != null)
                {
                    EditorGUI.indentLevel++;
                    // loops though the array and generates the menu items
                    for (int i = 0; i < linkList.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (linkList[i].error == null)
                        {
                            EditorGUILayout.LabelField(linkList[i].name);
                            if (GUILayout.Button("Open Link", EditorStyles.miniButton))
                            {
                                openLink(linkList[i].id, linkList[i].href);
                            }
                        }
                        else
                        {
                            var oldColor = GUI.backgroundColor;
                            GUI.backgroundColor = errorColor;

                            EditorGUILayout.LabelField(linkList[i].name);
                            if (GUILayout.Button("Error", EditorStyles.miniButton))
                            {
                                UnityEngine.Debug.Log("Unable to use link " + linkList[i].name + " because of this error: " + linkList[i].error + ".");
                            }

                            GUI.backgroundColor = oldColor;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
                else
                {
                    GUILayout.Label("No link available", EditorStyles.label);
                }
            }
        }
    }

    // GENVID - On link start
    public void openLink(string name, string url)
    {
        UnityEngine.Debug.Log(string.Format("Opening {0} at {1}", name, url) + ".");
        Application.OpenURL(url);
    }
    // GENVID - On link stop

    public void openLogs()
    {

        var logLink = ClusterUIUrl + "#/logs";
        UnityEngine.Debug.Log(string.Format("Opening {0} at {1}", "logs", logLink) + ".");
        Application.OpenURL(logLink);
    }

    private void logsDisplay()
    {
        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(maxHorizontalWidth));
        logsShowPosition = EditorGUILayout.Foldout(logsShowPosition, logsSectionName, styleFoldout);
        if (!logsShowPosition)
        {
            var styleLog = new GUIStyle(EditorStyles.miniButton);
            styleLog.fixedWidth = buttonWidth;
            styleLog.normal.textColor = Color.white;
            if (GUILayout.Button("Cluster-UI", styleLog))
            {
                openLogs();
            }
            styleLog = new GUIStyle(EditorStyles.miniButton);
            styleLog.fixedWidth = buttonWidth;
            styleLog.normal.textColor = Color.white;
            if (GUILayout.Button("Editor", styleLog))
            {
                var logFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                logFile = System.IO.Path.Combine(logFile, @"Unity\Editor\Editor.log");
                Application.OpenURL(logFile);
            }
        }
        EditorGUILayout.EndHorizontal();

        if(logsShowPosition)
        {
            EditorGUILayout.BeginVertical();
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cluster-UI Logs");
            if (GUILayout.Button("Open Link", EditorStyles.miniButton))
            {
                openLogs();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Editor Logs");
            if (GUILayout.Button("Open Link", EditorStyles.miniButton))
            {
                var logFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                logFile = System.IO.Path.Combine(logFile, @"Unity\Editor\Editor.log");
                Application.OpenURL(logFile);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
    }

    private void settingsDisplay()
    {
        lock (safeGUI)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(maxHorizontalWidth));
            settingsShowPosition = EditorGUILayout.Foldout(settingsShowPosition, settingsSectionName, styleFoldout);
        
            if (newSettings != null)
            {
                GUILayout.FlexibleSpace();
                nameFound = "";
                heightFound = "";
                widthFound = "";
                audioFound = "";

                settingsResume(settingsToSend, newSettings);

                GUILayout.Space(spaceClipping);
                GUILayout.Label(nameFound + " | " + widthFound + "x" + heightFound + " | " + "Audio " + audioFound, EditorStyles.label);
            }

            EditorGUILayout.EndHorizontal();
            if (settingsShowPosition)
            {
                if (newSettings != null)
                {
                    EditorGUILayout.BeginVertical();
                    settingsMenu(settingsToSend, newSettings);
                    EditorGUILayout.EndVertical();

                    GUILayout.BeginHorizontal(GUILayout.MaxWidth(maxHorizontalWidth));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Reload settings", GUILayout.Width(buttonWidth)))
                    {
                        reloadSettingsOperation = true;
                    }
                    if (GUILayout.Button("Save settings", GUILayout.Width(buttonWidth)))
                    {
                        saveSettings(newSettings);
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("No settings available", EditorStyles.label);
                }
            }
        }
    }

    private void settingsResume(PropertyInfo[] settingsReceived, object targetToSet)
    {
        foreach (PropertyInfo property in settingsReceived)
        {
            var propertyValue = property.GetValue(targetToSet, null);

            if (property.Name == "info" || property.Name == "encode" || property.Name == "input" || property.Name == "output")
            {
                var settingsToSend = propertyValue.GetType().GetProperties();
                var parentTarget = property.GetValue(targetToSet, null);

                settingsResume(settingsToSend, parentTarget);
            }
            else if (property.Name == "name")
            {
                nameFound = propertyValue.ToString();
            }
            else if (property.Name == "width" && targetToSet.ToString().Contains("Output"))
            {
                widthFound = propertyValue.ToString();
            }
            else if (property.Name == "height" && targetToSet.ToString().Contains("Output"))
            {
                heightFound = propertyValue.ToString();
            }
            else if (property.Name == "silent" && targetToSet.ToString().Contains("Input"))
            {
                if (Convert.ToBoolean(propertyValue))
                {
                    audioFound = "Off";
                }
                else
                {
                    audioFound = "On";
                }
            }
        }
    }

    void saveSettingsCallback(RestSharp.IRestResponse apiCallResponse)
    {
        if (((int)apiCallResponse.StatusCode) >= 400)
        {
            UnityEngine.Debug.LogError("Error calling saveSettings: " + apiCallResponse.Content + "with error code: " + (int)apiCallResponse.StatusCode + ".");
        }
        else if (((int)apiCallResponse.StatusCode) == 0)
        {
            UnityEngine.Debug.LogError("Error calling saveSettings: " + apiCallResponse.Content + "with error code: " + (int)apiCallResponse.StatusCode + ".");
        }
        else
        {
            UnityEngine.Debug.Log("New settings were saved properly.");
        }
    }

    // GENVID - On save settings start
    public void saveSettings(Genvid.Model.Cluster.Settings newSettings)
    {
        var api = new Genvid.Api.SettingsApi(GetClusterURL());
        var saveSettingsCallbackObject = new Genvid.Rest.ApiClient.apiCallDelegate(saveSettingsCallback);
        api.setSettings(newSettings, saveSettingsCallbackObject);
    }
    // GENVID - On save settings stop

    private void settingsMenu(PropertyInfo[] settingsReceived, object targetToSet)
    {
        EditorGUI.indentLevel++;
        foreach (PropertyInfo property in settingsReceived)
        {
            var propertyValue = property.GetValue(targetToSet, null);

            if (property.PropertyType == typeof(Nullable<Boolean>))
            {
                bool contentSelection = EditorGUILayout.Toggle(property.Name, Convert.ToBoolean(propertyValue));
                property.SetValue(targetToSet, contentSelection, null);
            }
            else if (property.PropertyType == typeof(Nullable<Int32>))
            {
                string contentInside = EditorGUILayout.TextField(property.Name, propertyValue.ToString(), GUILayout.MaxWidth(maxTextFieldWidth));
                property.SetValue(targetToSet, Int32.Parse(contentInside), null);
            }
            else if (property.PropertyType == typeof(Nullable<float>))
            {
                string contentInside = EditorGUILayout.TextField(property.Name, propertyValue.ToString(), GUILayout.MaxWidth(maxTextFieldWidth));
                property.SetValue(targetToSet, float.Parse(contentInside), null);
            }
            else if (property.PropertyType == typeof(string))
            {
                string contentInside = EditorGUILayout.TextField(property.Name, propertyValue.ToString(), GUILayout.MaxWidth(maxTextFieldWidth));
                property.SetValue(targetToSet, contentInside, null);
            }
            else if (property.PropertyType == typeof(Newtonsoft.Json.Linq.JToken))
            {
                property.SetValue(targetToSet, propertyValue, null);
            }
            else if (property.Name != null)
            {
                bool tryValue;
                if (!settingsFoldout.TryGetValue(property.Name, out tryValue))
                {
                    settingsFoldout.Add(property.Name, true);
                }
                settingsFoldout[property.Name] = EditorGUILayout.Foldout(settingsFoldout[property.Name], property.Name, styleFoldout);

                if (settingsFoldout[property.Name])
                {
                    var settingsToSend = propertyValue.GetType().GetProperties();
                    var parentTarget = property.GetValue(targetToSet, null);
                    settingsMenu(settingsToSend, parentTarget);
                }
            }
        }
        EditorGUI.indentLevel--;
    }

    private bool compareSettings(PropertyInfo[] newSettingsReceived, object newSettingsTarget, PropertyInfo[] oldSettingsReceived, object oldSettingsTarget)
    {
        foreach (PropertyInfo property in newSettingsReceived)
        {
            var propertyValue = property.GetValue(newSettingsTarget, null);

            foreach (PropertyInfo oldProperty in oldSettingsReceived)
            {
                if(property.Name == oldProperty.Name)
                {
                    var oldPropertyValue = oldProperty.GetValue(oldSettingsTarget, null);

                    if (property.Name != null && property.PropertyType != typeof(Nullable<Boolean>) && property.PropertyType != typeof(Nullable<Int32>) &&
                        property.PropertyType != typeof(Nullable<float>)  && property.PropertyType != typeof(string))
                    {
                        var newSettingsToSend = propertyValue.GetType().GetProperties();
                        var newParentTarget = property.GetValue(newSettingsTarget, null);
                        var oldSettingsToSend = oldPropertyValue.GetType().GetProperties();
                        var oldParentTarget = oldProperty.GetValue(oldSettingsTarget, null);

                        if(compareSettings(newSettingsToSend, newParentTarget, oldSettingsToSend, oldParentTarget) == false)
                        {
                            return false;
                        }
                    }
                    else if(propertyValue.Equals(oldPropertyValue) == false)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    // GENVID - On getSettings callback start
    void getSettingsCallback(RestSharp.IRestResponse apiCallResponse)
    {
        if (((int)apiCallResponse.StatusCode) >= 400)
        {
            UnityEngine.Debug.LogError("Error calling getSettings: " + apiCallResponse.Content + "with error code: " + (int)apiCallResponse.StatusCode + ".");
        }
        else if (((int)apiCallResponse.StatusCode) == 0)
        {
            UnityEngine.Debug.LogError("Error calling getSettings: " + apiCallResponse.Content + "with error code: " + (int)apiCallResponse.StatusCode + ".");
        }
        else
        {
            lock (safeGUI)
            {
                if (reloadSettingsOperation && getNewSettings == false)
                {
                    oldSettingsToSendLoaded = null;
                    oldSettingsLoaded = null;
                    getNewSettings = true;
                }
                Genvid.Rest.ApiClient settingsApiClient = new Genvid.Rest.ApiClient();
                var tempSettings = (Genvid.Model.Cluster.Settings)settingsApiClient.Deserialize(apiCallResponse.Content, typeof(Genvid.Model.Cluster.Settings));
                var tempSettingsProperties = tempSettings.GetType().GetProperties();

                if (getNewSettings)
                {
                    newSettings = tempSettings;
                    settingsToSend = newSettings.GetType().GetProperties();
                    getNewSettings = false;
                    getSettingsOperation = false;
                    if (reloadSettingsOperation)
                    {
                        UnityEngine.Debug.Log("Settings reload completed.");
                        reloadSettingsOperation = false;
                        resetGuiFocus = true;
                    }
                }
                else if (oldSettingsLoaded == null || compareSettings(tempSettingsProperties, tempSettings, oldSettingsToSendLoaded, oldSettingsLoaded) == false)
                {
                    oldSettingsLoaded = tempSettings;
                    oldSettingsToSendLoaded = oldSettingsLoaded.GetType().GetProperties();
                    getNewSettings = true;
                    getSettingsOperation = false;
                    loadSettings();
                }
            }
        }
        getSettingsOperation = false;
    }
    // GENVID - On getSettings callback stop

    // GENVID - On loadSettings start
    private void loadSettings()
    {
        if(getSettingsOperation == false)
        {
            var api = new Genvid.Api.SettingsApi(GetClusterURL());
            var getSettingsCallbackObject = new Genvid.Rest.ApiClient.apiCallDelegate(getSettingsCallback);
            api.getSettings(getSettingsCallbackObject);
            getSettingsOperation = true;
        }
    }
    // GENVID - On loadSettings stop

    Texture2D texture = null;
    Texture2D textureRefresh = null;
    GUIStyle textureStyle = null;
    bool showFoldout1 = true;
    bool showFoldout2 = true;
    bool showFoldout3 = true;

    void findDllVersion()
    {
        string subPath;

        if (Application.isEditor)
        {
            if (IntPtr.Size == 8)
            {
                subPath = "/Genvid/SDK/Plugins/x64/Genvid.dll";
            }
            else
            {
                subPath = "/Genvid/SDK/Plugins/x86/Genvid.dll";
            }
        }
        else
        {
            subPath = "/Plugins/Genvid.dll";
        }

        try
        {
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(Application.dataPath + subPath);
            versionLoaded = info.FileVersion;
        }
        catch (Exception ex)
        {
            versionLoaded = "No Genvid.dll present.";
            UnityEngine.Debug.LogError(ex.Message);
        }
    }

    void versionFoldout()
    {
        if (textureStyle == null)
        {
            textureStyle = new GUIStyle(EditorStyles.toolbarButton);
            textureStyle.fixedHeight = 32;
        }

        if (GUILayout.Button(texture, textureStyle, GUILayout.MaxWidth(maxHorizontalWidth)))
        {
            Application.OpenURL("https://www.genvidtech.com/");
        }

        //Get SDK version
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Genvid Technologies");
        string sdkVersion = "Missing";
        if(key != null)
        {
            var keyArray = key.GetSubKeyNames();
            if(keyArray.Length != 0)
            {
                sdkVersion = "";
                foreach (string versionInKey in keyArray)
                {
                    sdkVersion += versionInKey + " ";
                }
            }
        }

        var passColor = new Color(0.5f, 1, 0.5f, 0.5f);
        var failColor = new Color(1, 0.25f, 0.25f, 0.5f);
        var oldColor = GUI.backgroundColor;

        if(sdkVersion == "Missing")
        {
            GUI.backgroundColor = failColor;
        }
        else
        {
            GUI.backgroundColor = passColor;
        }
        
        EditorGUILayout.BeginHorizontal(EditorStyles.miniButtonMid, GUILayout.MaxWidth(maxHorizontalWidth));
        showFoldout1 = EditorGUILayout.Foldout(showFoldout1, "Genvid SDK", true);
        GUILayout.FlexibleSpace();

        if (GUI.backgroundColor == passColor)
        {
            GUILayout.Label("Present", EditorStyles.miniLabel);
        }
        else
        {
            GUILayout.Label("Missing", EditorStyles.miniLabel);
        }        

        EditorGUILayout.EndHorizontal();
        if (showFoldout1)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.MaxWidth(maxHorizontalWidth));
            EditorGUILayout.BeginVertical(EditorStyles.label, GUILayout.MaxWidth(maxHorizontalWidth));
            
            if(GUI.backgroundColor == passColor)
            {
                var keyArray = key.GetSubKeyNames();
                if(keyArray.Length != 0)
                {
                    foreach (string versionInKey in keyArray)
                    {
                        RegistryKey regKey = key.OpenSubKey(versionInKey);
                        var installDir = regKey.GetValue("InstallDir") as string;
                        if (Directory.Exists(installDir))
                        {
                            if (GUILayout.Button("Path: " + installDir, EditorStyles.miniLabel))
                            {
                                Application.OpenURL(installDir);
                            }
                        }
                        else
                        {
                            GUILayout.Label("Path: " + StrikeThrough(installDir), EditorStyles.miniLabel);
                        }
                    }
                }

            }
            else
            {
                GUILayout.Label("Status: Missing", EditorStyles.miniLabel);
                GUILayout.Label("Path: Unknown", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        //Check if genvidPlugin is present
        if (File.Exists(System.Environment.CurrentDirectory + "/Assets/Genvid/SDK/Plugins/x64/GenvidPlugin.dll")) 
        {
            GUI.backgroundColor = passColor;
        }
        else
        {
            GUI.backgroundColor = failColor;
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.miniButtonMid, GUILayout.MaxWidth(maxHorizontalWidth));
        showFoldout2 = EditorGUILayout.Foldout(showFoldout2, "GenvidPlugin.dll", true);
        GUILayout.FlexibleSpace();

        if(GUI.backgroundColor == passColor)
        {
            GUILayout.Label("Present", EditorStyles.miniLabel);
        }
        else
        {
            GUILayout.Label("Missing", EditorStyles.miniLabel);
        }
        
        EditorGUILayout.EndHorizontal();
        if (showFoldout2)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.MaxWidth(maxHorizontalWidth));
            EditorGUILayout.BeginVertical(EditorStyles.label, GUILayout.MaxWidth(maxHorizontalWidth));

            if(GUI.backgroundColor == passColor)
            {
                if (IntPtr.Size == 8)
                {
                    GUILayout.Label(@"Path: /Assets/Genvid/SDK/Plugins/x64/", EditorStyles.miniLabel);
                }
                else
                {
                    GUILayout.Label(@"Path: /Assets/Genvid/SDK/Plugins/x86/", EditorStyles.miniLabel);
                }
            }
            else
            {
                GUILayout.Label("Status: Missing", EditorStyles.miniLabel);
                GUILayout.Label(@"Path: Unknown", EditorStyles.miniLabel);
            }
                
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        if(versionLoaded == "")
        {
            findDllVersion();
        }

        if(versionLoaded != "No Genvid.dll present.")
        {
            GUI.backgroundColor = passColor;
        }
        else
        {
            GUI.backgroundColor = errorColor;
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.miniButtonMid, GUILayout.MaxWidth(maxHorizontalWidth));
        showFoldout3 = EditorGUILayout.Foldout(showFoldout3, "Genvid.dll", true);
        GUILayout.FlexibleSpace();

        if(GUI.backgroundColor == passColor)
        {
            GUILayout.Label(versionLoaded, EditorStyles.miniLabel);
        }
        else
        {
            GUILayout.Label("Missing", EditorStyles.miniLabel);
        }

        if(textureRefresh == null)
        {
            textureRefresh = AssetDatabase.LoadAssetAtPath("Assets/Genvid/Editor/Ressources/Textures/sync-solid.png", typeof(Texture2D)) as Texture2D;
        }

        if(GUILayout.Button(textureRefresh))
        {
            findDllVersion();
        }
        
        EditorGUILayout.EndHorizontal();
        if (showFoldout3)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(maxHorizontalWidth));

            if(GUI.backgroundColor == passColor)
            {
                if (IntPtr.Size == 8)
                {
                    GUILayout.Label(@"Path: /Assets/Genvid/SDK/Plugins/x64/", EditorStyles.miniLabel);
                }
                else
                {
                    GUILayout.Label(@"Path: /Assets/Genvid/SDK/Plugins/x86/", EditorStyles.miniLabel);
                }
            }
            else
            {
                GUILayout.Label("Status: Missing", EditorStyles.miniLabel);
                GUILayout.Label(@"Path: Unknown", EditorStyles.miniLabel);
            }
                    
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        GUI.backgroundColor = oldColor;

        GUILayout.Label("Cluster-UI", EditorStyles.miniButtonMid, GUILayout.MaxWidth(maxHorizontalWidth));
    }

    void OnGUI()
    {
        lock (safeGUI)
        {
            // Reset the UI control focus.
            if (resetGuiFocus)
            {
                GUI.FocusControl(null);
                resetGuiFocus = false;
            }

            versionFoldout();
            foldoutInit();

            // loops though the array and generates the menu items
            if (projectAvailable)
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

                clustersListDisplay();

                if (initClusterSelection)
                {
                    healthCheckDisplay();
                    jobsDisplay();
                    linksDisplay();
                    logsDisplay();
                    settingsDisplay();
                }

                GUI.EndScrollView();
            }
            else
            {
                GUILayout.Label("No project loaded", EditorStyles.label);
            }
        }
    }

    // GENVID - On clusters get callback start
    void getClustersCallback(RestSharp.IRestResponse apiCallResponse)
    {
        if (apiCallResponse.StatusCode != System.Net.HttpStatusCode.OK)
        {
            string errorMessage = ", message: ";
            if (apiCallResponse.ErrorMessage != null && apiCallResponse.ErrorMessage != "")
            {
                errorMessage += apiCallResponse.ErrorMessage;
            }
            else
            {
                errorMessage += apiCallResponse.StatusCode.ToString();
            }
            UnityEngine.Debug.LogError("Error calling getClusters, " + " error: " + (int)apiCallResponse.StatusCode + errorMessage);
        }
        else
        {
            lock (safeGUI)
            {
                Genvid.Rest.ApiClient clusterApiClient = new Genvid.Rest.ApiClient();
                listClusterOperator = (List<Genvid.Model.Bastion.ClusterOperator>) clusterApiClient.Deserialize(apiCallResponse.Content, typeof(List<Genvid.Model.Bastion.ClusterOperator>));
                listClusterOperator.Sort(delegate (Genvid.Model.Bastion.ClusterOperator elementCompare1, Genvid.Model.Bastion.ClusterOperator elementCompare2)
                {
                    return elementCompare1.id.CompareTo(elementCompare2.id);
                });
                getClustersOperation = false;
            }
        }
    }
    // GENVID - On clusters get callback stop

    void getJobsCallback(RestSharp.IRestResponse apiCallResponse)
    {
        if (apiCallResponse.StatusCode != System.Net.HttpStatusCode.OK)
        {
            string errorMessage = ", message: ";
            if (apiCallResponse.ErrorMessage != null && apiCallResponse.ErrorMessage != "")
            {
                errorMessage += apiCallResponse.ErrorMessage;
            }
            else
            {
                errorMessage += apiCallResponse.StatusCode.ToString();
            }
            UnityEngine.Debug.LogError("Error calling getJobs, " + " error: " + (int)apiCallResponse.StatusCode + errorMessage);

            lock (safeGUI)
            {
                getJobsOperation = false;
            }
        }
        else
        {
            lock (safeGUI)
            {
                Genvid.Rest.ApiClient jobApiClient = new Genvid.Rest.ApiClient();
                jobsList = (List<Genvid.Model.Cluster.IJob>)jobApiClient.Deserialize(apiCallResponse.Content, typeof(List<Genvid.Model.Cluster.IJob>));
                jobsList.Sort((x, y) => x.name.CompareTo(y.name));
                getJobsOperation = false;
            }
        }
    }

    // GENVID - getLinks callback start
    void getLinksCallback(RestSharp.IRestResponse apiCallResponse)
    {
        if (apiCallResponse.StatusCode != System.Net.HttpStatusCode.OK)
        {
            string errorMessage = ", message: ";
            if (apiCallResponse.ErrorMessage != null && apiCallResponse.ErrorMessage != "")
            {
                errorMessage += apiCallResponse.ErrorMessage;
            }
            else
            {
                errorMessage += apiCallResponse.StatusCode.ToString();
            }
            UnityEngine.Debug.LogError("Error calling getLinks, " + " error: " + (int)apiCallResponse.StatusCode + errorMessage);

            lock (safeGUI)
            {
                getLinksOperation = false;
            }
        }
        else
        {
            lock (safeGUI)
            {
                Genvid.Rest.ApiClient linksApiClient = new Genvid.Rest.ApiClient();
                linkList = (List<Genvid.Model.Cluster.ILink>)linksApiClient.Deserialize(apiCallResponse.Content, typeof(List<Genvid.Model.Cluster.ILink>));
                linkList.Sort((x, y) => x.name.CompareTo(y.name));
                // Add Cluster UI at the top.
                linkList.Insert(0, new Genvid.Model.Cluster.ILink
                {
                    id = "cluster-ui",
                    name = "Cluster-UI",
                    category = "global",
                    href = ClusterUIUrl,
                });
                getLinksOperation = false;
            }
        }
    }
    // GENVID - getLinks callback stop

    // GENVID - On health check callback start
    void getHealthCheckCallback(RestSharp.IRestResponse apiCallResponse)
    {
        if (apiCallResponse.StatusCode != System.Net.HttpStatusCode.OK)
        {
            string errorMessage = ", message: ";
            if (apiCallResponse.ErrorMessage != null && apiCallResponse.ErrorMessage != "")
            {
                errorMessage += apiCallResponse.ErrorMessage;
            }
            else
            {
                errorMessage += apiCallResponse.StatusCode.ToString();
            }
            UnityEngine.Debug.LogError("Error calling getHealth, " + " error: " + (int)apiCallResponse.StatusCode + errorMessage);

            lock (safeGUI)
            {
                getHealthCheck = false;
            }
        }
        else
        {
            lock (safeGUI)
            {
                Genvid.Rest.ApiClient healthApiClient = new Genvid.Rest.ApiClient();
                HealthCheckList = (List<Genvid.Model.Cluster.HealthCheck>)healthApiClient.Deserialize(apiCallResponse.Content, typeof(List<Genvid.Model.Cluster.HealthCheck>));
                getHealthCheck = false;
            }
        }
    }
    // GENVID - On health check callback stop

    private void UpdateStatuses()
    {
        lock (safeGUI)
        {
            if (jobsList != null)
            {
                jobsStatus = new bool[jobsList.Count];
                for (int i = 0; i < jobsList.Count; i++)
                {
                    jobsStatus[i] = jobsList[i].status == statusRunning;
                }
            }
        }
    }

    private void UpdateFoldouts()
    {
        lock (safeGUI)
        {
            if (jobsList != null)
            {
                if (jobsFoldout == null || jobsList.Count != jobsFoldout.Length)
                {
                    initJobs = true;
                }

                if (initJobs)
                {
                    jobsFoldout = new bool[jobsList.Count];
                    for (int i = 0; i < jobsFoldout.Length; ++i)
                        jobsFoldout[i] = false;
                }
            }
        }
    }

    void OnInspectorUpdate()
    {
        if(EditorApplication.isPlaying != gameRunning)
        {
            gameRunning = EditorApplication.isPlaying;
            oldTimeStartup = Time.realtimeSinceStartup;
        }

        if (Time.realtimeSinceStartup - oldTimeStartup > maxTimeToJob)
        {
            lock (safeGUI)
            {
                oldTimeStartup = Time.realtimeSinceStartup;

                if (BastionUrl == "")
                {
                    RequestBastionUrl();
                }

                if (projectAvailable && BastionUrl != "")
                {
                    string stringCluster = GetClusterURL();

                    if (initClusterSelection)
                    {
                        if (getJobsOperation == false)
                        {
                            var jobapi = new Genvid.Api.JobsApi(stringCluster);
                            var getJobsCallbackObject = new Genvid.Rest.ApiClient.apiCallDelegate(getJobsCallback);
                            jobapi.getJobs(initWaitIndex, getJobsCallbackObject);
                            getJobsOperation = true;
                        }

                        // GENVID - getLinks start
                        if (getLinksOperation == false)
                        {
                            var linkapi = new Genvid.Api.LinksApi(stringCluster);
                            var getLinksCallbackObject = new Genvid.Rest.ApiClient.apiCallDelegate(getLinksCallback);
                            linkapi.getLinks(null, null, getLinksCallbackObject);
                            getLinksOperation = true;
                        }
                        // GENVID - getLinks stop

                        initJobs = false;

                        loadSettings();

                        // GENVID - On health check start
                        if (getHealthCheck == false)
                        {
                            var healthApi = new Genvid.Api.HealthApi(stringCluster);
                            var getHealthCheckCallbackObject = new Genvid.Rest.ApiClient.apiCallDelegate(getHealthCheckCallback);
                            healthApi.getServiceHealth("SDK Health check", getHealthCheckCallbackObject);
                            getHealthCheck = true;
                        }
                        // GENVID - On health check stop

                        // GENVID - On health check serialize start
                        bool passing = HealthCheckList != null && HealthCheckList.Count > 0 && HealthCheckList[healthCheckSelection].Status == "passing";
                        currentHealthCheckOutput = passing ? JsonConvert.DeserializeObject<Genvid.Model.Cluster.HealthCheckOutput>(HealthCheckList[healthCheckSelection].Output) : null;
                        // GENVID - On health check serialize stop
                    }
                    // GENVID - On clusters get start
                    if (getClustersOperation == false)
                    {
                        var tempBastion = new Genvid.Api.BastionClustersApi(BastionUrl);
                        var getClustersCallbackObject = new Genvid.Rest.ApiClient.apiCallDelegate(getClustersCallback);
                        tempBastion.getClusters(getClustersCallbackObject);
                        getClustersOperation = true;
                    }
                    // GENVID - On clusters get stop

                    if (listClusterOperator != oldListClusterOperator)
                    {
                        optionsClusterCount = 0;
                        optionsCluster = initClusterSelection ? new List<string> { } : new List<string> { "< Select a Cluster >" };

                        if (listClusterOperator.Count > 0)
                        {
                            foreach (Genvid.Model.Bastion.ClusterOperator element in listClusterOperator)
                            {
                                if (element.category == "cluster")
                                {
                                    optionsCluster.Add(element.id);
                                    ++optionsClusterCount;
                                }
                            }
                        }
                        else
                        {
                            optionsCluster = new List<string> { "< Select a Cluster >" };
                        }
                        oldListClusterOperator = listClusterOperator;
                    }
                }
            }

            Repaint();
        }
    }

    public string StrikeThrough(string s)
    {
         string strikethrough = "";
         foreach (char c in s)
         {
             strikethrough = strikethrough + c + '\u0336';
         }
         return strikethrough;
    }
}
#endif

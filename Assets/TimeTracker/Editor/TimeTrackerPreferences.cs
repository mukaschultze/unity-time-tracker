using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TimeTrackerPreferences {

    #if UNITY_2018_3_OR_NEWER
    [SettingsProvider]
    private static SettingsProvider RetrieveSettingsProvider() {
        var settingsProvider = new SettingsProvider("Preferences/TimeTracker", SettingsScope.User);
        settingsProvider.guiHandler = new Action<string>((str) => OnPreferencesGUI(str));
        return settingsProvider;
    }

    #else
    [PreferenceItem("TimeTracker")]
    private static void OnPreferencesGUI() {
        OnPreferencesGUI(string.Empty);
    }
    #endif

    public static string DefaultLogFilePath {
        get { return Path.GetFullPath("time-tracker.log"); }
    }

    public static string LogFilePath {
        get { return EditorPrefs.GetString("TimeTracker_LogFilePath", DefaultLogFilePath); }
        set { EditorPrefs.SetString("TimeTracker_LogFilePath", Path.GetFullPath(value)); }
    }

    public static string LogLine {
        get { return EditorPrefs.GetString("TimeTracker_LogLine", "[%date:s%] %event% @ %project%"); }
        set { EditorPrefs.SetString("TimeTracker_LogLine", value); }
    }

    public static float AFKThreshold {
        get { return EditorPrefs.GetFloat("TimeTracker_AFKThreshold", 60f); }
        set { EditorPrefs.SetFloat("TimeTracker_AFKThreshold", value); }
    }

    private static readonly GUIContent content = new GUIContent();

    private static void OnPreferencesGUI(string search) {

        var demoEvt = TimeTrackerEvent.EditorFocus;

        using(new EditorGUILayout.HorizontalScope()) {

            content.text = content.tooltip = LogFilePath;
            GUILayout.Label(content, GUILayout.MinWidth(150f));

            if (GUILayout.Button("Browse")) {
                var path = EditorUtility.SaveFilePanel(
                    "Select log file location",
                    Path.GetDirectoryName(LogFilePath),
                    Path.GetFileName(LogFilePath),
                    Path.GetExtension(LogFilePath).Replace(".", "")
                );

                if (!string.IsNullOrWhiteSpace(path))
                    LogFilePath = path;
            }
        }

        AFKThreshold = EditorGUILayout.Slider("AFK Threshold (seconds)", AFKThreshold, 30f, 30f * 60);

        EditorGUILayout.Space();

        LogLine = EditorGUILayout.TextField("Log Line", LogLine);
        GUILayout.Label(TimeTracker.GetLogString(demoEvt));

        EditorGUILayout.Space();

        GUILayout.Label("File name arguments", EditorStyles.boldLabel);
        TimeTracker.logArgs
            .Select((kvp) => new { find = kvp.Key, replace = kvp.Value })
            .ToList()
            .ForEach((arg) => EditorGUILayout.LabelField("%" + arg.find + "%", arg.replace(demoEvt, null)));

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("Old logs won't change when changing these settings", MessageType.Warning);

    }

}

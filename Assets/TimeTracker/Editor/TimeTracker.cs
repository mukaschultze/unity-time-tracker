using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public enum TimeTrackerEvent {
    TimeTrackerStarted,
    TimeTrackerDestroyed,
    EditorStart,
    EditorClose,
    PlaymodeEnter,
    PlaymodeExit,
    PlaymodePause,
    PlaymodeUnpause,
    EditorFocus,
    EditorBlur,
    UserAFK,
    UserInteraction,
}

public class TimeTracker : ScriptableObject {

    private static TimeTracker instance;
    private static readonly Encoding encoding = Encoding.UTF8;
    public static readonly Dictionary<string, Func<TimeTrackerEvent, string, string>> logArgs;

    static TimeTracker() {
        logArgs = new Dictionary<string, Func<TimeTrackerEvent, string, string>>();
        logArgs["project"] = (evt, format) => Application.productName;
        logArgs["date"] = (evt, format) => DateTime.Now.ToString(format);
        logArgs["timeSinceStartup"] = (evt, format) => EditorApplication.timeSinceStartup.ToString(format);
        logArgs["event"] = (evt, format) => evt.ToString();
        logArgs["PID"] = (evt, format) => System.Diagnostics.Process.GetCurrentProcess().Id.ToString(format);
    }

    [InitializeOnLoadMethod]
    private static void Init() {
        instance = Resources
            .FindObjectsOfTypeAll<TimeTracker>()
            .FirstOrDefault();

        if (instance == null)
            instance = ScriptableObject.CreateInstance<TimeTracker>();

        instance.hideFlags = HideFlags.HideAndDontSave;

    }

    [SerializeField]
    private bool focused;
    [SerializeField]
    private double lastUserInteraction;
    [SerializeField]
    private bool userAFK;

    private void OnEnable() {
        EditorApplication.quitting += () => LogEvent(TimeTrackerEvent.EditorClose);
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += PlayModeChange;
        EditorApplication.pauseStateChanged += PauseChange;
        EditorApplication.modifierKeysChanged += () => lastUserInteraction = EditorApplication.timeSinceStartup;
    }

    private void OnDisable() { }

    private void Update() {
        var newFocusState = InternalEditorUtility.isApplicationActive;
        if (newFocusState != focused)
            LogEvent(newFocusState ?
                TimeTrackerEvent.EditorFocus :
                TimeTrackerEvent.EditorBlur);
        focused = newFocusState;

        if (EditorApplication.isPlaying)
            lastUserInteraction = EditorApplication.timeSinceStartup;

        var newUserAFKState = EditorApplication.timeSinceStartup - lastUserInteraction > TimeTrackerPreferences.AFKThreshold;
        if (newUserAFKState != userAFK)
            LogEvent(newUserAFKState ?
                TimeTrackerEvent.UserAFK :
                TimeTrackerEvent.UserInteraction);
        userAFK = newUserAFKState;
    }

    private void PauseChange(PauseState state) {
        switch (state) {
            case PauseState.Paused:
                LogEvent(TimeTrackerEvent.PlaymodePause);
                break;
            case PauseState.Unpaused:
                LogEvent(TimeTrackerEvent.PlaymodeUnpause);
                break;
        }
    }

    private void PlayModeChange(PlayModeStateChange mode) {
        switch (mode) {
            case PlayModeStateChange.EnteredPlayMode:
                LogEvent(TimeTrackerEvent.PlaymodeEnter);
                break;
            case PlayModeStateChange.ExitingPlayMode:
                LogEvent(TimeTrackerEvent.PlaymodeExit);
                break;
        }
    }

    private void Awake() {
        LogEvent(TimeTrackerEvent.EditorStart);
        LogEvent(TimeTrackerEvent.TimeTrackerStarted);
    }

    private void OnDestroy() {
        LogEvent(TimeTrackerEvent.TimeTrackerDestroyed);
    }

    private static readonly Regex regex = new Regex("%(?<name>.+?)(:(?<format>.+?))?%", RegexOptions.Compiled);

    public static string GetLogString(TimeTrackerEvent evt) {
        return regex.Replace(TimeTrackerPreferences.LogLine, (match) => {
            var name = match.Groups["name"].ToString();
            var format = match.Groups["format"].ToString();

            try {
                return logArgs.ContainsKey(name) ?
                    logArgs[name](evt, format) :
                    match.Value;
            } catch {
                return match.Value;
            }
        });
    }

    private static void LogEvent(TimeTrackerEvent evt) {
        LogFile(GetLogString(evt));
    }

    private static void LogFile(string format, params object[] args) {
        try {
            using(var writer = new StreamWriter(TimeTrackerPreferences.LogFilePath, true, encoding)) {
                writer.WriteLine(format, args);
            }
        } catch (Exception ex) {
            Debug.LogException(ex);
            Debug.LogWarningFormat("Failed to write log to file \"{0}\"", TimeTrackerPreferences.LogFilePath);
        }
    }

}

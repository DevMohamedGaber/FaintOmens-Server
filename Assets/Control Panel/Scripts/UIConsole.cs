using UnityEngine;
using System.Collections.Generic;
namespace GameServer.ControlPanel
{
    public class UIConsole : MonoBehaviour {
        public int height = 150;
        public int maxLogCount = 50;
        Queue<LogEntry> log = new Queue<LogEntry>();
        Vector2 scroll = Vector2.zero;
    #if !UNITY_EDITOR
        void Awake() => Application.logMessageReceived += OnLog;
    #endif
        void OnLog(string message, string stackTrace, LogType type) {
            if(/*(type == LogType.Error || type == LogType.Exception) && */!string.IsNullOrWhiteSpace(stackTrace))
                message += "\n" + stackTrace;

            log.Enqueue(new LogEntry(message, type));
            if (log.Count > maxLogCount)
                log.Dequeue();
            
            scroll.y = float.MaxValue;
            //Debug.Log(log.Count);
        }
        void OnGUI() {
            scroll = GUILayout.BeginScrollView(scroll, "Box", GUILayout.Width(Screen.width), GUILayout.Height(height));
            foreach(LogEntry entry in log) {
                if (entry.type == LogType.Error || entry.type == LogType.Exception)
                    GUI.color = Color.red;
                else if (entry.type == LogType.Warning)
                    GUI.color = Color.yellow;
                GUILayout.Label(entry.message);
                GUI.color = Color.white;
            }
            GUILayout.EndScrollView();
        }
    }
    struct LogEntry {
        public string message;
        public LogType type;
        public LogEntry(string message, LogType type) {
            this.message = message;
            this.type = type;
        }
    }
}
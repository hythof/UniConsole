using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class UniConsole : EditorWindow
{
    bool isInit = false;
    Logger logger = new Logger();
    Render render = new Render();

    [MenuItem("Window/UniConsole")]
    static void show()
    {
        GetWindow<UniConsole>().Show();
    }

    void OnGUI()
    {
        if(Application.isPlaying)
        {
            Application.RegisterLogCallback(addLog);
            if(!isInit)
            {
                isInit = true;
                logger.Clear();
            }
        }
        else
        {
            isInit = false;
        }

        var action = render.OnGUI(position, logger.Lines());
        switch(action)
        {
		case Action.LogClear:
            logger.Clear();
            break;
		case Action.LogFilter:
            logger.Filter(render.Word);
            break;
        }
        this.title = "UniConsole " + logger.Count();
    }

    void addLog(string condition, string stackTrace, LogType type)
    {
        switch(type)
        {
            case LogType.Error:
                Logger.Error(condition, stackTrace);
                break;
            case LogType.Exception:
                Logger.Error(condition, stackTrace);
                break;
            case LogType.Assert:
                Logger.Error(condition, stackTrace);
                break;
            case LogType.Warning:
                Logger.Warn(condition, stackTrace);
                break;
            default:
                Logger.Debug(condition, stackTrace);
                break;
        }
    }
}

enum Action {
	Noop,
    LogClear,
    LogFilter,
}

class Render
{
    public string Word = string.Empty;
    Vector2 scroll = Vector2.zero;
    GUIStyle evenBackground;
    GUIStyle selectBackground;
    GUIStyle messageStyle;
    bool isInit = false;
    bool showLevel = false;
    bool showTime = false;
    bool showScene = false;
    bool showFile = false;
    bool showMethod = false;
    int selectedIndex = 0;
    Action action = Action.Noop;

    public Render()
    {
    }

    GUIStyle scrollStyle(float r, float g, float b)
    {
        var style = new GUIStyle(GUI.skin.scrollView);
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, new Color(r, g, b));
        tex.Apply();
        style.normal.background = tex;
        return style;
    }

    public Action OnGUI(Rect pos, List<Log> logs)
    {
        if(!isInit)
        {
            evenBackground = scrollStyle(0.28f, 0.28f, 0.28f);
            selectBackground = scrollStyle(0.0f, 0.0f, 0.0f);
            messageStyle = new GUIStyle(GUI.skin.label);
            messageStyle.richText = true;
            isInit = true;
        }
        action = Action.Noop;
        search();
        option();
        table(pos, logs);
		return action;
    }

    void search()
    {
        GUILayout.BeginHorizontal();
        var word = GUILayout.TextField(Word);
        if(word != Word)
        {
            Word = word;
            action = Action.LogFilter;
        }
		if(GUILayout.Button("Clear", GUILayout.Width(60)))
        {
            action = Action.LogClear;
        }
        GUILayout.EndHorizontal();
    }
    
    void option()
    {
        GUILayout.BeginHorizontal();
        showLevel = GUILayout.Toggle(showLevel, "Level", GUILayout.Width(50));
        showScene = GUILayout.Toggle(showScene, "Scene", GUILayout.Width(50));
        showTime = GUILayout.Toggle(showTime, "Time", GUILayout.Width(45));
        showFile = GUILayout.Toggle(showFile, "File", GUILayout.Width(35));
        showMethod = GUILayout.Toggle(showMethod, "Method", GUILayout.Width(60));
        GUILayout.EndHorizontal();
    }

    void table(Rect pos, List<Log> logs)
    {
        int w1 = 0;
        int w2 = 0;
		foreach (var log in logs) {
            int x1 = (int)GUI.skin.label.CalcSize(new GUIContent(log.Time)).x;
            int x2 = (int)GUI.skin.label.CalcSize(new GUIContent(log.Scene)).x;
            w1 = w1 < x1 ? x1 : w1;
            w2 = w2 < x2 ? x2 : w2;
        }
        scroll = GUILayout.BeginScrollView(scroll);
        var regex = new Regex(Word,
                RegexOptions.Compiled |
                RegexOptions.IgnoreCase);
        for(int i=0; i<logs.Count; ++i)
        {
            var log = logs[i];
            if(i == selectedIndex)
            {
                GUILayout.BeginHorizontal(selectBackground);
            }
            else if(i % 2 == 0)
            {
                GUILayout.BeginHorizontal(evenBackground);
            }
            else
            {
                GUILayout.BeginHorizontal();
            }
            if(showLevel)
            {
                GUILayout.Label(log.LevelWord, GUILayout.Width(16));
            }
            if(showTime)
            {
                GUILayout.Label(log.Time, GUILayout.Width(w1));
            }
            if(showScene)
            {
                GUILayout.Label(log.Scene, GUILayout.Width(w2));
            }
            //if(showFile)
            //{
            //    GUILayout.Label(log.File, GUILayout.Width(w3));
            //}
            //if(showMethod)
            //{
            //    GUILayout.Label(log.Method, GUILayout.Width(w4));
            //}
            var msg = string.IsNullOrEmpty(Word)
                ? log.Message
                : regex.Replace(log.Message, "<color=#ffff33>$&</color>");
            if(GUILayout.Button(msg, messageStyle))
            {
                selectedIndex = i;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }
}

class Logger
{
    static Stopwatch stopwatch = Stopwatch.StartNew();
    static List<Log> all = new List<Log>();
    static List<Log> filtered = new List<Log>();
    static Regex filterRegex = null;
    static int filterLevel = Log.All;
 
    public static void Debug(string message, string stack) { Write(Log.Debug, message, stack); }
    public static void Warn(string message, string stack)  { Write(Log.Warn,  message, stack); }
    public static void Error(string message, string stack) { Write(Log.Error, message, stack); }

    public static void Write(int level, string message, string stack)
    {
        var log = new Log(
            level, 
            (int)stopwatch.ElapsedMilliseconds, 
            EditorApplication.currentScene,
            message,
            stack);
        all.Insert(0, log);

        if((log.Level & filterLevel) > 0 && filterRegex != null && filterRegex.IsMatch(log.Message))
        {
            filtered.Insert(0, log);
        }
    }

    public int Count()
    {
        return filtered.Count;
    }

    public void Clear()
    {
        stopwatch.Reset();
        stopwatch.Start();
        all.Clear();
        filtered.Clear();
        filtered = all;
        filterRegex = null;
    }

    public void Filter(string word)
    {
        if(string.IsNullOrEmpty(word))
        {
            filterRegex = null;
            filtered = all;
            return;
        }

        filterRegex = new Regex(word,
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase);
        filtered = new List<Log>(all.Count);
        foreach(var log in all)
        {
            if((log.Level & filterLevel) > 0 && filterRegex.IsMatch(log.Message))
            {
                filtered.Add(log);
            }
        }
    }

    public List<Log> Lines()
    {
        return filtered;
    }
}

struct Log
{
    public const int Debug = 1;
    public const int Warn = 1 << 1;
    public const int Error = 1 << 2;
    public const int All = Debug + Warn + Error;
    public readonly int Level;
    public readonly string Time;
    public readonly string Scene;
    public readonly string LevelWord;
    public readonly string Message;
    public readonly string Stack;

    public Log(int level, int time, string scene, string message, string stack)
    {
        Time = string.Format("{0:#,0}ms", time);
        Scene = scene;
        LevelWord = level == Debug ? "D" : level == Warn ? "W" : "E";
        Level = level;
        Message = message;
        Stack = stack;
    }
}
#endif

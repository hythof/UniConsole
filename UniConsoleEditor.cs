using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[ExecuteInEditMode()]
public class UniConsoleEditor : EditorWindow
{
    bool isInit = false;
    Stopwatch delayWatch = new Stopwatch();
    System.Action delayAction = () => {};
    Logger logger = new Logger();
    Render render = new Render();

    [MenuItem("Window/UniConsole")]
    static void show()
    {
        GetWindow<UniConsoleEditor>().Show();
    }

    void OnGUI()
    {
        if (Application.isPlaying)
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

        try
        {
            var action = render.OnGUI(position, logger.Lines());
            switch (action)
            {
                case UniConsoleAction.LogClear:
                    logger.Clear();
                    break;
                case UniConsoleAction.LogFilter:
                    delayWatch = Stopwatch.StartNew();
                    delayAction = () => logger.Filter(render.Word);
                    break;
            }

            if(delayWatch.ElapsedMilliseconds >= 1000)
            {
                delayWatch.Reset();
                delayAction();
                delayAction = () => {};
            }
        }
        catch(System.Exception ex)
        {
            EditorGUILayout.SelectableLabel(ex.Message + "\n" + ex.StackTrace);
        }

        this.title = "UniConsole" + logger.Count();
    }

    void Update()
    {
        Repaint();
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

enum UniConsoleAction {
	Noop,
    LogClear,
    LogFilter,
}

class Render
{
    public string Word = string.Empty;
    Vector2 scroll = Vector2.zero;
    GUIStyle logEvenHorizontalStyle;
    GUIStyle fileOddHorizontalStyle;
    GUIStyle fileEvenHorizontalStyle;
    GUIStyle selectHorizontalStyle;
    GUIStyle labelButtonStyle;
    bool isInit = false;
    bool isShowLevel = false;
    bool isShowTime = false;
    bool isShowScene = false;
    bool isShowFile = false;
    bool isShowMethod = false;
    int logSelectedIndex = -1;
    int fileSelectedIndex = -1;
    UniConsoleAction action = UniConsoleAction.Noop;
    Log current;

    GUIStyle scrollStyle(float r, float g, float b)
    {
        var style = new GUIStyle(GUI.skin.scrollView);
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, new Color(r, g, b));
        tex.Apply();
        style.normal.background = tex;
        return style;
    }

    public UniConsoleAction OnGUI(Rect pos, List<Log> logs)
    {
        if(!isInit)
        {
            logEvenHorizontalStyle = scrollStyle(0.28f, 0.28f, 0.28f);
            selectHorizontalStyle = scrollStyle(0.3f, 0.4f, 0.8f);
            fileEvenHorizontalStyle = scrollStyle(0.33f, 0.33f, 0.33f);
            fileOddHorizontalStyle = scrollStyle(0.38f, 0.38f, 0.38f);
            labelButtonStyle = new GUIStyle(GUI.skin.label);
            labelButtonStyle.richText = true;
            isInit = true;
        }
        action = UniConsoleAction.Noop;

        showSearch();
        showOption();
        showTable(pos, logs);
        return action;
    }

    void showSplit()
    {
        GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
    }

    void showSearch()
    {
        GUILayout.BeginHorizontal();
        var word = GUILayout.TextField(Word);
        if(word != Word)
        {
            Word = word;
            action = UniConsoleAction.LogFilter;
        }
		if(GUILayout.Button("Clear", GUILayout.Width(60)))
        {
            action = UniConsoleAction.LogClear;
        }
        GUILayout.EndHorizontal();
    }
    
    void showOption()
    {
        GUILayout.BeginHorizontal();
        isShowLevel = GUILayout.Toggle(isShowLevel, "Level", GUILayout.Width(50));
        isShowScene = GUILayout.Toggle(isShowScene, "Scene", GUILayout.Width(50));
        isShowTime = GUILayout.Toggle(isShowTime, "Time", GUILayout.Width(45));
        isShowFile = GUILayout.Toggle(isShowFile, "File", GUILayout.Width(35));
        isShowMethod = GUILayout.Toggle(isShowMethod, "Method", GUILayout.Width(60));
        GUILayout.EndHorizontal();
    }

    void showTable(Rect pos, List<Log> logs)
    {
        int w1 = 0;
        int w2 = 0;
        int w3 = 0;
        int w4 = 0;
        foreach (var log in logs) {
            int x1 = (int)GUI.skin.label.CalcSize(new GUIContent(log.Time)).x;
            int x2 = (int)GUI.skin.label.CalcSize(new GUIContent(log.Scene)).x;
            int x3 = (int)GUI.skin.label.CalcSize(new GUIContent(log.FileLine)).x;
            int x4 = (int)GUI.skin.label.CalcSize(new GUIContent(log.Method)).x;
            w1 = w1 < x1 ? x1 : w1;
            w2 = w2 < x2 ? x2 : w2;
            w3 = w3 < x3 ? x3 : w3;
            w4 = w4 < x4 ? x4 : w4;
        }
        scroll = GUILayout.BeginScrollView(scroll);
        var regex = new Regex(Word,
                RegexOptions.Compiled |
                RegexOptions.IgnoreCase);
        current = Log.Empty;
        var ws0 = GUILayout.Width(16);
        var ws1 = GUILayout.Width(w1);
        var ws2 = GUILayout.Width(w2);
        var ws3 = GUILayout.Width(w3);
        var ws4 = GUILayout.Width(w4);
        for (int i=0; i<logs.Count; ++i)
        {
            var log = logs[i];
            var msg = string.IsNullOrEmpty(Word)
                ? log.Message
                : regex.Replace(log.Message, "<color=#ffff33>$&</color>");
            var file = string.IsNullOrEmpty(Word) || !isShowFile
                ? log.FileLine
                : regex.Replace(log.FileLine, "<color=#ffff33>$&</color>");
            var method = string.IsNullOrEmpty(Word) || !isShowMethod
                ? log.Method
                : regex.Replace(log.Method, "<color=#ffff33>$&</color>");

            if (i == logSelectedIndex)
            {
                current = log;
                GUILayout.BeginHorizontal(selectHorizontalStyle);
            }
            else if(i % 2 == 0)
            {
                GUILayout.BeginHorizontal(logEvenHorizontalStyle);
            }
            else
            {
                GUILayout.BeginHorizontal();
            }

            if(isShowLevel)
            {
                GUILayout.Label(log.LevelWord, ws0);
            }
            if(isShowTime)
            {
                GUILayout.Label(log.Time, ws1);
            }
            if(isShowScene)
            {
                GUILayout.Label(log.Scene, ws2);
            }
            if(isShowFile)
            {
                if(GUILayout.Button(file, labelButtonStyle, ws3))
                {
                    logSelectedIndex = i;
                    fileSelectedIndex = -1;
                }
            }
            if(isShowMethod)
            {
                if(GUILayout.Button(method, labelButtonStyle, ws4))
                {
                    logSelectedIndex = i;
                    fileSelectedIndex = -1;
                }
            }
            if(GUILayout.Button(msg, labelButtonStyle))
            {
                logSelectedIndex = i;
                fileSelectedIndex = -1;
            }

            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();

        showSplit();
        showStack();
    }

    void showStack()
    {
        if(current.Stacks.Count == 0)
        {
            return;
        }

        int w1 = 0;
        foreach (var stack in current.Stacks)
        {
            int x1 = (int)GUI.skin.label.CalcSize(new GUIContent(stack.FileLine)).x;
            w1 = w1 < x1 ? x1 : w1;
        }

        var ws0 = GUILayout.Width(50);
        var ws1 = GUILayout.Width(w1);
        for(int i=0; i<current.Stacks.Count; ++i)
        {
            var stack = current.Stacks[i];
            if(fileSelectedIndex == i)
            {
                GUILayout.BeginHorizontal(selectHorizontalStyle);
            }
            else if(i % 2 == 0)
            {
                GUILayout.BeginHorizontal(fileEvenHorizontalStyle);
            }
            else
            {
                GUILayout.BeginHorizontal(fileOddHorizontalStyle);
            }

            GUILayout.Space(10);
            if (
                GUILayout.Button(stack.FileLine, labelButtonStyle, ws1) |
                GUILayout.Button(stack.LineCode(), labelButtonStyle)
            )
            {
                UnityEngine.Object _asset = AssetDatabase.LoadAssetAtPath(stack.Path, typeof(Object)) as Object;
                AssetDatabase.OpenAsset(_asset, stack.Line);
                fileSelectedIndex = i;
            }
            GUILayout.EndHorizontal();
        }
    }
}

class Logger
{
    const int LogLimit = 10 * 1000;
    const int ShowLogLimit = 300;
    readonly static Stopwatch stopwatch = Stopwatch.StartNew();
    readonly static List<Log> all = new List<Log>();
    readonly static List<Log> filtered = new List<Log>();
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
        if(all.Count > LogLimit)
        {
            all.RemoveAt(all.Count - 1);
        }

        if((log.Level & filterLevel) > 0 && log.IsMatch(filterRegex))
        {
            filtered.Insert(0, log);
            if (filtered.Count > ShowLogLimit)
            {
                filtered.RemoveAt(filtered.Count - 1);
            }
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
        filterRegex = null;
    }

    public void Filter(string word)
    {
        filtered.Clear();

        if (string.IsNullOrEmpty(word))
        {
            int count = all.Count > ShowLogLimit ? ShowLogLimit : all.Count;
            filterRegex = null;
            for(int i=0; i<count; ++i)
            {
                filtered.Add(all[i]);
            }
            return;
        }

        filterRegex = new Regex(word,
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase);
        foreach(var log in all)
        {
            if((log.Level & filterLevel) > 0 && log.IsMatch(filterRegex))
            {
                filtered.Add(log);
                if(filtered.Count >= ShowLogLimit)
                {
                    break;
                }
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
    public static readonly Log Empty = new Log(0, 0, string.Empty, string.Empty, string.Empty);
    const string none = "(-)";
    public const int Debug = 1;
    public const int Warn = 1 << 1;
    public const int Error = 1 << 2;
    public const int All = Debug + Warn + Error;
    public readonly int Level;
    public readonly string Time;
    public readonly string Scene;
    public readonly string LevelWord;
    public readonly string Message;
    public readonly string FileLine;
    public readonly string Method;
    public readonly string Stack;
    public readonly List<StackLine> Stacks;

    public Log(int level, int time, string scene, string message, string stack)
    {
        Time = string.Format("{0:#,0}ms", time);
        Scene = scene;
        LevelWord = level == Debug ? "D" : level == Warn ? "W" : "E";
        Level = level;
        Message = message;
        Stack = stack;
        Stacks = new List<StackLine>();

        var lines = stack.Split('\n');
        foreach(var line in lines)
        {
            var method_file = line.Split(new string[] { ") (at " }, 2, System.StringSplitOptions.RemoveEmptyEntries);
            if (method_file.Length >= 2)
            {
                var method = method_file[0];
                var file = method_file[1];
                var stackLine = new StackLine(
                    file.Substring(0, file.Length - 1),
                    method.Substring(0, method.LastIndexOf('(')) + "()");
                Stacks.Add(stackLine);
            }
        }

        if(Stacks.Count > 0)
        {
            var top = Stacks[0];
            Method = top.Method;
            FileLine = top.FileLine;
        }
        else
        {
            Method = none;
            FileLine = none;
        }
    }

    public bool IsMatch(Regex r)
    {
        return r == null || (r.IsMatch(Message) || r.IsMatch(FileLine) || r.IsMatch(Method));
    }
}

struct StackLine
{
    public readonly string FileLine;
    public readonly string File;
    public readonly string Path;
    public readonly int Line;
    public readonly string Method;
    string lineCode;

    public StackLine(string file, string method)
    {
        var fx = file.Split(':');
        if(fx.Length >= 2)
        {
            Path = fx[0];
            File = System.IO.Path.GetFileName(Path);
            if(!int.TryParse(fx[1], out Line))
            {
                Line = 1;
            }
        }
        else
        {
            Path = string.Empty;
            File = string.Empty;
            Line = 1;
        }
        Method = method;
        lineCode = string.Empty;
        FileLine = File + ":" + Line ;
    }

    public string LineCode()
    {
        if (string.IsNullOrEmpty(lineCode))
        {
            try
            {
                var code = AssetDatabase.LoadAssetAtPath(Path, typeof(TextAsset)) as TextAsset;
                lineCode = code.text.Split('\n')[Line - 1].Trim();
            }
            catch (System.Exception ex)
            {
                return ex.Message;
            }
        }

        return lineCode;
    }
}
#endif

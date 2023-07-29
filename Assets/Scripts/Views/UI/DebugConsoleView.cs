using System.Collections.Generic;
using UnityEngine;

namespace Views.UI
{
    public class DebugConsoleView : MonoBehaviour
    {
        [SerializeField] private float height;
        [SerializeField] private float commandHeight = 22;
        [SerializeField] private float width = 600;
        [SerializeField] private float upperLogHeight = 333;
        [SerializeField] private Texture2D _background;
        [field: SerializeField] public int LoggedStringWidth { get; private set; } = 13;
        
        private Vector2 _scroll;
        private readonly GUIStyle _guiStyle = new GUIStyle();
        private readonly List<LoggedString> _log = new List<LoggedString>();
        
        private bool _previousInput;
        
        public bool ConsoleShown { get; private set; }
        public string Input { get; private set; }
        
        public bool UpdateConsoleState()
            => ConsoleShown = !ConsoleShown;

        public void ResetInput()
            => Input = "";

        public void SetInput(string previous)
        {
            Input = previous;
            _previousInput = true;
        }

        public void Log(params LoggedString[] logged)
        {
            foreach (LoggedString loggedString in logged)
                _log.Add(loggedString);

            _scroll = new Vector2(0, _log.Count * commandHeight);
        }

        private void OnGUI()
        {
            if (!ConsoleShown)
                return;

            float y = Screen.height / height;

            GUI.Box(new Rect(0, y, width, upperLogHeight), "");
            Rect viewport = new Rect(0, 0, width - 30, commandHeight * _log.Count);
            _scroll = GUI.BeginScrollView(new Rect(0, y + 5f, width, upperLogHeight - 10), _scroll, viewport);

            for (int i = 0; i < _log.Count; i++)
            {
                Rect labelRect = new Rect(5, commandHeight * i, viewport.width - 100, commandHeight);
                _guiStyle.normal.textColor = _log[i].Color;
                GUI.Label(labelRect, _log[i].String, _guiStyle);
            }

            GUI.EndScrollView();
            y += upperLogHeight;

            GUI.Box(new Rect(0, y, width, 30), "");
            GUI.backgroundColor = new Color32(0, 0, 0, 255);

            GUI.SetNextControlName("console");
            Input = GUI.TextField(new Rect(10f, y + 5f, width - 20f, 20f), Input);

            GUI.FocusControl("console");
            GUI.skin.box.normal.background = _background;

            if (!_previousInput)
                return;

            _previousInput = false;

            TextEditor textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            textEditor.MoveTextEnd();
        }
    }

    public class LoggedString
    {
        public readonly string String;
        public readonly Color Color;

        public LoggedString(string s, Color color)
        {
            String = s;
            Color = color;
        }
    }
}
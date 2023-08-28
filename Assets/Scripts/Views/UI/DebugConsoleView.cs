using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Views.UI
{
    public class DebugConsoleView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _logText;
        [field: SerializeField] public TMP_InputField InputField { get; private set; }
        [field: SerializeField] public Button OptionPrefab { get; private set; }
        [field: SerializeField] public GameObject Options { get; private set; }
        
        private Vector2 _scroll;
        private readonly List<LoggedString> _log = new List<LoggedString>();
        
        public bool ConsoleShown { get; private set; }
        
        public bool UpdateConsoleState()
        {
            gameObject.SetActive(!ConsoleShown);
            InputField.ActivateInputField();
            return ConsoleShown = !ConsoleShown;
        }

        public void ResetInput()
        {
            InputField.text = "";
            InputField.ActivateInputField();
        }

        public async void SetInput(string text)
        {
            InputField.ActivateInputField();
            InputField.text = text;
            await System.Threading.Tasks.Task.Delay(10);
            InputField.MoveToEndOfLine(true, false);
        }

        public void Log(params LoggedString[] logged)
        {
            foreach (LoggedString loggedString in logged)
            {
                _log.Add(loggedString);
                string hexColor = ColorUtility.ToHtmlStringRGB(loggedString.Color);
                _logText.text += $"\n<color=#{hexColor}>{loggedString.String}</color>";
            }

            RectTransform rect = _logText.rectTransform;
            rect.anchoredPosition = new Vector3(rect.anchoredPosition.x, 0f);
        }

        private void OnGUI()
        {
            // if (!ConsoleShown)
            //     return;
            //
            // float y = Screen.height / height;
            //
            // GUI.Box(new Rect(0, y, width, upperLogHeight), "");
            // Rect viewport = new Rect(0, 0, width - 30, commandHeight * _log.Count);
            // _scroll = GUI.BeginScrollView(new Rect(0, y + 5f, width, upperLogHeight - 10), _scroll, viewport);
            //
            // for (int i = 0; i < _log.Count; i++)
            // {
            //     Rect labelRect = new Rect(5, commandHeight * i, viewport.width - 100, commandHeight);
            //     _guiStyle.normal.textColor = _log[i].Color;
            //     GUI.Label(labelRect, _log[i].String, _guiStyle);
            // }
            //
            // GUI.EndScrollView();
            // y += upperLogHeight;
            //
            // GUI.Box(new Rect(0, y, width, 30), "");
            // GUI.backgroundColor = new Color32(0, 0, 0, 255);
            //
            // GUI.SetNextControlName("console");
            // Input = GUI.TextField(new Rect(10f, y + 5f, width - 20f, 20f), Input);
            //
            // GUI.FocusControl("console");
            // GUI.skin.box.normal.background = _background;
            //
            // if (!_previousInput)
            //     return;
            //
            // _previousInput = false;
            //
            // TextEditor textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            // textEditor.MoveTextEnd();
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
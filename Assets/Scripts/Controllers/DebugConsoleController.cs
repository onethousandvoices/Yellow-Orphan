using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Views.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace YellowOrphan.Controllers
{
    public class DebugConsoleController : IInitializable, IConsoleHandler
    {
        [Inject] private DebugConsoleView _view;
        [Inject] private IPlayerState _playerState;

        private List<DebugCommandBase> _commands;
        private readonly List<Button> _options = new List<Button>();
        private readonly List<string> _previousCommands = new List<string>();
        private int _commandsPointer = -1;
        private int _optionsPointer = -1;

        public bool ConsoleShown => _view.ConsoleShown;

        public void Initialize()
        {
            _view.InputField.onValueChanged.AddListener(CreateOptions);

            DebugCommand help = new DebugCommand("help", "List of all commands", Help);
            DebugCommand testException = new DebugCommand("exception", "Test exception", TestException);
            DebugCommand<int> setFps = new DebugCommand<int>("fps_", "Set fps (0 - uncapped)", FrameRateChange);
            DebugCommand<float> setTimeScale = new DebugCommand<float>("time_", "Set time scale 0 - 100", TimeScaleChange);

            _commands = new List<DebugCommandBase>
            {
                help,
                testException,
                setFps,
                setTimeScale,
            };
        }

        private void CreateOptions(string text)
        {
            ClearOptions();

            if (string.IsNullOrEmpty(text))
                return;

            foreach (DebugCommandBase command in _commands)
            {
                if (!command.Id.ToLower().Contains(text.ToLower()))
                    continue;

                Button newOption = Object.Instantiate(_view.OptionPrefab, _view.OptionPrefab.transform.parent);
                newOption.GetComponentInChildren<TextMeshProUGUI>().text = command.Id;
                newOption.onClick.AddListener(() =>
                {
                    _view.SetInput(command.Id);
                    _view.Options.SetActive(false);
                });
                newOption.gameObject.SetActive(true);
                _options.Add(newOption);
            }

            _view.Options.SetActive(_options.Count > 0);
        }

        private void ClearOptions()
        {
            foreach (Button button in _options)
                Object.Destroy(button.gameObject);

            _options.Clear();
        }

        private void HandeInput()
        {
            if (!_view.ConsoleShown || string.IsNullOrEmpty(_view.InputField.text))
                return;

            string inputText = _view.InputField.text.ToLower();

            _previousCommands.Add(inputText);

            string properties = string.Concat(inputText.SkipWhile(x => x != '_').Skip(1));

            foreach (DebugCommandBase command in _commands)
            {
                if (!inputText.Contains(command.Id.ToLower()))
                    continue;

                switch (command)
                {
                    case DebugCommand debugCommand:
                        debugCommand.Invoke();
                        break;
                    case DebugCommand<int> debugCommandInt:
                        int.TryParse(properties, out int intResult);
                        debugCommandInt.Invoke(intResult);
                        break;
                    case DebugCommand<float> debugCommandFloat:
                        float.TryParse(properties, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out float floatResult);
                        debugCommandFloat.Invoke(floatResult);
                        break;
                    case DebugCommand<string> debugCommandString:
                        debugCommandString.Invoke(properties);
                        break;
                }
            }
        }

        private void AppLog(string condition, string stacktrace, LogType type)
        {
            Color color = new Color();

            switch (type)
            {
                case LogType.Error:
                    color = Color.red;
                    break;
                case LogType.Warning:
                    color = Color.yellow;
                    break;
                case LogType.Log:
                    color = Color.white;
                    break;
                case LogType.Exception:
                    color = Color.red;
                    break;
            }
            _view.Log(new LoggedString(condition, color));
        }

        private void Help()
            => _view.Log(_commands.Select(x => new LoggedString($"{x.Id} - {x.Description}", Color.green)).ToArray());

        private static void TestException()
            => throw new Exception("TEST EXCEPTION");

        private static void FrameRateChange(int fps)
        {
            Application.targetFrameRate = fps;
            Debug.Log($"Fps set to {Application.targetFrameRate}");
        }

        private static void TimeScaleChange(float time)
        {
            Time.timeScale = time;
            Debug.Log($"Time scale set to {time}");
        }

        public void ShowConsole()
        {
            _view.ResetInput();
            _playerState.InputBlocked = _view.UpdateConsoleState();
            _commandsPointer = -1;
            _optionsPointer = -1;
        }

        public void SubscribeToLog()
            => Application.logMessageReceived += AppLog;

        public void OnReturn()
        {
            if (!string.IsNullOrEmpty(_view.InputField.text) && _optionsPointer > -1)
            {
                _options[_optionsPointer].onClick.Invoke();
                _optionsPointer = -1;
                return;
            }
            
            HandeInput();
            _view.ResetInput();
            _commandsPointer = -1;
            _optionsPointer = -1;
        }

        public void OnUpArrow()
        {
            if (_optionsPointer > -1)
            {
                _optionsPointer--;

                if (_optionsPointer < 0)
                    _optionsPointer = -1;
                
                HighlightOption();
                return;
            }

            if (!string.IsNullOrEmpty(_view.InputField.text) || _previousCommands.Count < 1)
                return;
            
            switch (_commandsPointer)
            {
                case -1:
                    _commandsPointer = _previousCommands.Count - 1;
                    break;
                case 0:
                    break;
                default:
                    _commandsPointer--;
                    break;
            }
            _view.SetInput(_previousCommands[_commandsPointer]);
        }

        public void OnDownArrow()
        {
            _optionsPointer++;
            
            if (_optionsPointer >= _options.Count)
                _optionsPointer = _options.Count - 1;

            HighlightOption();
        }

        private void HighlightOption()
        {
            for (int i = 0; i < _options.Count; i++)
            {
                if (i != _optionsPointer)
                {
                    _options[i].GetComponent<Image>().color = _view.OptionPrefab.GetComponent<Image>().color;
                    continue;
                }
                _options[i].GetComponent<Image>().color = _view.OptionPrefab.colors.selectedColor;
            }
        }

        public void AddCommand(DebugCommandBase commandBase)
            => _commands.Add(commandBase);
    }

    public interface IConsoleHandler
    {
        public bool ConsoleShown { get; }
        
        public void ShowConsole();
        public void SubscribeToLog();
        public void OnReturn();
        public void OnUpArrow();
        public void OnDownArrow();
        public void AddCommand(DebugCommandBase commandBase);
    }

    public abstract class DebugCommandBase
    {
        public readonly string Id;
        public readonly string Description;

        protected DebugCommandBase(string id, string description)
        {
            Id = id;
            Description = description;
        }
    }

    public class DebugCommand<T> : DebugCommandBase
    {
        private readonly Action<T> _command;

        public DebugCommand(string id, string description, Action<T> command) : base(id, description)
            => _command = command;

        public void Invoke(T value)
        {
            try
            {
                _command?.Invoke(value);
            }
            catch (Exception)
            {
                Debug.LogError("Something went wrong...");
            }
        }
    }

    public class DebugCommand : DebugCommandBase
    {
        private readonly Action _command;

        public DebugCommand(string id, string description, Action command) : base(id, description)
            => _command = command;

        public void Invoke()
        {
            try
            {
                _command?.Invoke();
            }
            catch (Exception)
            {
                Debug.LogError("Something went wrong...");
            }
        }
    }
}
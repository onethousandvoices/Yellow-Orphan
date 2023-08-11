using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Views.UI;
using YellowOrphan.Player;
using YellowOrphan.Utility;
using Zenject;

namespace YellowOrphan.Controllers
{
    public class DebugConsoleController : IInitializable, IConsoleHandler
    {
        [Inject] private DebugConsoleView _view;
        [Inject] private IPlayerState _playerState;

        private List<DebugCommandBase> _commands;
        private readonly List<string> _previousCommands = new List<string>();
        private int _commandsPointer;

        public void Initialize()
        {
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

        private void HandeInput()
        {
            if (!_view.ConsoleShown)
                return;

            if (!string.IsNullOrEmpty(_view.Input)) 
                _previousCommands.Add(_view.Input);
            string properties = string.Concat(_view.Input.SkipWhile(x => x != '_').Skip(1));

            foreach (DebugCommandBase command in _commands)
            {
                if (!_view.Input.Contains(command.Id))
                    continue;

                switch (command)
                {
                    case DebugCommand debugCommand:
                        debugCommand.Invoke();
                        break;
                    case DebugCommand<int> debugCommandInt:
                        debugCommandInt.Invoke(int.Parse(properties));
                        break;
                    case DebugCommand<float> debugCommandFloat:
                        debugCommandFloat.Invoke(float.Parse(properties, CultureInfo.InvariantCulture.NumberFormat));
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

            IEnumerable<string> splitInParts = condition.SplitInParts(_view.LoggedStringWidth);

            foreach (string s in splitInParts)
                _view.Log(new LoggedString(s, color));
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
        }

        public void SubscribeToLog()
            => Application.logMessageReceived += AppLog;

        public void OnReturn()
        {
            HandeInput();
            _view.ResetInput();
            _commandsPointer = -1;
        }

        public void OnUpArrow()
        {
            switch (_commandsPointer)
            {
                case -1:
                    _commandsPointer = _previousCommands.Count - 1;
                    break;
                case 0:
                    return;
                default:
                    _commandsPointer--;
                    break;
            }
            _view.SetInput(_previousCommands[_commandsPointer]);
        }

        public void AddCommand(DebugCommandBase commandBase)
            => _commands.Add(commandBase);
    }

    public interface IConsoleHandler
    {
        public void ShowConsole();
        public void SubscribeToLog();
        public void OnReturn();
        public void OnUpArrow();
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
            => _command?.Invoke(value);
    }

    public class DebugCommand : DebugCommandBase
    {
        private readonly Action _command;

        public DebugCommand(string id, string description, Action command) : base(id, description)
            => _command = command;

        public void Invoke()
            => _command?.Invoke();
    }
}
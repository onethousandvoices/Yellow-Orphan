using System;
using UnityEngine;
using Views.UI;
using Zenject;

namespace YellowOrphan.Controllers
{
    public class GameController : IInitializable
    {
        [Inject] private IConsoleHandler _consoleHandler;
        [Inject] private ITimeTickable _timeTickable;
        [Inject] private CMDebugCamera _cmDebugCamera;

        public void Initialize()
        {
            _consoleHandler.SubscribeToLog();
            _timeTickable.AddTickable(0.01f, CheckCursor);
            
            _consoleHandler.AddCommand(new DebugCommand("debugCam", "Toggle debug camera", 
                () => _cmDebugCamera.gameObject.SetActive(!_cmDebugCamera.gameObject.activeSelf)));
            
            Debug.Log($"Started at {DateTime.Now}");
        }
        
        private void CheckCursor()
        {
            if (_consoleHandler.ConsoleShown)
            {
                Cursor.lockState = CursorLockMode.Confined;
                return;
            }
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
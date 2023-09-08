using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Views.UI;

namespace YellowOrphan.Controllers
{
    public class GameController : IStartable
    {
        [Inject] private readonly IConsoleHandler _consoleHandler;
        [Inject] private readonly ITimeTickable _timeTickable;
        [Inject] private readonly CMDebugCamera _cmDebugCamera;

        public void Start()
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
using System;
using UnityEngine;
using YellowOrphan.Player;
using Zenject;

namespace YellowOrphan.Controllers
{
    public class GameController : IInitializable
    {
        [Inject] private IConsoleHandler _consoleHandler;
        [Inject] private IPlayerState _player;
        [Inject] private ITimeTickable _timeTickable;

        public void Initialize()
        {
            _consoleHandler.SubscribeToLog();
            _timeTickable.AddTickable(0.01f, CheckCursor);
            
            Debug.Log($"Started at {DateTime.Now}");
        }
        
        private void CheckCursor()
        {
            if (_player.InputBlocked)
            {
                Cursor.lockState = CursorLockMode.Confined;
                return;
            }
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
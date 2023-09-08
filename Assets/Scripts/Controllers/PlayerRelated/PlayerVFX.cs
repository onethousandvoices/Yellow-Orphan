using System;
using UnityEngine;
using Views;

namespace YellowOrphan.Controllers
{
    public class PlayerVFX
    {
        private readonly PlayerView _playerView;
        private readonly HookView _hookView;

        private Action<Vector3> _onSuccess;
        private RaycastHit _hit;
        private Vector3 _hookPosition;
        private Vector3 _hookTarget;

        private bool _hookActive;
        private bool _hooked;

        public PlayerVFX(PlayerView playerView, HookView hookView)
        {
            _playerView = playerView;
            _hookView = hookView;
            SetActive(false);
        }

        public void SetOnSuccessHook(Action<Vector3> onSuccess)
            => _onSuccess = onSuccess;

        public void SetActive(bool state)
        {
            _hookActive = state;
            _hooked = false;
            switch (_hookActive)
            {
                case true:
                    _hookView.Effect.enabled = true;
                    break;
                case false:
                    _hookTarget = _playerView.HookRayStart.position;
                    break;
            }
        }

        public void TryHook()
        {
            SetActive(true);
            Physics.Raycast(
                _playerView.HookRayStart.position, _playerView.HookRayStart.forward, out _hit, _playerView.HookRangeMax);

            if (_hit.transform != null)
                _hookTarget = _hit.point;
            else
                _hookTarget = _playerView.HookRayStart.forward * _playerView.HookRangeMax;
            _hookPosition = _playerView.HookRayStart.position;
        }

        public void CheckHook()
        {
            if (!_hookView.Effect.enabled)
                return;

            if (!_hooked)
                _hookPosition = Vector3.Lerp(_hookPosition, _hookTarget, _playerView.HookFlySpeed * Time.deltaTime);

            _hookView.Pos3.position = _hookPosition;
            _hookView.Pos4.position = _hookPosition;

            if ((_hookPosition - _hookTarget).sqrMagnitude > 0.1f || _hooked)
                return;
            
            if (!_hookActive)
                _hookView.Effect.enabled = false;
            else if (_hit.transform != null && _hit.distance >= _playerView.HookRangeMin)
            {
                _hookPosition = _hit.point;
                _onSuccess?.Invoke(_hookPosition);
                _hooked = true;
                // Debug.LogError("hooked");
            }
            else
                _hookTarget = _playerView.HookRayStart.position;
        }
    }
}
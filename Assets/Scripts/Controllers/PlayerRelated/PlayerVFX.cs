using UnityEngine;
using Views;

namespace YellowOrphan.Controllers
{
    public class PlayerVFX
    {
        private readonly HookView _hookView;
        private Vector3 _target;

        private bool _isActive;

        public PlayerVFX(HookView hookView)
        {
            _hookView = hookView;
            SetActive(false);
        }

        public void SetActive(bool state)
        {
            _isActive = state;
            _hookView.Effect.enabled = _isActive;
        }

        public void SetHookTarget(Vector3 target)
        {
            SetActive(true);
            _target = target;
        }

        public void CheckHook()
        {
            if (!_isActive)
                return;
            
            _hookView.Pos3.position = _target;
            _hookView.Pos4.position = _target;
        }
    }
}
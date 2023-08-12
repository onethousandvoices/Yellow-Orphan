using System.Collections;
using UnityEngine;
using Views;
using YellowOrphan.Player;

namespace YellowOrphan.Controllers
{
    public class PlayerPhysics
    {
        private SpringJoint _joint;
        private Vector3 _hookPoint;
        
        private float _currentMaxDistance;
        private bool _isHooked;

        private readonly Rigidbody _rb;
        private readonly PlayerView _view;
        private readonly IPlayerState _playerState;
        private readonly RigidbodyConstraints _rbConstraints;
        private readonly Vector3 _jointAnchor;

        private readonly float _baseRbMass;
        private readonly float _baseRbDrag;
        private readonly float _baseRbAngularDrag;
        
        public Vector3 Velocity
        {
            get => _rb.velocity;
            set => _rb.velocity = value;
        }
        
        public PlayerPhysics(PlayerView view, IPlayerState playerState)
        {
            _view = view;
            _playerState = playerState;
            _rb = _view.Rb;
            
            _rbConstraints = _rb.constraints;

            _baseRbMass = _rb.mass;
            _baseRbDrag = _rb.drag;
            _baseRbAngularDrag = _rb.angularDrag;
            
            _jointAnchor = new Vector3(0f, 0.24f, 0f);
            
            HookStop();
        }

        public void TryHook(Vector3 target)
        {
            _isHooked = true;

            SetDrag(_view.HookedRbDrag);
            SetGravity(true);
            _rb.mass = _view.HookedRbMass;
            _rb.angularDrag = _view.HookedRbAngularDrag;
            _rb.constraints = RigidbodyConstraints.None;

            _hookPoint = target;
            _currentMaxDistance = Vector3.Distance(_rb.transform.position, _hookPoint) * 0.95f;
            
            _joint = _rb.gameObject.AddComponent<SpringJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = _hookPoint;
            _joint.anchor = _jointAnchor;
            _joint.spring = _view.HookedSpring;
            _joint.damper = _view.HookedDamper;
            _joint.massScale = _view.HookedMassScale;
            _joint.minDistance = _view.HookMinLength;
            _joint.maxDistance = _currentMaxDistance;

            // Debug.LogError($"hookMaxDistance {_currentMaxDistance}");
        }

        public void CheckRbState()
        {
            if (_isHooked && _rb.IsSleeping())
                _rb.WakeUp();
        }
        
        public void HookStop()
        {
            _isHooked = false;
            
            ResetDrag();
            _rb.mass = _baseRbMass;
            _rb.angularDrag = _baseRbAngularDrag;

            _hookPoint = Vector3.zero;
            
            Object.Destroy(_joint);
        }

        public void HookDescend(float speed = 1f)
        {
            if (_joint == null)
                return;
            _joint.maxDistance =
                Mathf.Lerp(_joint.maxDistance, _currentMaxDistance, Time.deltaTime * speed);
        }

        public void HookAscend(float speed = 1f)
        {
            if (_joint == null)
                return;
            _joint.maxDistance =
                Mathf.Lerp(_joint.maxDistance, _joint.minDistance, Time.deltaTime * speed);
        }

        public void AddForce(Vector3 force, ForceMode mode)
            => _rb.AddForce(force, mode);

        public void SetGravity(bool state)
            => _rb.useGravity = state;

        public void SetDrag(float newDrag)
            => _rb.drag = newDrag;

        public void ResetDrag()
            => _rb.drag = _baseRbDrag;
        
        public IEnumerator NormalizeRotation(float time)
        {
            _playerState.InputBlocked = true;
            float t = 0f;
            Quaternion startRotation = _rb.rotation;
            Quaternion target = Quaternion.Euler(new Vector3(0f, _rb.rotation.eulerAngles.y, 0f));
            while (t < 1f)
            {
                startRotation = Quaternion.Lerp(startRotation, target, t * t);
                _rb.MoveRotation(startRotation);
                t += Time.deltaTime / time;
                yield return null;
            }
            _rb.MoveRotation(target);
            _rb.constraints = _rbConstraints;
            _playerState.InputBlocked = false;
        }
        
        public void DrawLine()
        {
            if (!_playerState.IsHooked)
            {
                _view.LineRenderer.SetPosition(0, _view.HookRayStart.position);
                _view.LineRenderer.SetPosition(1, _view.HookRayStart.position);
                return;
            }
            _view.LineRenderer.SetPosition(0, _view.HookRayStart.position);
            _view.LineRenderer.SetPosition(1, _hookPoint);
        }
    }
}
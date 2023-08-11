﻿using System.Collections;
using UnityEngine;
using Views;
using YellowOrphan.Player;

namespace YellowOrphan.Controllers
{
    public class PlayerPhysics
    {
        private SpringJoint _joint;
        private Vector3 _hookPoint;
        
        private readonly Rigidbody _rb;
        private readonly PlayerView _view;
        private readonly IPlayerState _playerState;
        private readonly RigidbodyConstraints _rbConstraints;

        private readonly float _baseRbMass;
        private readonly float _baseRbDrag;
        private readonly float _baseRbAngularDrag;

        private const float _hookedRbMass = 1f;
        private const float _hookedRbDrag = 1.5f;
        private const float _hookedRbAngularDrag = 1f;
        
        public Vector3 Velocity
        {
            get => _rb.velocity;
            set => _rb.velocity = value;
        }
        
        public bool RotationNormalizing { get; private set; }
        
        public PlayerPhysics(PlayerView view, IPlayerState playerState)
        {
            _view = view;
            _playerState = playerState;
            _rb = _view.Rb;
            
            _rbConstraints = _rb.constraints;

            _baseRbMass = _rb.mass;
            _baseRbDrag = _rb.drag;
            _baseRbAngularDrag = _rb.angularDrag;

            HookStop();
        }

        public void TryHook(Vector3 target)
        {
            SetDrag(_hookedRbDrag);
            _rb.mass = _hookedRbMass;
            _rb.angularDrag = _hookedRbAngularDrag;
            _rb.constraints = RigidbodyConstraints.None;

            _hookPoint = target;
            
            _joint = _rb.gameObject.AddComponent<SpringJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = target;
            _joint.anchor = new Vector3(0f, 1.2f, 0f);

            float distance = Vector3.Distance(_rb.transform.position, target);

            _joint.maxDistance = distance * 0.8f;
            _joint.minDistance = 0.1f;

            _joint.spring = 4.5f;
            _joint.damper = 7f;
            _joint.massScale = 14.5f;
        }

        public void HookStop()
        {
            ResetDrag();
            _rb.mass = _baseRbMass;
            _rb.angularDrag = _baseRbAngularDrag;

            _hookPoint = Vector3.zero;
            
            Object.Destroy(_rb.gameObject.GetComponent<SpringJoint>());
        }

        public void HookDescend(float maxDistance, float speed = 1f)
        {
            if (_joint == null)
                return;
            _joint.maxDistance =
                Mathf.Lerp(_joint.maxDistance, maxDistance, Time.deltaTime * speed);
        }

        public void HookClimb(float speed = 1f)
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
            RotationNormalizing = true;
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
            RotationNormalizing = false;
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
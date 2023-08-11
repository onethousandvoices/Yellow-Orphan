﻿using System;
using UnityEngine;
using Views;
using YellowOrphan.Player;

namespace Controllers
{
    public class PlayerTracks
    {
        private readonly PlayerView _view;
        private readonly IPlayerState _player;
        private readonly IPlayerMotion _playerMotion;
        private readonly float _turnSpeed;

        private float _leftMod;
        private float _rightMod;
        private float _leftTrackSpeed;
        private float _rightTrackSpeed;
        private float _previousMoveMagnitude;

        private static readonly int _trackOffset = Shader.PropertyToID("_Track_offset");

        private const float _tracksModStep = 0.1f;
        private const float _angleMinDelta = 8f;

        public PlayerTracks(PlayerView view, IPlayerState playerState, IPlayerMotion playerMotion)
        {
            _view = view;
            _player = playerState;
            _playerMotion = playerMotion;
            _turnSpeed = _view.TurnSpeed;
        }

        public void AdjustLegs()
        {
            float rayLength = _player.IsGrounded ? 0.9f : 0.5f;

            Transform leftLegTargetPosition = _view.LeftLegTargetPosition;
            Transform rightLegTargetPosition = _view.RightLegTargetPosition;
            Transform leftLegTargetRotation = _view.LeftLegTargetRotation;
            Transform rightLegTargetRotation = _view.RightLegTargetRotation;

            Physics.Raycast(new Ray(leftLegTargetRotation.position, -leftLegTargetRotation.up), out RaycastHit leftLegHit, rayLength, layerMask: _view.GroundLayers);
            Physics.Raycast(new Ray(rightLegTargetRotation.position, -rightLegTargetRotation.up), out RaycastHit rightLegHit, rayLength, layerMask: _view.GroundLayers);

            Vector3 leftLegOffset = new Vector3(0f, Vector3.Distance(_view.LeftLegBottom.position, leftLegHit.point), 0f);
            Vector3 rightLegOffset = new Vector3(0f, Vector3.Distance(_view.RightLegBottom.position, rightLegHit.point), 0f);

            Quaternion leftRotationInAir = Quaternion.Euler(new Vector3(leftLegTargetRotation.eulerAngles.x, leftLegTargetRotation.eulerAngles.y, 15f));
            Quaternion rightRotationInAir = Quaternion.Euler(new Vector3(rightLegTargetRotation.eulerAngles.x, rightLegTargetRotation.eulerAngles.y, -15f));

            Vector3 leftForward = Vector3.ProjectOnPlane(_view.transform.forward, leftLegHit.normal);
            Quaternion leftRotation = Quaternion.LookRotation(leftForward, leftLegHit.normal);

            Vector3 rightForward = Vector3.ProjectOnPlane(_view.transform.forward, rightLegHit.normal);
            Quaternion rightRotation = Quaternion.LookRotation(rightForward, rightLegHit.normal);

            if (leftLegHit.transform != null)
            {
                if (_player.IsGrounded)
                    leftLegTargetRotation.rotation = Quaternion.Lerp(leftLegTargetRotation.rotation, leftRotation, Time.deltaTime * _turnSpeed * 4f);
                leftLegTargetPosition.position = Vector3.Lerp(leftLegTargetPosition.position, leftLegHit.point + leftLegOffset, Time.deltaTime * _turnSpeed * 4f);
            }
            else if (leftLegHit.transform == null)
            {
                if (!_player.IsHooked)
                    leftLegTargetRotation.rotation = Quaternion.Lerp(leftLegTargetRotation.rotation, leftRotationInAir, Time.deltaTime * _turnSpeed / 2f);
                leftLegTargetPosition.localPosition = Vector3.Lerp(leftLegTargetPosition.localPosition, _view.LeftLegBasePos, Time.deltaTime * _turnSpeed / 2f);
            }

            if (rightLegHit.transform != null)
            {
                if (_player.IsGrounded)
                    rightLegTargetRotation.rotation = Quaternion.Lerp(rightLegTargetRotation.rotation, rightRotation, Time.deltaTime * _turnSpeed * 4f);
                rightLegTargetPosition.position = Vector3.Lerp(rightLegTargetPosition.position, rightLegHit.point + rightLegOffset, Time.deltaTime * _turnSpeed * 4f);
            }
            else if (rightLegHit.transform == null)
            {
                if (!_player.IsHooked)
                    rightLegTargetRotation.rotation = Quaternion.Lerp(rightLegTargetRotation.rotation, rightRotationInAir, Time.deltaTime * _turnSpeed / 2f);
                rightLegTargetPosition.localPosition = Vector3.Lerp(rightLegTargetPosition.localPosition, _view.RightLegBasePos, Time.deltaTime * _turnSpeed / 2f);
            }
        }

        public void RotateTracks()
        {
            float leftTrackTargetSpeed = _playerMotion.CurrentHorizontalSpeed * _leftMod;
            float rightTrackTargetSpeed = _playerMotion.CurrentHorizontalSpeed  * _rightMod;
            
            _leftTrackSpeed += leftTrackTargetSpeed * Time.fixedDeltaTime * _turnSpeed;
            _rightTrackSpeed += rightTrackTargetSpeed * Time.fixedDeltaTime * _turnSpeed;

            _view.LeftLegMaterial.SetFloat(_trackOffset, -_leftTrackSpeed);
            _view.RightLegMaterial.SetFloat(_trackOffset, -_rightTrackSpeed);
            
            if (_player.IsHooked && !_player.IsGrounded || !_player.IsGrounded)
            {
                _leftMod = Mathf.Lerp(_leftMod, 0, Time.fixedDeltaTime);
                _rightMod = Mathf.Lerp(_rightMod, 0, Time.fixedDeltaTime);
                return;
            }
            
            if (_playerMotion.InputDirection.magnitude == 0)
                return;
            
            float fromY = _view.transform.rotation.eulerAngles.y;
            float toY = Quaternion.LookRotation(_playerMotion.InputDirection).eulerAngles.y;

            if (fromY > 270 && toY == 0)
                fromY -= 360;
            
            if (Math.Abs(fromY - toY) > _angleMinDelta)
            {
                float clockwise;
                float counterClockwise;

                if (fromY <= toY)
                {
                    clockwise = toY - fromY;
                    counterClockwise = fromY + (360 - toY);
                }
                else
                {
                    clockwise = 360 - fromY + toY;
                    counterClockwise = fromY - toY;
                }

                if (clockwise < counterClockwise)
                {
                    //right
                    _leftMod = _tracksModStep;
                    _rightMod = -_tracksModStep;
                }
                else if (clockwise > counterClockwise)
                {
                    //left
                    _leftMod = -_tracksModStep;
                    _rightMod = _tracksModStep;
                }
            }
            else
            {
                _leftMod = _tracksModStep;
                _rightMod = _tracksModStep;
            }
            
            // Debug.LogError($"left {_leftTrackSpeed:.##} {_leftMod} right {_rightTrackSpeed:.##} {_rightMod} from {fromY} to {toY}");
        }
    }
}
using System;
using UnityEngine;
using Views;
using YellowOrphan.Player;

namespace Controllers
{
    public class PlayerTracks
    {
        private readonly PlayerView _view;
        private readonly IPlayerState _playerState;
        private readonly IPlayerMotion _playerMotion;
        private readonly float _turnSpeed;

        private bool _leftTrackGrounded;
        private bool _rightTrackGrounded;

        private float _leftMod;
        private float _rightMod;
        private float _leftTrackSpeed;
        private float _rightTrackSpeed;
        private float _previousMoveMagnitude;

        private static readonly int _trackOffset = Shader.PropertyToID("_Track_offset");

        private const float _shaderValueMultiplier = 3f;
        private const float _tracksModStep = 0.1f;
        private const float _tracksMotionMinAngle = 8f;
        private const float _trackRotationMaxAngle = 50;

        public PlayerTracks(PlayerView view, IPlayerState playerStateState, IPlayerMotion playerMotion)
        {
            _view = view;
            _playerState = playerStateState;
            _playerMotion = playerMotion;
            _turnSpeed = _view.TurnSpeed;
        }

        public void AdjustLegs()
        {
            float rayLength = _view.LegsRayLength;

            Transform leftLegRayStart = _view.LeftLegRayStart;
            Transform rightLegRayStart = _view.RightLegRayStart;
            Transform leftLegTargetPosition = _view.LeftLegTargetPosition;
            Transform rightLegTargetPosition = _view.RightLegTargetPosition;
            Transform leftLegTargetRotation = _view.LeftLegTargetRotation;
            Transform rightLegTargetRotation = _view.RightLegTargetRotation;

            Physics.Raycast(new Ray(leftLegRayStart.position, -leftLegRayStart.up), out RaycastHit leftLegHit, rayLength, layerMask: _view.WalkableLayers);
            Physics.Raycast(new Ray(rightLegRayStart.position, -rightLegRayStart.up), out RaycastHit rightLegHit, rayLength, layerMask: _view.WalkableLayers);
#if UNITY_EDITOR
            Debug.DrawRay(leftLegRayStart.position, -leftLegRayStart.up * rayLength, Color.cyan);
            Debug.DrawRay(rightLegRayStart.position, -rightLegRayStart.up * rayLength, Color.cyan);
            // Debug.LogError($"leftTrack {_leftTrackGrounded} rightTrack {_rightTrackGrounded}");
#endif
            Vector3 leftLegOffset = new Vector3(0f, Vector3.Distance(_view.LeftLegBottom.position, leftLegHit.point), 0f);
            Vector3 rightLegOffset = new Vector3(0f, Vector3.Distance(_view.RightLegBottom.position, rightLegHit.point), 0f);

            Quaternion leftRotationInAir = _view.transform.rotation * Quaternion.Euler(0f, 0f, 15f);
            Quaternion rightRotationInAir = _view.transform.rotation * Quaternion.Euler(0f, 0f, -15f);

            if (leftLegHit.transform != null)
            {
                Vector3 leftForward = Vector3.ProjectOnPlane(_view.transform.forward, leftLegHit.normal);
                Quaternion leftRotation = Quaternion.LookRotation(leftForward, leftLegHit.normal);

                if (_playerState.IsGrounded)
                    if (Quaternion.Angle(leftLegTargetRotation.rotation, leftRotation) < _trackRotationMaxAngle)
                        leftLegTargetRotation.rotation = Quaternion.Lerp(leftLegTargetRotation.rotation, leftRotation, Time.deltaTime * _turnSpeed * 4f);
                leftLegTargetPosition.position = Vector3.Lerp(leftLegTargetPosition.position, leftLegHit.point + leftLegOffset, Time.deltaTime * _turnSpeed * 4f);
                _leftTrackGrounded = true;
            }
            else if (leftLegHit.transform == null)
            {
                leftLegTargetRotation.rotation = Quaternion.Lerp(leftLegTargetRotation.rotation, leftRotationInAir, Time.deltaTime * _turnSpeed / 2f);
                leftLegTargetPosition.localPosition = Vector3.Lerp(leftLegTargetPosition.localPosition, _view.LeftLegBasePos, Time.deltaTime * _turnSpeed / 2f);
                _leftTrackGrounded = false;
            }

            if (rightLegHit.transform != null)
            {
                Vector3 rightForward = Vector3.ProjectOnPlane(_view.transform.forward, rightLegHit.normal);
                Quaternion rightRotation = Quaternion.LookRotation(rightForward, rightLegHit.normal);

                if (_playerState.IsGrounded)
                    if (Quaternion.Angle(rightLegTargetRotation.rotation, rightRotation) < _trackRotationMaxAngle)
                        rightLegTargetRotation.rotation = Quaternion.Lerp(rightLegTargetRotation.rotation, rightRotation, Time.deltaTime * _turnSpeed * 4f);
                rightLegTargetPosition.position = Vector3.Lerp(rightLegTargetPosition.position, rightLegHit.point + rightLegOffset, Time.deltaTime * _turnSpeed * 4f);
                _rightTrackGrounded = true;
            }
            else if (rightLegHit.transform == null)
            {
                rightLegTargetRotation.rotation = Quaternion.Lerp(rightLegTargetRotation.rotation, rightRotationInAir, Time.deltaTime * _turnSpeed / 2f);
                rightLegTargetPosition.localPosition = Vector3.Lerp(rightLegTargetPosition.localPosition, _view.RightLegBasePos, Time.deltaTime * _turnSpeed / 2f);
                _rightTrackGrounded = false;
            }
        }

        public void RotateTracks()
        {
            float leftTrackTargetSpeed = _playerMotion.CurrentHorizontalSpeed * _leftMod * _shaderValueMultiplier;
            float rightTrackTargetSpeed = _playerMotion.CurrentHorizontalSpeed * _rightMod * _shaderValueMultiplier;

            _leftTrackSpeed += leftTrackTargetSpeed * Time.fixedDeltaTime * _turnSpeed;
            _rightTrackSpeed += rightTrackTargetSpeed * Time.fixedDeltaTime * _turnSpeed;

            _view.LeftLegRenderer.material.SetFloat(_trackOffset, -_leftTrackSpeed);
            _view.RightLegRenderer.material.SetFloat(_trackOffset, -_rightTrackSpeed);

            if (!_leftTrackGrounded)
                _leftMod = Mathf.Lerp(_leftMod, 0, Time.fixedDeltaTime);
            if (!_rightTrackGrounded)
                _rightMod = Mathf.Lerp(_rightMod, 0, Time.fixedDeltaTime);
            
            if (_playerMotion.InputDirection.magnitude == 0)
                return;

            float fromY = _view.transform.rotation.eulerAngles.y;
            float toY = Quaternion.LookRotation(_playerMotion.InputDirection).eulerAngles.y;

            if (fromY > 270 && toY == 0)
                fromY -= 360;

            if (Math.Abs(fromY - toY) > _tracksMotionMinAngle && _playerState.IsGrounded)
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
                if (_leftTrackGrounded)
                    _leftMod = _tracksModStep;
                if (_rightTrackGrounded)
                    _rightMod = _tracksModStep;
            }

            // Debug.LogError($"left {_leftTrackSpeed:.##} {_leftMod} right {_rightTrackSpeed:.##} {_rightMod} from {fromY} to {toY}");
        }
    }
}
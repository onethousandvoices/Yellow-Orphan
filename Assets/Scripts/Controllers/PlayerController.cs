using Controllers;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Views;
using YellowOrphan.Controllers;
using Zenject;

namespace YellowOrphan.Player
{
    public class PlayerController : IInitializable, ITickable, IFixedTickable, ILateTickable, IPlayerState, IPlayerMotion, IDisposable
    {
        [Inject] private MonoInstance _mono;
        [Inject] private PlayerView _view;
        [Inject] private IPlayerStatsUI _playerStatsUI;
        [Inject] private IConsoleHandler _consoleHandler;

        private Animator _animator;
        private InputMap _inputMap;
        private RaycastHit _groundHit;
        private SphereCollider _sphereCollider;
        private PlayerPhysics _physics;
        private PlayerTracks _tracks;

        private Vector2 _move;
        private Vector2 _look;

        private bool _sprint;
        private bool _staminaWasZero;
        private bool _staminaSpendable = true;
        private bool _airControl;
        private bool _isOnSlope;
        private bool _wasHooked;
        
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        private float _fallStartHeight;
        private float _slopeAngle;

        private const float _speedUpChangeRate = 30f;
        private const float _slowDownChangeRate = 20f;
        private const float _jumpTimeout = 0.01f;
        private const float _fallTimeout = 0.05f;

        private static readonly int _speedBlendHash = Animator.StringToHash("SpeedBlend");

        public float CurrentSpeed { get; private set; }
        public float CurrentStamina { get; private set; }
        public bool IsGrounded { get; private set; } = true;
        public bool IsAiming { get; private set; }
        public bool IsHooked { get; private set; }
        public bool InputBlocked { get; set; }
        
        public Vector3 InputDirection { get; private set; }
        public float CurrentHorizontalSpeed { get; private set; }

        public void Initialize()
        {
            _jumpTimeoutDelta = _jumpTimeout;
            _fallTimeoutDelta = _fallTimeout;

            CurrentStamina = _view.MaxStamina;

            _sphereCollider = _view.GetComponent<SphereCollider>();
            _animator = _view.Animator;

            _physics = new PlayerPhysics(_view, this);
            _tracks = new PlayerTracks(_view, this, this);

            BindInputs();
        }

        public void Dispose()
            => _inputMap.Dispose();

        public void Tick()
        {
            ReadInput();
            JumpAndGravity();
            GroundCheck();
            Look();
            _tracks.AdjustLegs();
            _tracks.RotateTracks();
        }

        public void FixedTick()
        {
            Movement();
        }

        public void LateTick()
        {
            _physics.DrawLine();
        }

        private void BindInputs()
        {
            _inputMap = new InputMap();
            _inputMap.Enable();

            _inputMap.Player.Jump.performed += OnJumpPerformed;
            _inputMap.Player.LMB.started += OnLMBStarted;
            _inputMap.Player.LMB.canceled += OnLMBCanceled;
            _inputMap.Player.RMB.started += OnRMBStarted;
            _inputMap.Player.RMB.canceled += OnRMBCanceled;

            _inputMap.Player.Console.performed += _ => _consoleHandler.ShowConsole();
            _inputMap.Player.ReturnButton.performed += _ => _consoleHandler.OnReturn();
            _inputMap.Player.ArrowUp.performed += _ => _consoleHandler.OnUpArrow();
        }

        private void OnRMBStarted(InputAction.CallbackContext obj)
            => IsAiming = true;

        private void OnRMBCanceled(InputAction.CallbackContext obj)
            => IsAiming = false;

        private void OnLMBStarted(InputAction.CallbackContext obj)
        {
            if (!IsAiming || InputBlocked)
                return;

            Physics.Raycast(_view.HookRayStart.position, _view.HookRayStart.forward * _view.HookRange, out RaycastHit hit);
            if (hit.transform == null)
                return;

            IsHooked = true;
            _wasHooked = true;
            _airControl = true;
            _physics.TryHook(hit.point);
        }

        private void OnLMBCanceled(InputAction.CallbackContext obj)
        {
            if (_view.IsHookDebug)
                return;
            
            if (!_wasHooked)
                return;

            IsHooked = false;
            _wasHooked = false;
            _airControl = false;
            _physics.HookStop();
            _mono.StartCoroutine(_physics.NormalizeRotation(0.3f));
        }

        private void ReadInput()
        {
            if (InputBlocked)
                return;

            _move = _inputMap.Player.Move.ReadValue<Vector2>();
            _look = _inputMap.Player.Look.ReadValue<Vector2>();
            
            if (_inputMap.Player.HookClimb.IsPressed())
                _physics.HookClimb(_view.HookClimbSpeed);
            else if (_inputMap.Player.HookDescend.IsPressed())
                _physics.HookDescend(_view.HookRange, _view.HookDescendSpeed);

            CheckSprint();
        }

        private void CheckSprint()
        {
            bool isSprinting = _inputMap.Player.Sprint.IsPressed() && !IsHooked;

            if (!isSprinting || !_staminaSpendable)
            {
                SpendStamina(_view.StaminaRecovery * Time.deltaTime);
                _sprint = false;
                return;
            }

            SpendStamina(-_view.StaminaSprintCost * Time.deltaTime);
            _sprint = true;
        }

        private void OnJumpPerformed(InputAction.CallbackContext obj)
        {
            if (!IsGrounded || IsHooked || _jumpTimeoutDelta > 0f)
                return;
            _physics.Velocity = new Vector3(_physics.Velocity.x, _view.JumpHeight, _physics.Velocity.z);
        }

        private void SpendStamina(float cost)
        {
            CurrentStamina += cost;

            if (CurrentStamina >= _view.MaxStamina)
                CurrentStamina = _view.MaxStamina;
            else if (CurrentStamina <= 0)
            {
                CurrentStamina = 0;
                _staminaWasZero = true;
                _staminaSpendable = false;
            }

            if (_staminaWasZero && CurrentStamina / _view.MaxStamina >= 0.3f)
            {
                _staminaWasZero = false;
                _staminaSpendable = true;
            }

            _playerStatsUI.SetStamina(CurrentStamina, _view.MaxStamina);
        }

        private void JumpAndGravity()
        {
            if (IsGrounded)
            {
                _fallTimeoutDelta = _fallTimeout;

                if (_jumpTimeoutDelta >= 0f)
                    _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = _jumpTimeout;

                if (_fallTimeoutDelta >= 0f)
                    _fallTimeoutDelta -= Time.deltaTime;
            }
        }

        private void Movement()
        {
            float targetSpeed = _sprint ? _view.SprintSpeed : _view.WalkSpeed;

            if (_move.sqrMagnitude == 0)
                targetSpeed = 0f;

            CurrentHorizontalSpeed = new Vector3(_physics.Velocity.x, 0f, _physics.Velocity.z).magnitude;
            float modifier = _move.sqrMagnitude > 0 ? _speedUpChangeRate : _slowDownChangeRate;

            CurrentSpeed = Mathf.Lerp(CurrentHorizontalSpeed, targetSpeed, Time.fixedDeltaTime * modifier);

            InputDirection = new Vector3(_move.x, 0.0f, _move.y).normalized;

            Vector3 inputDirectionModified = InputDirection * CurrentSpeed;

            Vector3 velocityChange = inputDirectionModified - _physics.Velocity;
            velocityChange = new Vector3(velocityChange.x, 0f, velocityChange.z);
            velocityChange = Vector3.ClampMagnitude(velocityChange, CurrentSpeed);

            if (IsGrounded || _airControl)
                _physics.AddForce(velocityChange, ForceMode.Acceleration);

            if (_isOnSlope && !IsHooked)
            {
                if (_slopeAngle >= _view.MaxSlopeAngle)
                    _physics.AddForce(-_view.transform.up * _view.ImpossibleSlopeDownwardForce, ForceMode.Force);
                else
                    _physics.AddForce(-_groundHit.transform.up * _view.PossibleSlopeDownwardForce, ForceMode.Force);
            }
            
            if (InputDirection.magnitude > 0 && IsGrounded)
                _view.transform.rotation = Quaternion.Slerp(_view.transform.rotation, Quaternion.LookRotation(velocityChange), Time.fixedDeltaTime * _view.TurnSpeed);

            _animator.SetFloat(_speedBlendHash, _physics.Velocity.magnitude);
        }

        private void GroundCheck()
        {
            Vector3 rayStart = new Vector3(_view.transform.position.x, _view.transform.position.y + _view.GroundCheckRayOffset, _view.transform.position.z);
            Ray ray = new Ray(rayStart, -_view.transform.up);

            Physics.Raycast(ray, out _groundHit, _view.GroundCheckRayLength);

#if UNITY_EDITOR
            Debug.DrawRay(rayStart, -_view.transform.up * _view.GroundCheckRayLength, Color.green);
            // Debug.LogError($"grounded {IsGrounded} slope {_isOnSlope} slopeAngle {_slopeAngle}");
#endif
            
            if (_groundHit.transform != null)
                _slopeAngle = Vector3.Angle(_view.transform.up, _groundHit.normal);

            IsGrounded = _groundHit.transform != null && _groundHit.distance < _view.GroundCheckMinDistance;
            _isOnSlope = _groundHit.transform != null && IsGrounded && Mathf.Abs(_slopeAngle) > _view.MinSlopeAngle;

            _physics.SetGravity(!_isOnSlope);

            switch (IsGrounded)
            {
                case true when !IsHooked:
                    _physics.ResetDrag();
                    _sphereCollider.material = _move.sqrMagnitude == 0
                                                   ? _view.FrictionMaterial
                                                   : _view.SlipperyMaterial;

                    // if (_fallStartHeight > 0)
                    // {
                    //     float fallHeight = Mathf.Abs(_view.transform.position.y - _fallStartHeight) / _playerHeight;
                    //     if (fallHeight >= _playerHeight)
                    //     {
                    //         int damage = (int)fallHeight * _view.FallDamagePerHeight;
                    //         Debug.Log($"{damage} fall height damage taken");
                    //     }
                    //     _fallStartHeight = 0f;
                    // }
                    break;
                case false:
                    if (!IsHooked)
                        _physics.SetDrag(_view.InAirDrag);
                    _sphereCollider.material = _view.SlipperyMaterial;
                    if (_fallStartHeight <= 0)
                        _fallStartHeight = _view.transform.position.y;
                    break;
            }
            // _animator.SetBool(_animGrounded, _isGrounded);
        }

        //todo obsolete?
        private Vector3 AdjustSlopeVelocity(Vector3 velocity)
        {
            Vector3 rayStart = new Vector3(_view.transform.position.x, _view.transform.position.y + 0.3f, _view.transform.position.z);
            Ray ray = new Ray(rayStart, Vector3.down);
            if (!Physics.Raycast(ray, out RaycastHit slopeHit, 2f))
                return velocity;
            Quaternion slopeRotation = Quaternion.FromToRotation(_view.transform.up, slopeHit.normal);
            Vector3 adjustedVelocity = slopeRotation * velocity;

            return adjustedVelocity.y != 0 ? adjustedVelocity : velocity;
        }

        private void Look()
        {
            if (_look.sqrMagnitude == 0f || !IsAiming)
                return;
            
            Vector3 angles = _view.HeadTarget.transform.localEulerAngles + new Vector3(_look.y, _look.x, 0f) * _view.Sensitivity;

            angles.x = angles.x > 180 ? angles.x - 360 : angles.x;
            angles.x = Mathf.Clamp(angles.x, _view.LookRangeX.x, _view.LookRangeX.y);

            angles.y = angles.y > 180 ? angles.y - 360 : angles.y;
            angles.y = Mathf.Clamp(angles.y, _view.LookRangeY.x, _view.LookRangeY.y);

            _view.HeadTarget.transform.localEulerAngles = angles;
        }
    }

    public interface IPlayerState
    {
        public float CurrentSpeed { get; }
        public float CurrentStamina { get; }
        
        public bool IsGrounded { get; }
        public bool IsAiming { get; }
        public bool IsHooked { get; }
        public bool InputBlocked { get; set; }
    }

    public interface IPlayerMotion
    {
        public Vector3 InputDirection{ get; }
        public float CurrentHorizontalSpeed { get; }
    }
}
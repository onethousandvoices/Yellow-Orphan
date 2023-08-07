using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Views;
using YellowOrphan.Controllers;
using Zenject;

namespace YellowOrphan.Player
{
    public class PlayerController : IInitializable, ITickable, IFixedTickable, IPlayerState, IDisposable
    {
        [Inject] private MonoInstance _mono;
        [Inject] private PlayerView _view;
        [Inject] private IPlayerStatsUI _playerStatsUI;
        [Inject] private IConsoleHandler _consoleHandler;

        private Animator _animator;
        private PlayerHook _playerHook;
        private InputMap _inputMap;
        private BoxCollider _boxCollider;
        private Vector2 _speedBlend;
        private Vector2 _sprintBlend;
        private Vector2 _move;
        private Vector2 _look;
        private Vector2 _preJumpAcceleration;

        private bool _sprint;
        private bool _staminaWasZero;
        private bool _staminaSpendable = true;
        private bool _airControl;
        private bool _isGrounded = true;

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        private float _fallStartHeight;
        private float _playerHeight;

        private const float _speedUpChangeRate = 80f;
        private const float _slowDownChangeRate = 30f;
        private const float _jumpTimeout = 0.01f;
        private const float _fallTimeout = 0.05f;
        private const float _groundedOffset = -0.15f;
        private const float _groundCheckSphereRadius = 0.3f;
        private const float _turnSpeed = 10f;

        private static readonly int _animSpeedX = Animator.StringToHash("SpeedX");
        private static readonly int _animSpeedY = Animator.StringToHash("SpeedY");
        private static readonly int _animGrounded = Animator.StringToHash("Grounded");
        private static readonly int _animJump = Animator.StringToHash("Jump");
        private static readonly int _animFreeFall = Animator.StringToHash("FreeFall");

        public float CurrentSpeed { get; private set; }
        public float CurrentStamina { get; private set; }
        public bool InputBlocked { get; set; }
        public bool IsAiming { get; private set; }
        public bool IsHooked { get; private set; }

        public void Initialize()
        {
            _jumpTimeoutDelta = _jumpTimeout;
            _fallTimeoutDelta = _fallTimeout;

            CurrentStamina = _view.MaxStamina;

            _boxCollider = _view.GetComponent<BoxCollider>();
            _playerHeight = _boxCollider.size.y;
            _animator = _view.Animator;

            _playerHook = new PlayerHook(_view.Rb, _view.Joint);
            
            _consoleHandler.AddCommand(new DebugCommand("hookStop", "Hook cancel", "hookStop", () =>
            {
                IsHooked = false;
                _playerHook.HookStop();
            }));
            
            BindInputs();
        }

        public void Dispose()
            => _inputMap.Dispose();

        public void Tick()
        {
            ReadInput();
            Look();
        }

        public void FixedTick()
        {
            NormalizeRotation();
            JumpAndGravity();
            GroundedCheck();
            Movement();
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
        {
            IsAiming = true;
        }

        private void OnRMBCanceled(InputAction.CallbackContext obj)
        {
            IsAiming = false;
        }

        private void OnLMBStarted(InputAction.CallbackContext obj)
        {
            if (!IsAiming)
                return;
            IsHooked = true;
            
            Physics.Raycast(_view.RayStart.position, _view.RayStart.forward * _view.HookRange, out RaycastHit hit);
            if (hit.rigidbody == null)
                return;
            
            _playerHook.TryHook(_view.transform.position - hit.point, hit.rigidbody);
        }

        private void OnLMBCanceled(InputAction.CallbackContext obj)
        {
            // IsHooked = false;
            // _playerHook.HookStop();
        }

        private void NormalizeRotation()
        {
            if (IsHooked)
                return;
            if (_view.transform.eulerAngles.magnitude < 0.1f)
            {
                _view.transform.rotation = Quaternion.identity;
                _view.Rb.constraints = RigidbodyConstraints.FreezeRotation;
                return;
            }
            _view.transform.eulerAngles = 
                Vector3.Lerp(_view.transform.eulerAngles, new Vector3(0f, _view.transform.eulerAngles.y, 0f), Time.deltaTime * 10f);
        }

        private void ReadInput()
        {
            if (InputBlocked)
                return;
            
            _move = _inputMap.Player.Move.ReadValue<Vector2>();
            _look = _inputMap.Player.Look.ReadValue<Vector2>();

            CheckSprint();
        }

        private void OnJumpPerformed(InputAction.CallbackContext obj)
        {
            if (!_isGrounded || _jumpTimeoutDelta > 0f)
                return;
            _view.Rb.velocity = new Vector3(_view.Rb.velocity.x, _view.JumpHeight, _view.Rb.velocity.z);
            _airControl = _move.sqrMagnitude < 0.001f;
            if (!_airControl)
                _preJumpAcceleration = new Vector2(_view.Rb.velocity.x, _view.Rb.velocity.z);
        }

        private void CheckSprint()
        {
            bool isSprinting = _inputMap.Player.Sprint.IsPressed();

            if (!isSprinting || !_staminaSpendable)
            {
                SpendStamina(_view.StaminaRecovery * Time.deltaTime);
                _sprint = false;
                return;
            }

            SpendStamina(-_view.StaminaSprintCost * Time.deltaTime);
            _sprint = true;
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
            if (_isGrounded)
            {
                _fallTimeoutDelta = _fallTimeout;

                // _animator.SetBool(_animJump, false);
                // _animator.SetBool(_animFreeFall, false);

                // if (_jump && _jumpTimeoutDelta <= 0f)
                // _animator.SetBool(_animJump, true);

                if (_jumpTimeoutDelta >= 0f)
                    _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = _jumpTimeout;

                if (_fallTimeoutDelta >= 0f)
                    _fallTimeoutDelta -= Time.deltaTime;

                // _animator.SetBool(_animFreeFall, true);
            }
        }

        private void GroundedCheck()
        {
            Vector3 playerPos = _view.transform.position;
            Vector3 spherePosition = new Vector3(playerPos.x, playerPos.y - _groundedOffset, playerPos.z);
            _isGrounded = Physics.CheckSphere(spherePosition, _groundCheckSphereRadius, _view.GroundLayers, QueryTriggerInteraction.Ignore);

            _view.SetGroundCheckSphereParams(spherePosition, _groundCheckSphereRadius);

            switch (_isGrounded)
            {
                case true:
                    _boxCollider.material = _move.sqrMagnitude == 0
                                                ? _view.FrictionMaterial
                                                : _view.SlipperyMaterial;

                    if (_fallStartHeight > 0)
                    {
                        float fallHeight = Mathf.Abs(_view.transform.position.y - _fallStartHeight) / _playerHeight;
                        if (fallHeight >= _playerHeight)
                        {
                            int damage = (int)fallHeight * _view.FallDamagePerHeight;
                            Debug.Log($"{damage} fall height damage taken");
                        }
                        _fallStartHeight = 0f;
                    }
                    break;
                case false:
                    _boxCollider.material = _view.SlipperyMaterial;
                    if (_fallStartHeight <= 0)
                        _fallStartHeight = _view.transform.position.y;
                    break;
            }
            // _animator.SetBool(_animGrounded, _isGrounded);
        }

        private void Movement()
        {
            float targetSpeed = _sprint ? _view.SprintSpeed : _view.WalkSpeed;

            if (_move == Vector2.zero)
                targetSpeed = 0f;

            float rbSpeed = new Vector3(_view.Rb.velocity.x, 0f, _view.Rb.velocity.z).magnitude;
            float modifier = _move.sqrMagnitude > 0 ? _speedUpChangeRate : _slowDownChangeRate;

            CurrentSpeed = Mathf.Lerp(rbSpeed, targetSpeed, Time.fixedDeltaTime * modifier);

            Vector3 inputDirection = new Vector3(_move.x, 0.0f, _move.y).normalized;

            if (!_isGrounded)
                CurrentSpeed *= _view.InAirVelocityReduction;

            Vector3 currentVelocity = _view.Rb.velocity;
            inputDirection *= CurrentSpeed;

            Vector3 velocityChange = inputDirection - currentVelocity;
            velocityChange = new Vector3(velocityChange.x, 0f, velocityChange.z);
            velocityChange = AdjustSlopeVelocity(velocityChange);
            velocityChange = Vector3.ClampMagnitude(velocityChange, CurrentSpeed);

            if (_isGrounded || _airControl)
                _view.Rb.AddForce(velocityChange, ForceMode.Acceleration);
            else
                _view.Rb.velocity = new Vector3(_preJumpAcceleration.x, _view.Rb.velocity.y, _preJumpAcceleration.y);

            if (inputDirection.sqrMagnitude > 0 && _isGrounded)
                _view.transform.rotation = Quaternion.Slerp(_view.transform.rotation, Quaternion.LookRotation(velocityChange), Time.fixedDeltaTime * _turnSpeed);

            // _sprintBlend = new Vector2(0f, _sprint ? 1 : 0);
            // _speedBlend = Vector3.Lerp(_speedBlend, _move + _sprintBlend, Time.fixedDeltaTime * modifier * 5f);

            // _animator.SetFloat(_animSpeedX, _speedBlend.x);
            // _animator.SetFloat(_animSpeedY, _speedBlend.y);
        }

        private Vector3 AdjustSlopeVelocity(Vector3 velocity)
        {
            Ray ray = new Ray(_view.transform.position, Vector3.down);
            if (!Physics.Raycast(ray, out RaycastHit slopeHit, 2f))
                return velocity;
            Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, slopeHit.normal);
            Vector3 adjustedVelocity = slopeRotation * velocity;

            return adjustedVelocity.y < 0 ? adjustedVelocity : velocity;
        }

        private void Look()
        {
            if (_look.sqrMagnitude < 0.1f || !IsAiming)
                return;

            _view.HeadTarget.transform.localEulerAngles += new Vector3(_look.y, _look.x, 0f);

            Vector3 angles = _view.HeadTarget.transform.localEulerAngles;

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

        public bool InputBlocked { get; set; }
        public bool IsAiming { get; }
        public bool IsHooked { get; }
    }
}
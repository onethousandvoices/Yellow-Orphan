using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Views;
using YellowOrphan.Controllers;
using YellowOrphan.Utility;
using Zenject;

namespace YellowOrphan.Player
{
    public enum MoveState : byte
    {
        Idle,
        Run,
        Sprint,
        Jump
    }

    [Flags]
    public enum InputState : byte
    {
        None = 1,
        BlockJump = 4,
        BlockCamera = 8,
        BlockSprint = 16,
        BlockMove = 32,
        BlockLook = 64,
        ShowCursor = 128
    }

    public class InputController : IInitializable, ITickable, IFixedTickable, IPlayerState, IDisposable
    {
        [Inject] private PlayerView _view;
        [Inject] private IConsoleHandler _consoleHandler;
        [Inject] private List<IRMBListener> _rmbListeners;
        [Inject] private List<ILMBListener> _lmbListeners;

        private InputMap _inputMap;
        // private Animator _animator;
        private BoxCollider _boxCollider;
        private Vector2 _speedBlend;
        private Vector2 _sprintBlend;
        private Vector2 _move;
        private Vector2 _look;
        private Vector2 _preJumpAcceleration;

        private bool _sprint;
        private bool _airControl;
        private bool _isLockCameraPosition;
        private bool _isGrounded = true;

        private float _relativeSpeed;
        private float _rotationVelocity;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        private float _staminaJumpCost;
        private float _staminaSprintCost;
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

        public event Action InputStateChanged;
        public event Action MoveStateChanged;

        public MoveState MoveState { get; private set; }
        public InputState InputState { get; private set; }

        public float CurrentSpeed { get; private set; }

        public void Initialize()
        {
            MoveState = MoveState.Idle;

            _jumpTimeoutDelta = _jumpTimeout;
            _fallTimeoutDelta = _fallTimeout;

            _boxCollider = _view.GetComponent<BoxCollider>();
            _playerHeight = _boxCollider.size.y;
            // _animator = _view.Animator;

            BindInputs();
        }

        public void Dispose()
            => _inputMap.Dispose();

        public void Tick()
        {
            CheckCursor();
            ReadInput();
            Look();
        }

        public void FixedTick()
        {
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
            => _rmbListeners.ForEach(x => x.OnRMBStarted(obj));

        private void OnRMBCanceled(InputAction.CallbackContext obj)
            => _rmbListeners.ForEach(x => x.OnRMBCanceled(obj));

        private void OnLMBStarted(InputAction.CallbackContext obj)
            => _lmbListeners.ForEach(x => x.OnLMBStarted(obj));

        private void OnLMBCanceled(InputAction.CallbackContext obj)
            => _lmbListeners.ForEach(x => x.OnLMBCanceled(obj));

        private void CheckCursor()
        {
            if (InputState.HasFlagOptimized(InputState.ShowCursor))
            {
                Cursor.lockState = CursorLockMode.Confined;
                return;
            }
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void ReadInput()
        {
            _move = InputState.HasFlagOptimized(InputState.BlockMove)
                        ? Vector2.zero
                        : _inputMap.Player.Move.ReadValue<Vector2>();

            _look = InputState.HasFlagOptimized(InputState.BlockLook)
                        ? Vector2.zero
                        : _inputMap.Player.Look.ReadValue<Vector2>();
            
            CheckSprint();
        }

        private void OnJumpPerformed(InputAction.CallbackContext obj)
        {
            if ( /*!_jumpPossible || */!_isGrounded || _jumpTimeoutDelta > 0f || InputState.HasFlagOptimized(InputState.BlockJump))
                return;
            _view.Rb.velocity = new Vector3(_view.Rb.velocity.x, _view.JumpHeight, _view.Rb.velocity.z);
            _airControl = _move.sqrMagnitude < 0.001f;
            if (!_airControl)
                _preJumpAcceleration = new Vector2(_view.Rb.velocity.x, _view.Rb.velocity.z);
        }

        private void CheckSprint()
        {
            bool isSprinting = _inputMap.Player.Sprint.IsPressed();

            if (InputState.HasFlagOptimized(InputState.BlockSprint))
                isSprinting = false;

            if (!isSprinting)
            {
                _sprint = false;
                return;
            }

            _sprint = true;
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
                    MoveState = MoveState.Idle;
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
                    MoveState = MoveState.Jump;
                    _boxCollider.material = _view.SlipperyMaterial;
                    if (_fallStartHeight <= 0)
                        _fallStartHeight = _view.transform.position.y;
                    break;
            }
            // _animator.SetBool(_animGrounded, _isGrounded);
            MoveStateChanged?.Invoke();
        }

        private void Movement()
        {
            float targetSpeed;
            MoveState pendingState;

            if (_sprint)
            {
                targetSpeed = _view.SprintSpeed;
                pendingState = MoveState.Sprint;
            }
            else
            {
                targetSpeed = _view.Speed;
                pendingState = MoveState.Run;
            }

            if (_move == Vector2.zero)
            {
                targetSpeed = 0f;
                pendingState = MoveState.Idle;
            }

            if (_isGrounded && MoveState != pendingState)
            {
                MoveState = pendingState;
                MoveStateChanged?.Invoke();
            }

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
            if (_look.sqrMagnitude < 0.1f)
                return;
            
            _view.HeadTarget.transform.eulerAngles += new Vector3(_look.y, _look.x, 0f);
            
            Vector3 angles = _view.HeadTarget.transform.eulerAngles;
            angles.x = angles.x > 180 ? angles.x - 360 : angles.x;
            angles.x = Mathf.Clamp(angles.x, _view.LookRange.x, _view.LookRange.y);

            _view.HeadTarget.transform.eulerAngles = angles;
        }
        
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f)
                lfAngle += 360f;
            if (lfAngle > 360f)
                lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        public void AddState(params InputState[] newState)
        {
            foreach (InputState state in newState)
                InputState |= state;
            InputStateChanged?.Invoke();
        }

        public void RemoveState(params InputState[] removeState)
        {
            foreach (InputState state in removeState)
                InputState &= ~state;
            InputStateChanged?.Invoke();
        }

        public void AddAllExcept(InputState state)
        {
            Array states = Enum.GetValues(typeof(InputState));

            foreach (InputState inputState in states)
            {
                if (inputState == state)
                    continue;
                InputState |= inputState;
            }
            InputStateChanged?.Invoke();
        }

        public void RemoveAllExcept(InputState state)
        {
            Array states = Enum.GetValues(typeof(InputState));

            foreach (InputState inputState in states)
            {
                if (inputState == state)
                    continue;
                InputState &= ~inputState;
            }
            InputStateChanged?.Invoke();
        }
    }

    public interface IPlayerState
    {
        public event Action InputStateChanged;
        public event Action MoveStateChanged;

        public MoveState MoveState { get; }
        public InputState InputState { get; }

        public float CurrentSpeed { get; }

        public void AddState(params InputState[] newStates);
        public void RemoveState(params InputState[] removeStates);
        public void AddAllExcept(InputState state);
        public void RemoveAllExcept(InputState state);
    }

    public interface IRMBListener
    {
        public void OnRMBStarted(InputAction.CallbackContext obj);
        public void OnRMBCanceled(InputAction.CallbackContext obj);
    }

    public interface ILMBListener
    {
        public void OnLMBStarted(InputAction.CallbackContext obj);
        public void OnLMBCanceled(InputAction.CallbackContext obj);
    }
}
using NaughtyAttributes;
using UnityEngine;

namespace Views
{
    public class PlayerView : MonoBehaviour
    {
        [field: SerializeField, HorizontalLine(color: EColor.Blue), Range(1f, 30f)] public float WalkSpeed { get; private set; }
        [field: SerializeField, Range(20f, 50f)] public float SprintSpeed { get; private set; }
        [field: SerializeField, Range(1f, 200f)] public int MaxStamina { get; private set; }
        [field: SerializeField, Range(0f, 10f)] public float StaminaSprintCost { get; private set; }
        [field: SerializeField, Range(0f, 10f)] public float StaminaRecovery { get; private set; }
        [field: SerializeField, Range(1f, 20f)] public float JumpHeight { get; private set; }
        [field: SerializeField, Range(0.5f, 1f)] public float InAirVelocityReduction { get; private set; }
        [field: SerializeField, Range(1, 100)] public int FallDamagePerHeight { get; private set; }
        [field: SerializeField, MinMaxSlider(-80, 80)] public Vector2 LookRangeX { get; private set; }
        [field: SerializeField, MinMaxSlider(-100, 100)] public Vector2 LookRangeY { get; private set; }
        [field: SerializeField, Range(1f, 20f)] public float HookRange { get; private set; }

        [field: SerializeField, HorizontalLine(color: EColor.Red)] public Animator Animator { get; private set; }
        // [field: SerializeField] public PlayerViewEventsReceiver EventsReceiver { get; private set; }
        [field: SerializeField] public Transform RayStart { get; private set; }
        [field: SerializeField] public Transform HeadTarget { get; private set; }
        [field: SerializeField] public LayerMask GroundLayers { get; private set; }
        [field: SerializeField] public Rigidbody Rb { get; private set; }
        [field: SerializeField] public ConfigurableJoint Joint { get; private set; }
        [field: SerializeField] public PhysicMaterial SlipperyMaterial { get; private set; }
        [field: SerializeField] public PhysicMaterial FrictionMaterial { get; private set; }
        
        private Vector3 _spherePosition;

        private float _sphereRadius;

        public void SetGroundCheckSphereParams(Vector3 position, float radius)
        {
            _spherePosition = position;
            _sphereRadius = radius;
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawSphere(_spherePosition, _sphereRadius);
        }
    }
}
using NaughtyAttributes;
using System;
using UnityEngine;

namespace Views
{
    public class PlayerView : MonoBehaviour
    {
        [field: Header("Settings")]
        [field: HorizontalLine(color: EColor.Blue), Header("Motion"), Space]
        [field: SerializeField, Range(1f, 30f)] public float WalkSpeed { get; private set; }
        [field: SerializeField, Range(20f, 50f)] public float SprintSpeed { get; private set; }
        [field: SerializeField, Range(20f, 80f)] public float MaxSlopeAngle { get; private set; }
        [field: SerializeField, Range(1f, 30f)] public float TurnSpeed { get; private set; }
        [field: SerializeField, Range(1f, 200f)] public int MaxStamina { get; private set; }
        [field: SerializeField, Range(0f, 10f)] public float StaminaSprintCost { get; private set; }
        [field: SerializeField, Range(0f, 10f)] public float StaminaRecovery { get; private set; }
        [field: SerializeField, Range(1f, 20f)] public float JumpHeight { get; private set; }
        [field: SerializeField, Range(1, 100)] public int FallDamagePerHeight { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float InAirDrag { get; private set; }

        [field: Header("Look"), Space]
        [field: SerializeField, Range(0.1f, 5f)] public float Sensitivity { get; private set; }
        [field: SerializeField, MinMaxSlider(-80, 80)] public Vector2 LookRangeX { get; private set; }
        [field: SerializeField, MinMaxSlider(-100, 100)] public Vector2 LookRangeY { get; private set; }

        [field: Header("Hook"), Space]
        [field: SerializeField, Range(1f, 20f)] public float HookRange { get; private set; }
        [field: SerializeField, Range(0.1f, 5f)] public float HookClimbSpeed { get; private set; }
        [field: SerializeField, Range(0.1f, 5f)] public float HookDescendSpeed { get; private set; }

        [field: Space]
        [field: Header("References")]
        [field: HorizontalLine(color: EColor.Red), Header("Transforms"), Space]
        [field: SerializeField] public Transform HookRayStart { get; private set; }
        [field: SerializeField] public Transform LeftLegTargetRotation { get; private set; }
        [field: SerializeField] public Transform RightLegTargetRotation { get; private set; }
        [field: SerializeField] public Transform LeftLegTargetPosition { get; private set; }
        [field: SerializeField] public Transform RightLegTargetPosition { get; private set; }
        [field: SerializeField] public Transform LeftLegBottom { get; private set; }
        [field: SerializeField] public Transform RightLegBottom { get; private set; }
        [field: SerializeField] public Transform HeadTarget { get; private set; }
        
        [field: Space]
        [field: Header("Others"), Space]
        [field: SerializeField] public Animator Animator { get; private set; }
        [field: SerializeField] public Rigidbody Rb { get; private set; }
        [field: SerializeField] public PhysicMaterial SlipperyMaterial { get; private set; }
        [field: SerializeField] public PhysicMaterial FrictionMaterial { get; private set; }
        [field: SerializeField] public Material LeftLegMaterial { get; private set; }
        [field: SerializeField] public Material RightLegMaterial { get; private set; }
        [field: SerializeField] public LineRenderer LineRenderer { get; private set; }
        [field: SerializeField] public LayerMask GroundLayers { get; private set; }

        //todo debug
        private Ray _ray;
        private Ray _ray2;

        public Vector3 LeftLegBasePos { get; private set; }
        public Vector3 RightLegBasePos { get; private set; }

        private void Awake()
            => OnValidate();

        private void OnTriggerEnter(Collider other)
        {
            throw new NotImplementedException();
        }

        public void SetGizmos(Ray ray, Ray ray2)
        {
            _ray = ray;
            _ray2 = ray2;
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawRay(HookRayStart.position, HookRayStart.forward * HookRange);
            // Gizmos.DrawRay(_ray);
            Gizmos.DrawRay(_ray2);
        }

        [Button("Force Validate")]
        private void OnValidate()
        {
            Transform[] transforms = GetComponentsInChildren<Transform>();
            foreach (Transform t in transforms)
            {
                switch (t.name)
                {
                    case "HookRayStart":
                        HookRayStart = t;
                        break;
                    case "Head_target":
                        HeadTarget = t;
                        break;
                    case "LeftLeg_target":
                        LeftLegTargetRotation = t;
                        break;
                    case "RightLeg_target":
                        RightLegTargetRotation = t;
                        break;
                    case "LeftLeg_targetPosition":
                        LeftLegTargetPosition = t;
                        break;
                    case "RightLeg_targetPosition":
                        RightLegTargetPosition = t;
                        break;
                    case "LeftLegBottom":
                        LeftLegBottom = t;
                        LeftLegBottom.localPosition = new Vector3(0f, -0.0927f, 0f);
                        break;
                    case "RightLegBottom":
                        RightLegBottom = t;
                        RightLegBottom.localPosition = new Vector3(0f, -0.0927f, 0f);
                        break;
                }
            }

            if (LeftLegTargetPosition != null)
                LeftLegBasePos = LeftLegTargetPosition.localPosition;
            if (RightLegTargetPosition != null)
                RightLegBasePos = RightLegTargetPosition.localPosition;

            Animator ??= GetComponent<Animator>();
            Rb ??= GetComponent<Rigidbody>();
            LineRenderer ??= GetComponent<LineRenderer>();
        }
    }
}
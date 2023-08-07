using UnityEngine;

namespace YellowOrphan.Controllers
{
    public class PlayerHook
    {
        private readonly Rigidbody _rb;
        private readonly ConfigurableJoint _joint;

        private readonly JointDrive _xDrive;
        private readonly JointDrive _yDrive;
        private readonly JointDrive _zDrive;
        private readonly JointDrive _angularXDrive;
        private readonly JointDrive _angularYZDrive;
        
        private readonly float _baseRbMass;
        private readonly float _baseRbDrag;

        private const float _hookedRbMass = 1f;
        private const float _hookedRbDrag = 1f;
        
        public PlayerHook(Rigidbody rb, ConfigurableJoint joint)
        {
            _rb = rb;
            _joint = joint;

            _baseRbMass = rb.mass;
            _baseRbDrag = rb.drag;

            _xDrive = _joint.xDrive;
            _yDrive = _joint.yDrive;
            _zDrive = _joint.zDrive;
            _angularXDrive = _joint.angularXDrive;
            _angularYZDrive = _joint.angularYZDrive;
            
            HookStop();
        }

        public void TryHook(Vector3 target, Rigidbody body)
        {
            _rb.mass = _hookedRbMass;
            _rb.drag = _hookedRbDrag;

            _rb.constraints = RigidbodyConstraints.None;

            _joint.connectedBody = body;
            _joint.connectedAnchor = target;
            _joint.xDrive = _xDrive;
            _joint.yDrive = _yDrive;
            _joint.zDrive = _zDrive;
            _joint.angularXDrive = _angularXDrive;
            _joint.angularYZDrive = _angularYZDrive;
        }

        public void HookStop()
        {
            _rb.mass = _baseRbMass;
            _rb.drag = _baseRbDrag;

            // _rb.constraints = RigidbodyConstraints.FreezeRotation;
            
            _joint.connectedBody = null;
            _joint.connectedAnchor = Vector3.zero;
            _joint.xDrive = new JointDrive();
            _joint.yDrive = new JointDrive();
            _joint.zDrive = new JointDrive();
            _joint.angularXDrive = new JointDrive();
            _joint.angularYZDrive = new JointDrive();
        }
    }
}
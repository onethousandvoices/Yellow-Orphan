using UnityEngine;

namespace YellowOrphan.Controllers
{
    public class PlayerHook
    {
        private SpringJoint _joint;
        private readonly Rigidbody _rb;
        private readonly RigidbodyConstraints _rbConstraints;
        
        private readonly float _baseRbMass;
        private readonly float _baseRbDrag;
        private readonly float _baseRbAngularDrag;

        private const float _hookedRbMass = 1f;
        private const float _hookedRbDrag = 1f;
        private const float _hookedRbAngularDrag = 1f;
        
        public PlayerHook(Rigidbody rb)
        {
            _rb = rb;
            _rbConstraints = _rb.constraints;

            _baseRbMass = rb.mass;
            _baseRbDrag = rb.drag;
            _baseRbAngularDrag = _rb.angularDrag;
            
            HookStop();
        }

        public void TryHook(Vector3 target)
        {
            _rb.mass = _hookedRbMass;
            _rb.drag = _hookedRbDrag;
            _rb.angularDrag = _hookedRbAngularDrag;
            _rb.constraints = RigidbodyConstraints.None;
            
            _joint = _rb.gameObject.AddComponent<SpringJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = target;
            _joint.anchor = new Vector3(0f, 1.2f, 0f);

            float distance = Vector3.Distance(_rb.transform.position, target);

            _joint.maxDistance = distance * 0.8f;
            _joint.minDistance = 0.1f;

            _joint.spring = 4.5f;
            _joint.damper = 7f;
            _joint.massScale = 4.5f;
        }

        public void HookStop()
        {
            _rb.mass = _baseRbMass;
            _rb.drag = _baseRbDrag;
            _rb.angularDrag = _baseRbAngularDrag;
            _rb.constraints = _rbConstraints;
            
            Object.Destroy(_rb.gameObject.GetComponent<SpringJoint>());
        }

        public void Descend(float maxDistance, float speed = 1f)
        {
            if (_joint == null)
                return;
            _joint.maxDistance = 
                Mathf.Lerp(_joint.maxDistance, maxDistance, Time.deltaTime * speed);
        }

        public void Climb(float speed = 1f)
        {
            if (_joint == null)
                return;
            _joint.maxDistance = 
                Mathf.Lerp(_joint.maxDistance, _joint.minDistance, Time.deltaTime * speed);
        }
    }
}
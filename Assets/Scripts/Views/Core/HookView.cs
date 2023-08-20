using UnityEngine;
using UnityEngine.VFX;

namespace Views
{
    public class HookView : MonoBehaviour
    {
        [field: SerializeField] public Transform Pos1 { get; private set; }
        [field: SerializeField] public Transform Pos2 { get; private set; }
        [field: SerializeField] public Transform Pos3 { get; private set; }
        [field: SerializeField] public Transform Pos4 { get; private set; }
        [field: SerializeField] public VisualEffect Effect { get; private set; }
    }
}
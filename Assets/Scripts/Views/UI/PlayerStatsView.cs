using UnityEngine;
using UnityEngine.UI;

namespace Views.UI
{
    public class PlayerStatsView : MonoBehaviour
    {
        [field: SerializeField] public Image StaminaBar { get; private set; }
    }
}
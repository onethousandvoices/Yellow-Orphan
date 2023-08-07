using Views.UI;
using Zenject;

namespace YellowOrphan.Controllers
{
    public class UIController : IInitializable, IPlayerStatsUI
    {
        [Inject] private PlayerStatsView _playerStatsView;

        public void Initialize() { }

        public void SetStamina(float current, float max)
            => _playerStatsView.StaminaBar.fillAmount = current / max;
    }

    public interface IPlayerStatsUI
    {
        public void SetStamina(float current, float max);
    }
}
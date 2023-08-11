using Tayx.Graphy;
using Views.UI;
using Zenject;

namespace YellowOrphan.Controllers
{
    public class UIController : IInitializable, IPlayerStatsUI
    {
        [Inject] private PlayerStatsView _playerStatsView;
        [Inject] private GraphyManager _graphy;
        [Inject] private IConsoleHandler _console;

        public void Initialize()
            => _console.AddCommand(new DebugCommand("stats", "Toggle stats", ToggleStats));

        private void ToggleStats()
        {
            _graphy.gameObject.SetActive(!_graphy.gameObject.activeSelf);
            _playerStatsView.gameObject.SetActive(!_playerStatsView.gameObject.activeSelf);
        }

        public void SetStamina(float current, float max)
            => _playerStatsView.StaminaBar.fillAmount = current / max;
    }

    public interface IPlayerStatsUI
    {
        public void SetStamina(float current, float max);
    }
}
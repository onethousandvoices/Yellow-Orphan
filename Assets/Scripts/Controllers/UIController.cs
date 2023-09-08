using Tayx.Graphy;
using VContainer;
using VContainer.Unity;
using Views.UI;

namespace YellowOrphan.Controllers
{
    public class UIController : IStartable, IPlayerStatsUI
    {
        [Inject] private readonly PlayerStatsView _playerStatsView;
        [Inject] private readonly GraphyManager _graphy;
        [Inject] private readonly IConsoleHandler _console;

        public void Start()
        {
            _console.AddCommand(new DebugCommand("stats", "Toggle stats", () =>
            {
                _graphy.gameObject.SetActive(!_graphy.gameObject.activeSelf);
                _playerStatsView.gameObject.SetActive(!_playerStatsView.gameObject.activeSelf);
            }));
        }

        public void SetStamina(float current, float max)
            => _playerStatsView.StaminaBar.fillAmount = current / max;
    }

    public interface IPlayerStatsUI
    {
        public void SetStamina(float current, float max);
    }
}
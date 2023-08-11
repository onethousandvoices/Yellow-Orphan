using Tayx.Graphy;
using UnityEngine;
using Views;
using Views.UI;
using Zenject;

namespace YellowOrphan.Installers
{
    public class UIViewsInstaller : MonoInstaller
    {
        [SerializeField] private DebugConsoleView _debugConsoleView;
        [SerializeField] private GraphyManager _graphyManager;
        [SerializeField] private PlayerStatsView _playerStatsView;
        [SerializeField] private MonoInstance _mono;

        public override void InstallBindings()
        {
            Container.Bind<MonoInstance>().FromInstance(_mono);
            Container.Bind<GraphyManager>().FromInstance(_graphyManager);
            
            Container.BindInterfacesAndSelfTo<DebugConsoleView>().FromInstance(_debugConsoleView);
            Container.BindInterfacesAndSelfTo<PlayerStatsView>().FromInstance(_playerStatsView);
        }
    }
}
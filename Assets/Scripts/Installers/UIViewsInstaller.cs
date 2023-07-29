using UnityEngine;
using Views.UI;
using Zenject;

namespace YellowOrphan.Installers
{
    public class UIViewsInstaller : MonoInstaller
    {
        [SerializeField] private DebugConsoleView _debugConsoleView;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<DebugConsoleView>().FromInstance(_debugConsoleView);
        }
    }
}
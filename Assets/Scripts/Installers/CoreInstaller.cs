using UnityEngine;
using YellowOrphan.Controllers;
using YellowOrphan.Player;
using Zenject;

namespace YellowOrphan.Installers
{
    public class CoreInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<GameController>().AsSingle();
            Container.BindInterfacesTo<TimeTickablesController>().AsSingle();
            Container.BindInterfacesTo<DebugConsoleController>().AsSingle();

            Container.BindInterfacesTo<UIController>().AsSingle();
            Container.BindInterfacesTo<PlayerController>().AsSingle();
        }
    }
}
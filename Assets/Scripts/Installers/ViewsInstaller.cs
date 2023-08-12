using UnityEngine;
using Views;
using Views.UI;
using Zenject;

namespace YellowOrphan.Installers
{
    public class ViewsInstaller : MonoInstaller
    {
        [SerializeField] private PlayerView _playerView;
        [SerializeField] private CMDebugCamera _cmDebugCamera;

        public override void InstallBindings()
        {
            Container.Bind<PlayerView>().FromInstance(_playerView).AsSingle();
            Container.Bind<CMDebugCamera>().FromInstance(_cmDebugCamera).AsSingle();
        }
    }
}
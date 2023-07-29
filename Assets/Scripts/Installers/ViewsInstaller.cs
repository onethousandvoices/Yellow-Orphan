using UnityEngine;
using Views;
using Zenject;

namespace YellowOrphan.Installers
{
    public class ViewsInstaller : MonoInstaller
    {
        [SerializeField] private PlayerView _playerView;
        [SerializeField] private Camera _camera;

        public override void InstallBindings()
        {
            Container.Bind<PlayerView>().FromInstance(_playerView).AsSingle();
            Container.Bind<Camera>().FromInstance(_camera).AsSingle();
        }
    }
}
using NaughtyAttributes;
using Tayx.Graphy;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Views;
using Views.UI;
using YellowOrphan.Controllers;

public class DIContainer : LifetimeScope
{
    [Header("Views"), HorizontalLine(color: EColor.Orange)]
    [SerializeField] private PlayerView _playerView;
    [SerializeField] private CMDebugCamera _cmDebugCamera;
    [SerializeField] private HookView _hookView;
    
    [Header("UI"), HorizontalLine(color: EColor.Orange)]
    [SerializeField] private DebugConsoleView _debugConsoleView;
    [SerializeField] private GraphyManager _graphyManager;
    [SerializeField] private PlayerStatsView _playerStatsView;
    [SerializeField] private MonoInstance _mono;
    
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<DebugConsoleController>(Lifetime.Scoped).AsImplementedInterfaces();
        builder.Register<GameController>(Lifetime.Scoped).AsImplementedInterfaces();
        builder.Register<PlayerController>(Lifetime.Scoped).AsImplementedInterfaces();
        builder.Register<TimeTickablesController>(Lifetime.Scoped).AsImplementedInterfaces();
        builder.Register<UIController>(Lifetime.Scoped).AsImplementedInterfaces();
        
        builder.RegisterInstance(_mono);
        builder.RegisterInstance(_graphyManager);
        builder.RegisterInstance(_debugConsoleView);
        builder.RegisterInstance(_playerStatsView);
        
        builder.RegisterInstance(_playerView);
        builder.RegisterInstance(_cmDebugCamera);
        builder.RegisterInstance(_hookView);
    }
}

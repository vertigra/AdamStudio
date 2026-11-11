using AdamStudio.Core.AvalonDock.Services;
using AdamStudio.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AdamStudio.Extensions;

public record OpenSettingsMessage();

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует обработчик сообщения OpenSettingsMessage.
    /// </summary>
    public static void AddSettingsDocument(this IServiceCollection services)
    {
        services.AddSingleton<SettingsViewModel>();

        services.AddSingleton<IMessenger, WeakReferenceMessenger>();

        // Регистрируем обработчик через фабрику, чтобы получить доступ к ServiceProvider
        services.AddSingleton(sp =>
        {
            var messenger = sp.GetRequiredService<IMessenger>();
            var dockService = sp.GetRequiredService<IDockWindowService>();
            var settingsVm = sp.GetRequiredService<SettingsViewModel>();

            return new MessengerHandler<OpenSettingsMessage>(messenger, () =>
            {
                if (dockService.Documents.Contains(settingsVm))
                    dockService.ActivateDocument(settingsVm);
                else
                {
                    dockService.OpenDocument(settingsVm);
                    dockService.ActivateDocument(settingsVm);
                }
            });
        });
    }

    /// <summary>
    /// Вспомогательный класс для регистрации обработчика.
    /// </summary>
    public sealed class MessengerHandler<TMessage> where TMessage : class
    {
        public MessengerHandler(IMessenger messenger, Action action)
        {
            messenger.Register<TMessage>(this, (r, m) => action());
        }
    }
}
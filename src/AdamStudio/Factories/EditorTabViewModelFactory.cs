using AdamStudio.Core.AvalonDock.Model;
using AdamStudio.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AdamStudio.Factories;

/// <summary>
/// Фабрика для создания экземпляров EditorTabViewModel через DI.
///
/// Зачем нужна:
/// EditorTabViewModel — transient-объект (каждый документ — новый экземпляр),
/// но при этом может иметь зависимости в конструкторе (например, логирование,
/// диспетчер команд, медиатор). Чтобы не вызывать new EditorTabViewModel()
/// напрямую из MainViewModel (что нарушает DI), фабрика инкапсулирует создание.
///
/// Регистрация в DI:
///   services.AddSingleton<IEditorTabViewModelFactory, EditorTabViewModelFactory>();
///   services.AddTransient<EditorTabViewModel>();
///
/// Использование:
///   var doc = _factory.Create();
///   _factory.CreateNew("Документ 1", "Содержимое...");
///   _factory.CreateFromFile(filePath);
/// </summary>
public interface IEditorTabViewModelFactory
{
    /// <summary>
    /// Создать новый документ с заданным заголовком и содержимым. Увеличивает счетчик открытых докуметов 
    /// </summary>
    EditorTabViewModel CreateNew(string title, string content);

    /// <summary>Создать документ и загрузить в него содержимое файла.</summary>
    EditorTabViewModel CreateFromFile(string filePath);

    /// <summary>
    /// Загружает состояние загруженного документа
    /// </summary>
    EditorTabViewModel LoadDocumentState(WorkspaceDocument savedDocument);
}

public sealed class EditorTabViewModelFactory : IEditorTabViewModelFactory
{
    private readonly IServiceProvider mServices;

    public EditorTabViewModelFactory(IServiceProvider services)
    {
        mServices = services;
    }

    public EditorTabViewModel CreateNew(string title, string content)
    {
        var doc = mServices.GetRequiredService<EditorTabViewModel>();
        doc.Title = title;
        doc.Content = content;
        doc.IsModified = false;

        return doc;
    }

    public EditorTabViewModel CreateFromFile(string filePath)
    {
        EditorTabViewModel doc = mServices.GetRequiredService<EditorTabViewModel>();
        doc.LoadFile(filePath);

        return doc;
    }

    public EditorTabViewModel LoadDocumentState(WorkspaceDocument savedDocument)
    {
        EditorTabViewModel doc = mServices.GetRequiredService<EditorTabViewModel>();

        doc.ContentId = savedDocument.ContentId;
        doc.Title = savedDocument.Title;
        doc.Content = savedDocument.Content;
        doc.IsModified = savedDocument.IsModified;
        doc.IsActive = savedDocument.IsActive;
        doc.FilePath = savedDocument.FilePath ?? string.Empty;
        
        return doc;
    }
}

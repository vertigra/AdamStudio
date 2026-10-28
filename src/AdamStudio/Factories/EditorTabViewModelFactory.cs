using AdamStudio.Model;
using AdamStudio.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection.Metadata;

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
[Obsolete("Использует view model нужно изменить на T")]
public interface IEditorTabViewModelFactory
{
    /// <summary>Создать пустой документ с заголовком по умолчанию.</summary>
    EditorTabViewModel Create();

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

[Obsolete("Использует view model нужно изменить на T")]
public sealed class EditorTabViewModelFactory : IEditorTabViewModelFactory
{
    private readonly IServiceProvider mServices;

    public EditorTabViewModelFactory(IServiceProvider services)
    {
        mServices = services;
    }

    public EditorTabViewModel Create()
    {
        // GetRequiredService<EditorTabViewModel>() работает, т.к. EditorTabViewModel
        // зарегистрирован как Transient — каждый запрос создаёт новый экземпляр.
        return mServices.GetRequiredService<EditorTabViewModel>();
    }

    public EditorTabViewModel CreateNew(string title, string content)
    {
        var doc = Create();
        doc.Title = $"{title}";
        doc.Content = content;
        doc.IsModified = false;
        doc.IsActive = true;
        return doc;
    }

    public EditorTabViewModel CreateFromFile(string filePath)
    {
        EditorTabViewModel doc = Create();
        doc.LoadFile(filePath);
        doc.IsActive = true;
        return doc;
    }

    public EditorTabViewModel LoadDocumentState(WorkspaceDocument savedDocument)
    {
        var doc = Create();
        doc.ContentId = savedDocument.ContentId;
        doc.Title = savedDocument.Title;
        doc.Content = savedDocument.Content;
        doc.IsModified = savedDocument.IsDirty;
        doc.IsActive = savedDocument.IsActive;
        doc.FilePath = savedDocument.FilePath ?? string.Empty;
        return doc;
    }
}

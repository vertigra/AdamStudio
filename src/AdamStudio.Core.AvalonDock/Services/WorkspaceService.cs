using AdamStudio.Core.AvalonDock.Model;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdamStudio.Services;

public interface IWorkspaceService
{
    WorkspaceState Load(string path);
    void Save(string path, WorkspaceState state);
}

public sealed class WorkspaceService : IWorkspaceService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public WorkspaceState Load(string path)
    {
        if (!File.Exists(path))
            return new WorkspaceState();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<WorkspaceState>(json, Options) ?? new WorkspaceState();
        }
        catch (JsonException)
        {
            return new WorkspaceState();
        }
    }

    public void Save(string path, WorkspaceState state)
    {
        state.SavedAt = DateTime.Now;
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(state, Options);
        File.WriteAllText(path, json);
    }
}

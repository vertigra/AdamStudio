namespace AdamStudio.Core.AvalonDock.Model
{
    /// <summary>
    /// Cериализация рабочего состояния.
    /// </summary>
    public sealed class WorkspaceDocument
    {
        public string ContentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsModified { get; set; }
        public bool IsActive { get; set; }
        public string DocumentTypeName { get; set; } = string.Empty;
    }
}

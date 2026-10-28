namespace AdamStudio.Model
{
    /// <summary>Описание документа для сериализации рабочего состояния.</summary>
    public sealed class WorkspaceDocument
    {
        public string ContentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsDirty { get; set; }
        public bool IsActive { get; set; }
    }
}

namespace AdamStudio.Model
{
    /// <summary>Описание документа для сериализации рабочего состояния.</summary>
    public sealed class WorkspaceDocument
    {
        public string ContentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsModified { get; set; }
        public bool IsActive { get; set; }
    }
}

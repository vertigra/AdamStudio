using System;
using System.Collections.Generic;

namespace AdamStudio.Model
{
    /// <summary>Рабочее состояние приложения — список открытых документов.</summary>
    public sealed class WorkspaceState
    {
        public List<WorkspaceDocument> Documents { get; set; } = new();
        public int NewDocumentCounter { get; set; }
        public DateTime SavedAt { get; set; }
    }
}

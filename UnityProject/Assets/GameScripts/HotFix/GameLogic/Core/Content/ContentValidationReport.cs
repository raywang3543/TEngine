using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GameLogic.Core.Content
{
    /// <summary>
    /// 配置校验问题级别。
    /// </summary>
    public enum ContentValidationSeverity
    {
        Warning,
        Error,
    }

    /// <summary>
    /// 单条配置校验结果。
    /// </summary>
    public sealed class ContentValidationIssue
    {
        public ContentValidationIssue(ContentValidationSeverity severity, string table, string rowId, string message,
            string sourceUrl)
        {
            Severity = severity;
            Table = table;
            RowId = rowId;
            Message = message;
            SourceUrl = sourceUrl;
        }

        public ContentValidationSeverity Severity { get; }
        public string Table { get; }
        public string RowId { get; }
        public string Message { get; }
        public string SourceUrl { get; }

        public override string ToString()
        {
            return $"[{Severity}] {Table}/{RowId}: {Message} ({SourceUrl})";
        }
    }

    /// <summary>
    /// Original 内容配置的完整审计报告。
    /// </summary>
    public sealed class ContentValidationReport
    {
        private readonly List<ContentValidationIssue> _issues = new List<ContentValidationIssue>();

        public IReadOnlyList<ContentValidationIssue> Issues => new ReadOnlyCollection<ContentValidationIssue>(_issues);
        public bool HasErrors => _issues.Any(issue => issue.Severity == ContentValidationSeverity.Error);

        internal void Warning(string table, string rowId, string message, string sourceUrl)
        {
            _issues.Add(new ContentValidationIssue(ContentValidationSeverity.Warning, table, rowId, message, sourceUrl));
        }

        internal void Error(string table, string rowId, string message, string sourceUrl)
        {
            _issues.Add(new ContentValidationIssue(ContentValidationSeverity.Error, table, rowId, message, sourceUrl));
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, _issues.Select(issue => issue.ToString()));
        }
    }

    /// <summary>
    /// 玩法请求了尚未核实的必需数值时抛出的异常。
    /// </summary>
    public sealed class ContentDataUnavailableException : InvalidOperationException
    {
        public ContentDataUnavailableException(string table, string rowId, string field, string sourceUrl)
            : base($"配置 {table}/{rowId} 缺少玩法必需字段 {field}。来源：{sourceUrl}")
        {
            Table = table;
            RowId = rowId;
            Field = field;
            SourceUrl = sourceUrl;
        }

        public string Table { get; }
        public string RowId { get; }
        public string Field { get; }
        public string SourceUrl { get; }
    }
}

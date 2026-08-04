using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GameLogic.Core.Model
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
        public ContentValidationIssue(ContentValidationSeverity severity, string table, string rowId, string message)
        {
            Severity = severity;
            Table = table;
            RowId = rowId;
            Message = message;
        }

        public ContentValidationSeverity Severity { get; }
        public string Table { get; }
        public string RowId { get; }
        public string Message { get; }

        public override string ToString()
        {
            return $"[{Severity}] {Table}/{RowId}: {Message}";
        }
    }

    /// <summary>
    /// Stacklands 运行时配置的可用性校验报告。
    /// </summary>
    public sealed class ContentValidationReport
    {
        private readonly List<ContentValidationIssue> _issues = new List<ContentValidationIssue>();

        public IReadOnlyList<ContentValidationIssue> Issues => new ReadOnlyCollection<ContentValidationIssue>(_issues);
        public bool HasErrors => _issues.Any(issue => issue.Severity == ContentValidationSeverity.Error);

        internal void Warning(string table, string rowId, string message)
        {
            _issues.Add(new ContentValidationIssue(ContentValidationSeverity.Warning, table, rowId, message));
        }

        internal void Error(string table, string rowId, string message)
        {
            _issues.Add(new ContentValidationIssue(ContentValidationSeverity.Error, table, rowId, message));
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, _issues.Select(issue => issue.ToString()));
        }
    }

    /// <summary>
    /// 玩法请求了缺失的必需数值时抛出的异常。
    /// </summary>
    public sealed class ContentDataUnavailableException : InvalidOperationException
    {
        public ContentDataUnavailableException(string table, string rowId, string field)
            : base($"配置 {table}/{rowId} 缺少玩法必需字段 {field}。")
        {
            Table = table;
            RowId = rowId;
            Field = field;
        }

        public string Table { get; }
        public string RowId { get; }
        public string Field { get; }
    }
}

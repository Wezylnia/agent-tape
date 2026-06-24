using System.Net;
using System.Text;
using AgentTape.Core.Abstractions;
using AgentTape.Core.Models;

namespace AgentTape.Reporting.Html;

public sealed class HtmlReportGenerator : IReportGenerator
{
    public string Format => "html";

    public Task<string> GenerateAsync(TapeSession session, CancellationToken cancellationToken)
    {
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine("<title>AgentTape Session Report</title>");
        html.AppendLine("<style>");
        html.AppendLine("body{font-family:Arial,sans-serif;margin:32px;line-height:1.45;color:#1f2933;background:#f7f8fa}");
        html.AppendLine("main{max-width:980px;margin:0 auto;background:white;border:1px solid #d9dee7;border-radius:8px;padding:24px}");
        html.AppendLine("code{background:#eef1f5;padding:2px 4px;border-radius:4px}");
        html.AppendLine(".grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:12px}");
        html.AppendLine(".metric{border:1px solid #d9dee7;border-radius:6px;padding:12px}.metric strong{display:block;font-size:24px}");
        html.AppendLine("li{margin:6px 0}.warning{color:#9a3412}");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body><main>");
        html.AppendLine("<h1>AgentTape Session Report</h1>");
        html.AppendLine($"<p><strong>{Encode(session.Name)}</strong> in <code>{Encode(session.WorkingDirectory)}</code></p>");
        html.AppendLine("<section class=\"grid\">");
        AddMetric(html, "Commands", session.Commands.Count.ToString());
        AddMetric(html, "Files changed", session.FileChanges.Count.ToString());
        AddMetric(html, "Warnings", session.Warnings.Count.ToString());
        AddMetric(html, "Duration", $"{session.Duration.TotalSeconds:0.0}s");
        html.AppendLine("</section>");
        html.AppendLine("<h2>Commands</h2><ol>");
        foreach (var command in session.Commands)
        {
            html.AppendLine($"<li><code>{Encode(command.Command)}</code> exited <strong>{command.ExitCode}</strong></li>");
        }

        html.AppendLine("</ol><h2>Changed Files</h2><ul>");
        foreach (var change in session.FileChanges)
        {
            html.AppendLine($"<li>{Encode(change.Kind.ToString())}: <code>{Encode(change.Path)}</code></li>");
        }

        html.AppendLine("</ul><h2>Risk Warnings</h2><ul>");
        foreach (var warning in session.Warnings)
        {
            html.AppendLine($"<li class=\"warning\"><strong>{Encode(warning.Code)}</strong>: {Encode(warning.Message)}</li>");
        }

        html.AppendLine("</ul></main></body></html>");
        return Task.FromResult(html.ToString());
    }

    private static void AddMetric(StringBuilder html, string label, string value)
    {
        html.AppendLine($"<div class=\"metric\"><span>{Encode(label)}</span><strong>{Encode(value)}</strong></div>");
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}

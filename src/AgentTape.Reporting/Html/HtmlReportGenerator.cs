using System.Net;
using System.Text;
using AgentTape.Core.Abstractions;
using AgentTape.Core.Models;

namespace AgentTape.Reporting.Html;

/// <summary>
/// Generates a self-contained static HTML report from a TapeSession.
/// No external network assets required.
/// </summary>
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
        html.AppendLine($"<title>{Encode(session.Name)} — AgentTape Session Report</title>");
        html.AppendLine("<style>");
        html.AppendLine("body{font-family:system-ui,sans-serif;margin:24px;line-height:1.5;color:#1f2933;background:#f3f4f6}");
        html.AppendLine("main{max-width:1024px;margin:0 auto;background:#fff;border:1px solid #d1d5db;border-radius:8px;padding:28px}");
        html.AppendLine("h1{font-size:1.6em;margin-top:0}h2{font-size:1.2em;border-bottom:1px solid #e5e7eb;padding-bottom:6px;margin-top:24px}");
        html.AppendLine("code{background:#f1f5f9;padding:1px 5px;border-radius:3px;font-size:.9em}");
        html.AppendLine("table{border-collapse:collapse;width:100%}th,td{border:1px solid #e5e7eb;padding:6px 10px;text-align:left}");
        html.AppendLine("th{background:#f8fafc}");
        html.AppendLine(".grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:10px;margin:16px 0}");
        html.AppendLine(".metric{border:1px solid #d1d5db;border-radius:6px;padding:10px;background:#fafbfc}");
        html.AppendLine(".metric .label{font-size:.8em;color:#6b7280}.metric .value{font-size:1.3em;font-weight:700}");
        html.AppendLine(".warn-high{color:#dc2626}.warn-warn{color:#d97706}.warn-info{color:#2563eb}");
        html.AppendLine(".note{font-size:.85em;color:#6b7280;margin-top:20px;border-top:1px solid #e5e7eb;padding-top:12px}");
        html.AppendLine("details{margin:6px 0}summary{cursor:pointer}");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body><main>");

        // Header
        html.AppendLine($"<h1>{Encode(session.Name)} — Session Report</h1>");
        html.AppendLine($"<p>Working directory: <code>{Encode(session.WorkingDirectory)}</code></p>");

        // Metrics row
        html.AppendLine("<div class=\"grid\">");
        AddMetric(html, "Commands", session.Commands.Count.ToString());
        AddMetric(html, "Files Changed", session.FileChanges.Count.ToString());
        AddMetric(html, "Warnings", session.Warnings.Count.ToString());
        AddMetric(html, "Duration", FormatDuration(session.Duration));
        html.AppendLine("</div>");

        if (session.AfterGit is { IsRepository: true })
        {
            html.AppendLine($"<p>Branch: <code>{Encode(session.AfterGit.Branch)}</code> &mdash; HEAD: <code>{Encode(session.AfterGit.HeadSha)}</code></p>");
        }

        // Command timeline
        html.AppendLine("<h2>Command Timeline</h2>");
        if (session.Commands.Count == 0)
        {
            html.AppendLine("<p><em>No commands captured.</em></p>");
        }
        else
        {
            html.AppendLine("<table><tr><th>#</th><th>Command</th><th>Exit</th><th>Duration</th></tr>");
            var idx = 1;
            foreach (var cmd in session.Commands)
            {
                html.AppendLine($"<tr><td>{idx}</td><td><code>{Encode(cmd.Command)}</code></td><td>{cmd.ExitCode}</td><td>{FormatDuration(cmd.Duration)}</td></tr>");
                idx++;
            }
            html.AppendLine("</table>");
        }

        // Changed files
        html.AppendLine("<h2>Changed Files</h2>");

        // Pre-existing changes
        if (session.PreExistingChanges.Count > 0)
        {
            html.AppendLine("<h3>Pre-existing Changes</h3>");
            html.AppendLine("<p><em>These files were already modified before recording started.</em></p>");
            html.AppendLine("<table><tr><th>Kind</th><th>Path</th></tr>");
            foreach (var change in session.PreExistingChanges)
            {
                var path = change.OldPath is not null
                    ? $"{Encode(change.OldPath)} → {Encode(change.Path)}"
                    : Encode(change.Path);
                html.AppendLine($"<tr><td>{change.Kind}</td><td><code>{path}</code></td></tr>");
            }
            html.AppendLine("</table>");
        }

        // Session changes (or fall back to FileChanges for backward compat)
        var sessionChanges = session.SessionChanges.Count > 0 || session.PreExistingChanges.Count > 0
            ? session.SessionChanges
            : session.FileChanges;

        html.AppendLine("<h3>Session Changes</h3>");
        if (sessionChanges.Count == 0 && session.PreExistingChanges.Count == 0 && session.FileChanges.Count == 0)
        {
            html.AppendLine("<p><em>No changed files captured.</em></p>");
        }
        else if (sessionChanges.Count == 0)
        {
            html.AppendLine("<p><em>No new session changes detected.</em></p>");
        }
        else
        {
            html.AppendLine("<table><tr><th>Kind</th><th>Path</th></tr>");
            foreach (var change in sessionChanges)
            {
                var path = change.OldPath is not null
                    ? $"{Encode(change.OldPath)} → {Encode(change.Path)}"
                    : Encode(change.Path);
                html.AppendLine($"<tr><td>{change.Kind}</td><td><code>{path}</code></td></tr>");
            }
            html.AppendLine("</table>");
        }

        // Test signals
        html.AppendLine("<h2>Test Signals</h2>");
        var testSummary = session.TestSummaries.FirstOrDefault(s => s.HasAnySignal);
        if (testSummary?.HasAnySignal == true)
        {
            html.AppendLine("<table><tr><th>Total</th><th>Passed</th><th>Failed</th><th>Skipped</th></tr>");
            html.AppendLine($"<tr><td>{testSummary.Total}</td><td>{testSummary.Passed}</td><td>{testSummary.Failed}</td><td>{testSummary.Skipped}</td></tr>");
            html.AppendLine("</table>");
        }
        else
        {
            html.AppendLine("<p><em>No test signals detected.</em></p>");
        }

        // Risk warnings
        html.AppendLine("<h2>Risk Warnings</h2>");
        if (session.Warnings.Count == 0)
        {
            html.AppendLine("<p><em>No risk warnings detected.</em></p>");
        }
        else
        {
            html.AppendLine("<ul>");
            foreach (var warning in session.Warnings)
            {
                var cls = warning.Severity switch
                {
                    RiskSeverity.High => "warn-high",
                    RiskSeverity.Warning => "warn-warn",
                    _ => "warn-info"
                };
                html.AppendLine($"<li class=\"{cls}\"><strong>{Encode(warning.Code)}</strong>: {Encode(warning.Message)}</li>");
            }
            html.AppendLine("</ul>");
        }

        // Final diff (collapsed)
        if (!string.IsNullOrEmpty(session.AfterGit?.StatusText))
        {
            html.AppendLine("<h2>Final Diff</h2>");
            html.AppendLine("<details>");
            html.AppendLine("<summary>Show diff</summary>");
            html.AppendLine($"<pre><code>{Encode(session.AfterGit.StatusText)}</code></pre>");
            html.AppendLine("</details>");
        }

        // Redaction note
        html.AppendLine($"<p class=\"note\">Report generated by AgentTape. Output redacted with <strong>{session.RedactionMode.ToString().ToLowerInvariant()}</strong> mode. No raw secrets are included.</p>");

        html.AppendLine("</main></body></html>");
        return Task.FromResult(html.ToString());
    }

    private static void AddMetric(StringBuilder html, string label, string value)
    {
        html.AppendLine($"<div class=\"metric\"><div class=\"label\">{Encode(label)}</div><div class=\"value\">{Encode(value)}</div></div>");
    }

    internal static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    internal static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return $"{duration.TotalMilliseconds:0}ms";
        if (duration.TotalSeconds < 60)
            return $"{duration.TotalSeconds:0.0}s";
        return $"{duration.TotalMinutes:0.0}m";
    }
}

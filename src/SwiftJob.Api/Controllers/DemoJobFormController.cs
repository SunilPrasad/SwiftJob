using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace SwiftJob.Api.Controllers;

/// <summary>
/// A local, harmless sample job application form for trying the browser and AI agent. Nothing is stored:
/// submitting only echoes what was received.
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("demo/job-form")]
public sealed class DemoJobFormController : ControllerBase
{
    private const string Style = """
        <style>
          body { font-family: system-ui, sans-serif; background: #f4f6fb; margin: 0; padding: 32px; color: #1d2433; }
          .card { max-width: 560px; margin: 0 auto; background: #fff; border-radius: 12px; padding: 28px 32px;
                  box-shadow: 0 1px 4px rgba(0,0,0,.12); }
          h1 { font-size: 22px; margin: 0 0 4px; } .muted { color: #5b6475; margin: 0 0 20px; }
          label { display: block; font-weight: 600; margin: 14px 0 6px; }
          input[type=text], input[type=email], input[type=tel] { width: 100%; box-sizing: border-box; padding: 10px;
                  border: 1px solid #b8c0cf; border-radius: 6px; font-size: 15px; }
          button { margin-top: 22px; background: #0b57d0; color: #fff; border: 0; border-radius: 20px;
                   padding: 10px 24px; font-size: 15px; cursor: pointer; }
          .banner { background: #fff4ce; border-radius: 8px; padding: 8px 12px; font-size: 13px; margin-bottom: 18px; }
          dt { font-weight: 600; margin-top: 8px; }
        </style>
        """;

    [HttpGet]
    public ContentResult Form() => Html($$"""
        <!doctype html><html lang="en"><head><meta charset="utf-8"><title>Apply – Senior .NET Developer</title>{{Style}}</head>
        <body><div class="card">
          <div class="banner">SwiftJob sample form – runs on your PC, nothing is sent anywhere.</div>
          <h1>Senior .NET Developer</h1>
          <p class="muted">Example Ltd · London (Hybrid)</p>
          <form method="post" enctype="multipart/form-data" action="/demo/job-form">
            <label for="firstName">First name *</label>
            <input id="firstName" name="firstName" type="text" required>
            <label for="lastName">Last name *</label>
            <input id="lastName" name="lastName" type="text" required>
            <label for="email">Email *</label>
            <input id="email" name="email" type="email" required>
            <label for="phone">Phone</label>
            <input id="phone" name="phone" type="tel">
            <label for="cv">CV (PDF) *</label>
            <input id="cv" name="cv" type="file" accept=".pdf,.doc,.docx" required>
            <button type="submit">Submit application</button>
          </form>
        </div></body></html>
        """);

    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public ContentResult Submit([FromForm] string? firstName, [FromForm] string? lastName, [FromForm] string? email,
        [FromForm] string? phone, IFormFile? cv) => Html($$"""
        <!doctype html><html lang="en"><head><meta charset="utf-8"><title>Application received</title>{{Style}}</head>
        <body><div class="card">
          <div class="banner">Sample form – this data was not stored.</div>
          <h1>Thank you, your application was received</h1>
          <dl>
            <dt>Name</dt><dd>{{E(firstName)}} {{E(lastName)}}</dd>
            <dt>Email</dt><dd>{{E(email)}}</dd>
            <dt>Phone</dt><dd>{{E(phone)}}</dd>
            <dt>CV</dt><dd>{{(cv is null ? "not attached" : $"{E(cv.FileName)} ({cv.Length:N0} bytes)")}}</dd>
          </dl>
          <p><a href="/demo/job-form">Back to the form</a></p>
        </div></body></html>
        """);

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static ContentResult Html(string html) => new() { Content = html, ContentType = "text/html; charset=utf-8" };
}

using System.Net;
using System.Text;

namespace SkillsetsBackend.Application.Notifications;

/// <summary>
/// The shared SkillSets shell every notification email is rendered into - branded header, content
/// card, optional detail rows, call-to-action button and footer - so the whole set looks like one
/// system instead of each handler inventing its own markup.
///
/// Built as nested tables with inline styles because that is what mail clients actually render:
/// Outlook ignores flex/grid and strips most external CSS. The one &lt;style&gt; block carries only
/// progressive enhancement - clients that honour it get the mobile stacking, clients that drop it
/// still get a readable single-column layout, since the base inline styles already work at any width.
/// </summary>
public static class EmailLayout
{
    private const string Brand = "#c81322";
    private const string Ink = "#1a1918";
    private const string Muted = "#6b6663";
    private const string Line = "#e7e5e3";
    private const string Panel = "#f7f6f5";
    private const string Page = "#f4f4f5";

    public record DetailRow(string Label, string Value);

    /// <param name="preheader">The one-line summary a mail client shows next to the subject in the
    /// inbox list. Hidden inside the message itself - without it clients fall back to scraping the
    /// first visible text, which is the greeting and tells the reader nothing.</param>
    public static string Render(
        string kicker,
        string preheader,
        string greeting,
        string intro,
        IReadOnlyList<DetailRow> details,
        string? ctaLabel,
        string? ctaUrl,
        string? footerNote)
    {
        var sb = new StringBuilder();

        sb.Append($@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>SkillSets</title>
<style>
  /* Progressive enhancement only - the inline styles below already render correctly without this. */
  @media only screen and (max-width: 600px) {{
    .sk-wrap {{ padding: 16px 12px !important; }}
    .sk-pad {{ padding: 22px 20px !important; }}
    .sk-label {{ display: block !important; width: 100% !important; padding-bottom: 2px !important; }}
    .sk-value {{ display: block !important; width: 100% !important; padding-bottom: 12px !important; }}
    .sk-cta a {{ display: block !important; }}
  }}
  @media (prefers-color-scheme: dark) {{
    .sk-card {{ background: #ffffff !important; }}
  }}
</style>
</head>
<body style=""margin:0;padding:0;background:{Page};"">
<div style=""display:none;max-height:0;overflow:hidden;opacity:0;"">{Esc(preheader)}</div>
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:{Page};"">
  <tr>
    <td class=""sk-wrap"" align=""center"" style=""padding:32px 16px;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;"">
      <table role=""presentation"" class=""sk-card"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width:600px;background:#ffffff;border:1px solid {Line};border-radius:12px;overflow:hidden;"">
        <tr>
          <td style=""background:{Brand};padding:22px 28px;"">
            <div style=""color:#ffffff;font-size:19px;font-weight:700;letter-spacing:0.5px;"">SKILLSETS</div>
            <div style=""color:#ffd9dc;font-size:12px;margin-top:4px;"">{Esc(kicker)}</div>
          </td>
        </tr>
        <tr>
          <td class=""sk-pad"" style=""padding:28px;"">
            <p style=""margin:0 0 14px;color:{Ink};font-size:15px;line-height:1.6;"">{Esc(greeting)}</p>
            <p style=""margin:0 0 22px;color:{Ink};font-size:14px;line-height:1.65;"">{intro}</p>");

        if (details.Count > 0)
        {
            sb.Append($@"
            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:{Panel};border:1px solid {Line};border-radius:10px;margin:0 0 24px;"">
              <tr><td style=""padding:6px 20px 6px;"">
                <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">");

            foreach (var row in details)
            {
                sb.Append($@"
                  <tr>
                    <td class=""sk-label"" style=""padding:10px 12px 10px 0;color:{Muted};font-size:11px;font-weight:600;letter-spacing:0.6px;text-transform:uppercase;vertical-align:top;white-space:nowrap;"">{Esc(row.Label)}</td>
                    <td class=""sk-value"" style=""padding:10px 0;color:{Ink};font-size:14px;line-height:1.5;vertical-align:top;word-break:break-word;"">{Esc(row.Value)}</td>
                  </tr>");
            }

            sb.Append(@"
                </table>
              </td></tr>
            </table>");
        }

        if (!string.IsNullOrWhiteSpace(ctaLabel) && !string.IsNullOrWhiteSpace(ctaUrl))
        {
            sb.Append($@"
            <table role=""presentation"" class=""sk-cta"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin:0 0 22px;"">
              <tr><td align=""center"">
                <a href=""{Esc(ctaUrl)}"" style=""display:inline-block;background:{Brand};color:#ffffff;text-decoration:none;font-size:15px;font-weight:600;padding:14px 32px;border-radius:8px;"">{Esc(ctaLabel)}</a>
              </td></tr>
            </table>
            <p style=""margin:0 0 4px;color:{Muted};font-size:12px;line-height:1.6;text-align:center;"">
              Button not working? Copy this link into your browser:<br>
              <a href=""{Esc(ctaUrl)}"" style=""color:{Brand};word-break:break-all;"">{Esc(ctaUrl)}</a>
            </p>");
        }

        if (!string.IsNullOrWhiteSpace(footerNote))
        {
            sb.Append($@"
            <p style=""margin:18px 0 0;padding-top:18px;border-top:1px solid {Line};color:{Muted};font-size:12px;line-height:1.65;"">{footerNote}</p>");
        }

        sb.Append($@"
          </td>
        </tr>
        <tr>
          <td style=""background:{Panel};border-top:1px solid {Line};padding:18px 28px;"">
            <div style=""color:{Ink};font-size:12px;font-weight:600;"">SkillSets Support</div>
            <div style=""color:{Muted};font-size:11px;line-height:1.6;margin-top:3px;"">
              Need a hand? Reply to this email and our support team will help you out.
            </div>
          </td>
        </tr>
      </table>
      <div style=""max-width:600px;color:{Muted};font-size:11px;line-height:1.6;padding:14px 8px 0;"">
        You received this email because you have a SkillSets account.
      </div>
    </td>
  </tr>
</table>
</body>
</html>");

        return sb.ToString();
    }

    /// <summary>Bold, brand-coloured emphasis for a value dropped into a sentence. Escapes the value
    /// itself, so callers can safely interpolate the result into an intro string.</summary>
    public static string Strong(string value) => $@"<strong style=""color:{Ink};"">{Esc(value)}</strong>";

    private static string Esc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}

using System.Net;
using SkillsetsBackend.Application.Auth.Interfaces;

namespace SkillsetsBackend.Application.Auth;

/// <summary>
/// Sends the "your account has been created" email to a newly created Company Admin, Manager or
/// Employee. One place rather than one copy per creation handler, since all three send the exact
/// same message and only differ in who they are addressed to.
///
/// Delivery is best-effort by design: a failure here is swallowed so it can never roll back or fail
/// an account that was already created successfully. The send is recorded in Email History either
/// way (see IEmailSender), which is where a failed delivery is diagnosed from.
/// </summary>
public class AccountWelcomeEmail
{
    /// <summary>Where a new account holder actually signs in - deliberately the customer-facing
    /// portal, not whichever internal admin host created the account.</summary>
    private const string PortalLoginUrl = "https://dashboard.skillsetsonline.com/login";

    private readonly IEmailSender _emailSender;

    public AccountWelcomeEmail(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    /// <param name="password">The password actually assigned to the account - auto-generated or
    /// typed by the admin. Passed in rather than re-derived so the email can never disagree with
    /// what was stored.</param>
    public async Task SendAsync(
        string toEmail,
        string? firstName,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _emailSender.SendAsync(
                toEmail,
                firstName,
                "Welcome to SkillSets - your account is ready",
                BuildBody(firstName ?? "there", toEmail, password),
                purpose: "AccountCreated",
                cancellationToken: cancellationToken);
        }
        catch
        {
            // Never let a mail problem surface as a failed account creation - the account exists and
            // is usable, and the admin can resend credentials from the account itself.
        }
    }

    private static string BuildBody(string firstName, string email, string password) => $$"""
        <div style="background-color:#f4f4f5;padding:32px 16px;font-family:'Segoe UI',Helvetica,Arial,sans-serif;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e7e5e3;">
            <tr>
              <td style="background:#c81322;padding:22px 28px;">
                <div style="color:#ffffff;font-size:18px;font-weight:700;letter-spacing:0.5px;">SKILLSETS</div>
                <div style="color:#ffd9dc;font-size:12px;margin-top:4px;">Your account is ready</div>
              </td>
            </tr>
            <tr>
              <td style="padding:28px;">
                <p style="margin:0 0 16px;color:#1a1918;font-size:14px;line-height:1.6;">Hi {{WebUtility.HtmlEncode(firstName)}},</p>
                <p style="margin:0 0 20px;color:#1a1918;font-size:14px;line-height:1.6;">
                  Welcome to SkillSets! Your account has been successfully created. You can use the credentials below to access your SkillSets account.
                </p>

                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f7f6f5;border:1px solid #e7e5e3;border-radius:10px;margin-bottom:20px;">
                  <tr>
                    <td style="padding:16px 20px;">
                      <div style="color:#6b6663;font-size:11px;font-weight:600;letter-spacing:0.6px;text-transform:uppercase;">Email</div>
                      <div style="color:#1a1918;font-size:14px;margin-top:4px;word-break:break-all;">{{WebUtility.HtmlEncode(email)}}</div>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:0 20px 16px;">
                      <div style="color:#6b6663;font-size:11px;font-weight:600;letter-spacing:0.6px;text-transform:uppercase;">Password</div>
                      <div style="font-size:24px;font-weight:700;letter-spacing:4px;color:#c81322;margin-top:6px;">{{WebUtility.HtmlEncode(password)}}</div>
                    </td>
                  </tr>
                </table>

                <div style="text-align:center;margin-bottom:20px;">
                  <a href="{{PortalLoginUrl}}" style="display:inline-block;background:#c81322;color:#ffffff;text-decoration:none;font-size:14px;font-weight:600;padding:12px 28px;border-radius:8px;">Sign in to SkillSets</a>
                </div>

                <p style="margin:0 0 8px;color:#6b6663;font-size:12px;line-height:1.6;">
                  Or go to <a href="{{PortalLoginUrl}}" style="color:#c81322;">{{PortalLoginUrl}}</a> and sign in with the details above.
                </p>
                <p style="margin:0;color:#6b6663;font-size:12px;line-height:1.6;">
                  For your security, please change your password after your first sign-in.
                </p>
              </td>
            </tr>
          </table>
        </div>
        """;
}

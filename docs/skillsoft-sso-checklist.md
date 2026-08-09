# Skillsoft/Percipio SAML 2.0 SSO — configuration checklist

## What's already built

`SkillsoftSsoService` (`src/Infrastructure/Skillsoft/`) implements the full flow: it validates
the caller's active company membership, their `ActiveLibraryCards` entitlement for that company
(joined by email + `Company_Code`, date-range checked), then builds and digitally signs a SAML 2.0
`<samlp:Response>` and returns it as an auto-submitting HTML form (HTTP-POST binding) that the
browser posts straight to Skillsoft's ACS URL. `GET /api/v1/skillsoft/launch-ticket` (Bearer-auth)
issues a short-lived, single-use ticket; `GET /api/v1/skillsoft/launch?ticket=...` (the browser
navigation target) consumes it and returns that HTML. No Skillsoft credentials are read, stored,
or transmitted anywhere in this flow — `ActiveLibraryCards.Password` isn't even mapped in code.

**We act as the SAML Identity Provider (IdP). Skillsoft/Percipio is the Service Provider (SP).**
This matches Skillsoft's own documentation (confirmed by fetching their docs directly): Percipio
expects a corporate IdP to send it signed assertions; it doesn't send us AuthnRequests first for
this kind of "launch from our app" flow.

## What is NOT yet configured (all of it must come from Skillsoft, not invented)

Nothing below has a real value yet. `SkillsoftSsoService` fails with a clear, actionable error the
moment `/launch-ticket` or `/launch` is called if these are missing — it does not fail at app
startup, since this is an optional integration.

Set these under the `SkillsoftSso` section (`appsettings.json` for non-secrets, **user-secrets or
an environment variable for the certificate/password** — never commit those):

| Setting | Source | Notes |
|---|---|---|
| `SkillsoftSso:IdpEntityId` | **You choose this** | Any stable URI identifying this app as an IdP, e.g. `https://skillsetsonline.com/saml/idp`. Give it to Skillsoft during onboarding. |
| `SkillsoftSso:SkillsoftAcsUrl` | **Ask Skillsoft** | The Assertion Consumer Service URL to POST signed responses to. Confirmed by their docs to be per-organization, not a fixed public value. |
| `SkillsoftSso:SkillsoftSpEntityId` | **Ask Skillsoft** | Their SP Entity ID, used as the assertion's audience restriction. |
| `SkillsoftSso:NameIdFormat` | **Ask Skillsoft** | Defaults to `urn:oasis:names:tc:SAML:2.0:nameid-format:unspecified`. Skillsoft's docs only say the NameID must be a "static, never-changing" value — they don't publish a required *format* string. Confirm before go-live. |
| `SkillsoftSso:FirstNameAttributeName` / `LastNameAttributeName` / `EmailAttributeName` | **Ask Skillsoft** | Default to `FirstName`/`LastName`/`Email`. Skillsoft's own SSO attribute-mapping doc explicitly says: *"confirm the exact attribute name from your IdP SAML assertion and with your Skillsoft Percipio Platform account team"* — these are case-sensitive. Do not assume the defaults are right. |
| `SkillsoftSso:SigningCertificateBase64` + `SigningCertificatePassword` (or `SigningCertificatePath`) | **You generate this** | A real X.509 cert+private key (PFX) used to sign assertions. Give Skillsoft the **public** certificate/IdP metadata; keep the private key secret, in user-secrets/environment config only. |

## Questions to put directly to Skillsoft's account/support team before go-live

1. Is SAML 2.0 SSO enabled for our organization already, or does their team need to turn it on?
2. What is our organization's ACS URL and SP Entity ID?
3. What exact attribute names do they expect for first name, last name, and email (case-sensitive)?
4. What NameID format do they expect, and do they confirm the Skillsoft `User_ID` (e.g.
   `12LC322511`) from `ActiveLibraryCards` is the correct stable identifier to use as NameID for
   this organization — per their own guidance to prefer a static ID over email?
5. Where do we upload our IdP metadata / public signing certificate in their admin console?
6. Is Just-In-Time (JIT) account creation/update enabled for us, or do learner accounts need to be
   provisioned in Percipio ahead of time?
7. Is there a specific deep-link/launch URL format for landing a user directly in the course
   catalog after SSO, or does Percipio's own post-login landing page suffice?

## How to test the plumbing before Skillsoft confirms the above

Set `SkillsoftSso:AllowDevSelfSignedCertificate: true` (Development environment only — this is
read and enforced in code, not just convention) to generate a throwaway local signing certificate,
so the full request/response/signing code path can be exercised end-to-end. Skillsoft will reject
an assertion signed by this certificate (they've never seen its public key) — it only proves our
side produces well-formed, correctly signed SAML. `SkillsoftAcsUrl`/`SkillsoftSpEntityId` still
need *some* value to test against (e.g. a SAML tracer/test SP) even with the dev certificate.

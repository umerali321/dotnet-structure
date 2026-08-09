# SkillSets Online → Skillsoft/Percipio SAML 2.0 SSO — setup request

Send this to Skillsoft's account/support team to enable SAML SSO from our application into
Skillport/Percipio.

## Our IdP details

| Field | Value |
|---|---|
| IdP Entity ID (Issuer) | `https://skillsetsonline.com/saml/idp` |
| Binding used | HTTP-POST (IdP-initiated) |
| NameID we send | The learner's Skillsoft User ID (e.g. `12LC322511`) — a stable, per-user identifier we already track for entitlement, **not** their email address |
| Attributes we send | `FirstName`, `LastName`, `Email` (exact names below still need your confirmation) |
| Signature algorithm | RSA-SHA256 |

## Our signing certificate (public only — safe to share)

Please register this as our IdP's signing certificate. Valid five years (Aug 2026 – Aug 2031).

```
-----BEGIN CERTIFICATE-----
MIIDEzCCAfugAwIBAgIQXHNte2Gj2aFFcCyJtH3snzANBgkqhkiG9w0BAQsFADAsMSowKAYDVQQD
DCFTa2lsbFNldHMgT25saW5lIFNBTUwgSWRQIFNpZ25pbmcwHhcNMjYwODA5MTg1ODA1WhcNMzEw
ODA5MTkwODA0WjAsMSowKAYDVQQDDCFTa2lsbFNldHMgT25saW5lIFNBTUwgSWRQIFNpZ25pbmcw
ggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDJxnXVbn856KeM4oeEwLIg4ZNcQEvUiIHQ
c14JTSbzhEC4FLsjOtdFyIT7tNGESFyv3KrCOnJy7DOGTJFOQUsRdQ+XTJo5HODE/U1OXApmFX2x
kUm/DlCydEy7T5bdVHpTKdwLs9DU95sV3Ovld00adq9umGgm4UOkTQN0N3FhPuA9EZY2mlo/WrgV
M6bfbD8VXWz6JhV5Ob6+Xe2UmSdBRboO29tJej7XFqzlrTakEzm9p+/Z52kWXJzC2XyYq+S9tN+W
++dRV0mNEuXRQO0UClD/cuYaLoVWZGvHW9RjY0dr1468G+OTspAr2J7IdIW47JoeQfjYNatGKLEv
bnv9AgMBAAGjMTAvMA4GA1UdDwEB/wQEAwIHgDAdBgNVHQ4EFgQU1eSsHdUARI4oYob2240iD2Ul
tV4wDQYJKoZIhvcNAQELBQADggEBAANCJeK6VtesYfRWxhNgJzCzBsFgdisHPWarIC9sOukM/J5I
AOjKdEPTk5PJ1WjRisKmPszhADlN6+vpS2AK+0h78x7ALxx170YDUU6HGuJEb1EFm1S7gZgd/Ytp
x1uGUOuYUL7QM56ko/3u50btpXO7+klliBAGa5Yh92nJh6ILSSS1UawLAqyb+8i6ESSij53jhNdd
jTVQ2Qli/wcNUonwdPXvevkJzC8FrJQmNuGRqC5Vwfhn74CiVDqdRY8idSNTRVZLJ9I9MS40BkIn
7z8UoOb4s1JjUwD5aKqdGC+dWIzVsQjVn8FDM/7gIYyDUaMSRjgZKevg1kLjvlSjR7Q=
-----END CERTIFICATE-----
```

- **Subject:** `CN=SkillSets Online SAML IdP Signing`
- **Thumbprint:** `617BDC17C87AE43010F088E367B2285311C7CD18`
- **Valid:** Aug 9, 2026 – Aug 9, 2031

This is the **public** certificate only — safe to send by email. The private key never leaves our
server.

## What we need back from you

1. Is SAML 2.0 SSO already enabled for our organization, or does it need to be turned on?
2. Our organization's **ACS URL** and **SP Entity ID** for the SAML integration — we've been
   testing against `https://skillsetsonline.skillport.com/skillportfe/login.action`, please
   confirm whether that is in fact the correct ACS endpoint for SAML POSTs, or whether there's a
   dedicated SSO consumer URL.
3. The exact (case-sensitive) attribute names you expect for first name, last name, and email —
   we're currently sending `FirstName`, `LastName`, `Email` but your own documentation says these
   must be confirmed with your account team before go-live.
4. What NameID format do you expect? We're currently sending
   `urn:oasis:names:tc:SAML:2.0:nameid-format:unspecified`.
5. Please confirm the Skillsoft User ID we already maintain (e.g. `12LC322511`) is the correct,
   stable identifier to use as the NameID for our organization.
6. Where do we upload/register the certificate above in your admin console (or should we send it
   to you directly, as done here)?
7. Is Just-In-Time (JIT) account creation/update enabled for us, or do learner accounts need to
   already exist in Percipio ahead of time?
8. Is there a specific deep-link URL to land a user directly in the course catalog after SSO, or
   does your standard post-login landing page suffice?

## Once confirmed

We'll update our configuration with your ACS URL, SP Entity ID, and confirmed attribute
names/NameID format — no further certificate or code changes should be needed on our side.

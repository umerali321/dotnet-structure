using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using SkillsetsBackend.Application.Settings.Interfaces;

namespace SkillsetsBackend.Infrastructure.Email;

/// <inheritdoc cref="ISmtpConnectionTester"/>
public class SmtpConnectionTester : ISmtpConnectionTester
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public async Task<SmtpConnectionTestResult> TestAsync(
        string host, int port, bool enableSsl, string username, string password, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(Timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var ct = linkedCts.Token;

        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(host, port, ct);

            Stream stream = tcpClient.GetStream();
            SslStream? sslStream = null;

            // Port 465 is "implicit TLS" - the whole connection is encrypted from the first byte, no
            // STARTTLS handshake. Everything else (587, 25, custom) starts in plaintext and upgrades
            // via STARTTLS when EnableSsl is requested.
            if (port == 465)
            {
                sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
                await sslStream.AuthenticateAsClientAsync(host);
                stream = sslStream;
            }

            using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
            await using var writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true) { AutoFlush = true, NewLine = "\r\n" };

            var greeting = await ReadResponseAsync(reader, ct);
            if (!greeting.Code.StartsWith('2'))
            {
                return new SmtpConnectionTestResult(false, $"Server did not greet properly: {greeting.Text}");
            }

            var ehlo = await SendAndReadAsync(writer, reader, "EHLO skillsets.local", ct);
            if (!ehlo.Code.StartsWith('2'))
            {
                return new SmtpConnectionTestResult(false, $"EHLO was rejected: {ehlo.Text}");
            }

            if (enableSsl && port != 465)
            {
                var startTls = await SendAndReadAsync(writer, reader, "STARTTLS", ct);
                if (!startTls.Code.StartsWith('2'))
                {
                    return new SmtpConnectionTestResult(false, $"STARTTLS was rejected: {startTls.Text}");
                }

                sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
                await sslStream.AuthenticateAsClientAsync(host);
                stream = sslStream;

                // SMTP requires EHLO to be re-sent once the connection is encrypted - the server's
                // pre-TLS EHLO response cannot be trusted (a plaintext MITM could have altered it).
                using var tlsReader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
                await using var tlsWriter = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true) { AutoFlush = true, NewLine = "\r\n" };

                var tlsEhlo = await SendAndReadAsync(tlsWriter, tlsReader, "EHLO skillsets.local", ct);
                if (!tlsEhlo.Code.StartsWith('2'))
                {
                    return new SmtpConnectionTestResult(false, $"EHLO after STARTTLS was rejected: {tlsEhlo.Text}");
                }

                return await AuthenticateAsync(tlsWriter, tlsReader, username, password, ct);
            }

            return await AuthenticateAsync(writer, reader, username, password, ct);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return new SmtpConnectionTestResult(false, "Connection timed out - check the host and port.");
        }
        catch (SocketException ex)
        {
            return new SmtpConnectionTestResult(false, $"Could not connect: {ex.Message}");
        }
        catch (AuthenticationException ex)
        {
            return new SmtpConnectionTestResult(false, $"TLS handshake failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new SmtpConnectionTestResult(false, $"Unexpected error: {ex.Message}");
        }
    }

    private static async Task<SmtpConnectionTestResult> AuthenticateAsync(
        StreamWriter writer, StreamReader reader, string username, string password, CancellationToken ct)
    {
        var authStart = await SendAndReadAsync(writer, reader, "AUTH LOGIN", ct);
        if (!authStart.Code.StartsWith('3'))
        {
            return new SmtpConnectionTestResult(false, $"Server does not support AUTH LOGIN: {authStart.Text}");
        }

        var userResponse = await SendAndReadAsync(writer, reader, Convert.ToBase64String(Encoding.UTF8.GetBytes(username)), ct);
        if (!userResponse.Code.StartsWith('3'))
        {
            return new SmtpConnectionTestResult(false, $"Username was rejected: {userResponse.Text}");
        }

        var passResponse = await SendAndReadAsync(writer, reader, Convert.ToBase64String(Encoding.UTF8.GetBytes(password)), ct);
        await SendAndReadAsync(writer, reader, "QUIT", ct, expectResponse: false);

        return passResponse.Code.StartsWith('2')
            ? new SmtpConnectionTestResult(true, "Connected and authenticated successfully.")
            : new SmtpConnectionTestResult(false, $"Authentication failed: {passResponse.Text}");
    }

    private static async Task<(string Code, string Text)> SendAndReadAsync(
        StreamWriter writer, StreamReader reader, string command, CancellationToken ct, bool expectResponse = true)
    {
        await writer.WriteLineAsync(command.AsMemory(), ct);
        return expectResponse ? await ReadResponseAsync(reader, ct) : ("221", string.Empty);
    }

    /// <summary>A multi-line SMTP response looks like "250-First\r\n250-Second\r\n250 Last\r\n" - only
    /// the final line (a space, not a hyphen, after the code) ends the response.</summary>
    private static async Task<(string Code, string Text)> ReadResponseAsync(StreamReader reader, CancellationToken ct)
    {
        string? line;
        string code = "000";
        var lastText = string.Empty;

        do
        {
            line = await reader.ReadLineAsync(ct);
            if (line is null || line.Length < 3)
            {
                return ("000", "Connection closed unexpectedly.");
            }

            code = line[..3];
            lastText = line.Length > 4 ? line[4..] : string.Empty;
        }
        while (line.Length > 3 && line[3] == '-');

        return (code, lastText);
    }
}

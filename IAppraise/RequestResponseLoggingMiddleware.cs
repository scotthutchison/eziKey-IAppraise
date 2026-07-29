using System.Diagnostics;
using System.Text;

namespace IAppraise
{
    /// <summary>
    /// Logs each inbound request and its response — method, path, status, elapsed ms, and the
    /// request / response bodies. Bodies are capped to prevent runaway logs. Sensitive headers
    /// (auth tokens) are redacted before logging.
    /// </summary>
    public class RequestResponseLoggingMiddleware
    {
        private const int MaxBodyBytesLogged = 8 * 1024; // 8 KiB per direction — plenty for our payloads

        private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "X-Tdl-Api-Token",
            "X-Api-Key",
        };

        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _log;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> log)
        {
            _next = next;
            _log = log;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip swagger UI static assets from body-logging — they're noisy and never carry secrets.
            var path = context.Request.Path.Value ?? "";
            if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var sw = Stopwatch.StartNew();
            var requestBody = await ReadRequestBodyAsync(context.Request);

            // Buffer the response so we can read what we're about to send back.
            var originalBody = context.Response.Body;
            using var responseBuffer = new MemoryStream();
            context.Response.Body = responseBuffer;

            Exception? failure = null;
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            responseBuffer.Seek(0, SeekOrigin.Begin);
            var responseBody = ReadWithCap(responseBuffer);
            responseBuffer.Seek(0, SeekOrigin.Begin);
            await responseBuffer.CopyToAsync(originalBody);
            context.Response.Body = originalBody;

            sw.Stop();
            var headers = FormatHeaders(context.Request.Headers);

            if (failure != null)
            {
                _log.LogError(failure,
                    "IN  {Method} {Path} -> UNHANDLED after {Elapsed}ms | headers: {Headers} | reqBody: {ReqBody}",
                    context.Request.Method, path, sw.ElapsedMilliseconds, headers, requestBody);
                throw failure;
            }

            var status = context.Response.StatusCode;
            var level = status >= 500 ? LogLevel.Error
                      : status >= 400 ? LogLevel.Warning
                      : LogLevel.Information;

            _log.Log(level,
                "IN  {Method} {Path} -> {Status} in {Elapsed}ms | headers: {Headers} | reqBody: {ReqBody} | resBody: {ResBody}",
                context.Request.Method, path, status, sw.ElapsedMilliseconds, headers, requestBody, responseBody);
        }

        private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            if (!request.Body.CanRead) return "(unreadable)";
            request.EnableBuffering();
            request.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
            var buffer = new char[MaxBodyBytesLogged];
            var read = await reader.ReadAsync(buffer, 0, buffer.Length);
            request.Body.Seek(0, SeekOrigin.Begin);

            var body = new string(buffer, 0, read);
            var truncated = reader.Peek() >= 0;
            return truncated ? body + $" …[truncated at {MaxBodyBytesLogged} chars]" : body;
        }

        private static string ReadWithCap(MemoryStream stream)
        {
            var length = (int)stream.Length;
            var toRead = Math.Min(length, MaxBodyBytesLogged);
            var buf = new byte[toRead];
            stream.Read(buf, 0, toRead);
            var body = Encoding.UTF8.GetString(buf);
            return length > MaxBodyBytesLogged ? body + $" …[truncated at {MaxBodyBytesLogged} bytes]" : body;
        }

        private static string FormatHeaders(IHeaderDictionary headers)
        {
            var parts = headers.Select(h => SensitiveHeaders.Contains(h.Key)
                ? $"{h.Key}=***"
                : $"{h.Key}={string.Join(",", h.Value.ToArray())}");
            return string.Join("; ", parts);
        }
    }
}

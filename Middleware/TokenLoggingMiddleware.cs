public class TokenLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public TokenLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (!string.IsNullOrEmpty(token))
        {
            Console.WriteLine($"Token detected: {token}");
        }

        await _next(context);
    }
}

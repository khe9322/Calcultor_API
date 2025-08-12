using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net.Http.Headers;
using Calcultor_API.Utils;

namespace Calcultor_API.Controllers
{
    [ApiController]
    [Route("calc")]
    [Produces("application/json")]
    public class CalculateController : ControllerBase
    {
        private readonly ILogger<CalculateController> _logger;
        private const string ValidToken = "secret_token_123";

        public CalculateController(ILogger<CalculateController> logger) => _logger = logger;

        private bool TryAuthorize(out IActionResult? unauthorized)
        {
            unauthorized = null;
            if (!Request.Headers.TryGetValue("Authorization", out var authValue) ||
                !AuthenticationHeaderValue.TryParse(authValue, out var authHeader) ||
                !string.Equals(authHeader.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(authHeader.Parameter))
            {
                unauthorized = Unauthorized(new { error = "Authorization header missing or malformed" });
                return false;
            }
            if (!string.Equals(authHeader.Parameter, ValidToken, StringComparison.Ordinal))
            {
                unauthorized = Unauthorized(new { error = "Invalid token" });
                return false;
            }
            return true;
        }

        private static bool TryParseInvariant(string s, out double value) =>
            double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);

        private IActionResult DoOp(string endpoint, string num1Str, string num2Str, Func<double, double, double> op, string symbol)
        {
            if (!TryAuthorize(out var unauth)) return unauth!;

            if (string.IsNullOrWhiteSpace(num1Str) || string.IsNullOrWhiteSpace(num2Str))
                return BadRequest(new { error = "num1 and num2 are required." });

            if (!TryParseInvariant(num1Str, out var a) || !TryParseInvariant(num2Str, out var b))
                return BadRequest(new { error = "Invalid number." });

            if (symbol == "/" && b == 0)
                return BadRequest(new { error = "Cannot divide by zero." });

            try
            {
                // 1. 연산 결과 계산
                var result = op(a, b).ToString(CultureInfo.InvariantCulture);

                ServerLog.Request(symbol, num1Str, num2Str);
                ServerLog.Response(result);

                // 2. 요청/응답 로깅용 문자열 준비
                var prettyReq = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}요청 :\n{{\n  op = \"{symbol}\"\n  num1 = \"{num1Str}\"\n  num2 = \"{num2Str}\"\n}}\n";
                var prettyRes = $"응답 :\n{{\n  result = \"{result}\"\n}}\n";

                // 4. 응답 객체 생성
                var response = new
                {
                    op = symbol,
                    num1 = a.ToString(CultureInfo.InvariantCulture),
                    num2 = b.ToString(CultureInfo.InvariantCulture),
                    result
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                ServerLog.Error(ex.Message);

                _logger.LogWarning(ex, "Compute failed on {Endpoint} ({Num1},{Num2})", endpoint, num1Str, num2Str);
                Console.WriteLine($"[ERR] {endpoint} {ex.Message}");
                return BadRequest(new { error = "Invalid expression" });
            }
        }

        [HttpGet("add")]
        public IActionResult Add([FromQuery] string num1, [FromQuery] string num2)
            => DoOp("/calc/add", num1, num2, (a, b) => a + b, "+");

        [HttpGet("sub")]
        public IActionResult Sub([FromQuery] string num1, [FromQuery] string num2)
            => DoOp("/calc/sub", num1, num2, (a, b) => a - b, "-");

        [HttpGet("mul")]
        public IActionResult Mul([FromQuery] string num1, [FromQuery] string num2)
            => DoOp("/calc/mul", num1, num2, (a, b) => a * b, "*");

        [HttpGet("div")]
        public IActionResult Div([FromQuery] string num1, [FromQuery] string num2)
            => DoOp("/calc/div", num1, num2, (a, b) => a / b, "/");
    }
}

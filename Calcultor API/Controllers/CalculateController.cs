using Microsoft.AspNetCore.Mvc;
using Calcultor_API.Models;

namespace Calcultor_API.Controllers
{
    [ApiController]             // API Controller 생성
    [Route("[controller]")]     // Route 설정 (예: /calculate)
    public class CalculateController : ControllerBase
    {
        private const string ValidToken = "secret_token_123";       //  유효한 토큰 설정

        [HttpPost]
        public IActionResult Post([FromBody] ExpressionRequest request)           // POST 메서드 생성, IActionResult로 응답 반환
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();              // Authorization 헤더에서 토큰 추출
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { error = "Authorization header missing or malformed" });
            }

            var token = authHeader.Replace("Bearer ", "").Trim();

            if (token != ValidToken)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Expression))
                    return BadRequest(new { error = "Expression is required." });
            try
            {
                string cleanexpression = request.Expression
                    .Replace("×", "*")
                    .Replace("÷", "/");

                var result = new System.Data.DataTable().Compute(cleanexpression, null);

                if (double.TryParse(result.ToString(), out double numericResult) && double.IsInfinity(numericResult))
                {
                    return BadRequest(new { error = "Cannot divide by zero." });
                }

                return Ok(new
                {
                    expression = request.Expression,
                    result = result,
                    _links = new
                    {
                        self = "/calculate",
                        history = "/history"
                    }
                });
            }
            catch (Exception)
            {
                return BadRequest(new { error = "Invalid expression"});
            }
        }

    }
    public class ExpressionRequest        
    {
        public string Expression { get; set; }
    }
}

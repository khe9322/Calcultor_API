using Microsoft.AspNetCore.Mvc;
using Calcultor_API.Models;

namespace Calcultor_API.Controllers
{
    [ApiController]             // API Controller ����
    [Route("[controller]")]     // Route ���� (��: /calculate)
    public class CalculateController : ControllerBase
    {
        private const string ValidToken = "secret_token_123";       //  ��ȿ�� ��ū ����

        [HttpPost]
        public IActionResult Post([FromBody] ExpressionRequest request)           // POST �޼��� ����, IActionResult�� ���� ��ȯ
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();              // Authorization ������� ��ū ����
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
                    .Replace("��", "*")
                    .Replace("��", "/");

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

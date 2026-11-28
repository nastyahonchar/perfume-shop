using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.Api.Models;
using Payments.Api.Services;
using Platform.ServiceDefaults;

namespace Payments.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class PaymentsController(PaymentStore store) : ControllerBase
{
    [RequirePermission("payments:read")]
    [HttpGet]
    public ActionResult<IReadOnlyList<PaymentDto>> GetAll()
        => Ok(store.GetAll());

    [RequirePermission("payments:process")]
    [HttpPost("process")]
    public ActionResult<PaymentDto> Process(ProcessPaymentRequest input)
        => Ok(store.Add(input.OrderId, input.Amount, input.Currency.ToUpperInvariant()));

    [RequirePermission("payments:refund")]
    [HttpPost("refund")]
    public ActionResult<PaymentDto> Refund(RefundPaymentRequest input)
    {
        var payment = store.Find(input.PaymentId);
        if (payment is null)
        {
            return NotFound(new ProblemDetails { Title = "Payment not found", Status = StatusCodes.Status404NotFound });
        }

        if (payment.Status == PaymentStatus.Refunded)
        {
            return Conflict(new ProblemDetails { Title = "Payment is already refunded", Status = StatusCodes.Status409Conflict });
        }

        return Ok(store.Refund(payment.Id));
    }
}

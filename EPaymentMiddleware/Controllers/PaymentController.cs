using EPaymentMiddleware.Dtos;
using EPaymentMiddleware.Services;
using Microsoft.AspNetCore.Mvc;

namespace EPaymentMiddleware.Controllers
{
    /// <summary>
    /// Reusable e-payment API. Any application can call these endpoints
    /// to register a payment order with SATIM and then confirm it.
    /// The OrderNumber is always provided by the calling application.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PaymentController : ControllerBase
    {
        private readonly ISatimPaymentService _satimService;

        public PaymentController(ISatimPaymentService satimService)
        {
            _satimService = satimService;
        }

        /// <summary>
        /// Register a new payment order with SATIM.
        /// Returns a formUrl the calling application should redirect the citizen to.
        /// </summary>
        /// <param name="request">Payment registration details. OrderNumber is required and must come from the caller.</param>
        [HttpPost("register")]
        public async Task<ActionResult<RegisterPaymentResponse>> Register([FromBody] RegisterPaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _satimService.RegisterAsync(request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Confirm (get the status of) a payment order from SATIM.
        /// Call this after the citizen is redirected back to your returnUrl.
        /// </summary>
        /// <param name="request">Confirmation details with the orderId returned by SATIM.</param>
        [HttpPost("confirm")]
        public async Task<ActionResult<ConfirmPaymentResponse>> Confirm([FromBody] ConfirmPaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _satimService.ConfirmAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Confirm a payment using path parameters (GET) — convenience endpoint
        /// that mirrors the original calling-application flow.
        /// </summary>
        [HttpGet("confirm/{lang}/{idsession}/{orderId}")]
        public async Task<ActionResult<ConfirmPaymentResponse>> ConfirmGet(string lang, string idsession, string orderId)
        {
            var request = new ConfirmPaymentRequest
            {
                OrderId = orderId,
                Lang = lang,
                SessionId = idsession
            };

            var result = await _satimService.ConfirmAsync(request);
            return Ok(result);
        }
    }
}

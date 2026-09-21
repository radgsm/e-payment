using EPaymentMiddleware.Data;
using EPaymentMiddleware.Dtos;
using EPaymentMiddleware.Models;
using EPaymentMiddleware.Models.Satim;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;

namespace EPaymentMiddleware.Services
{
    public interface ISatimPaymentService
    {
        Task<RegisterPaymentResponse> RegisterAsync(RegisterPaymentRequest request);
        Task<ConfirmPaymentResponse> ConfirmAsync(ConfirmPaymentRequest request);
    }

    public class SatimPaymentService : ISatimPaymentService
    {
        private readonly SatimSettings _settings;
        private readonly PaymentDbContext _db;
        private readonly HttpClient _httpClient;

        public SatimPaymentService(IOptions<SatimSettings> settings, PaymentDbContext db, IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value;
            _db = db;
            _httpClient = httpClientFactory.CreateClient("Satim");
        }

        public async Task<RegisterPaymentResponse> RegisterAsync(RegisterPaymentRequest request)
        {
            // Validate that the caller provided an order number
            if (string.IsNullOrWhiteSpace(request.OrderNumber))
            {
                return new RegisterPaymentResponse
                {
                    Success = false,
                    ErrorCode = "400",
                    Message = "OrderNumber is required (it must be provided by the calling application)."
                };
            }

            // Prevent duplicate registration for the same session
            if (await _db.PaymentOrders.AnyAsync(o => o.SessionId == request.SessionId && o.Status == 0))
            {
                return new RegisterPaymentResponse
                {
                    Success = false,
                    ErrorCode = "409",
                    Message = "A pending payment already exists for this session."
                };
            }

            int amountCentimes = request.Amount * 100;

            // Build returnUrl / failUrl — caller can override or use appsettings defaults
            string returnUrlBase = string.IsNullOrWhiteSpace(request.ReturnUrl)
                ? (_settings.returnUrl ?? "http://localhost")
                : request.ReturnUrl;
            string failUrlBase = string.IsNullOrWhiteSpace(request.FailUrl)
                ? (_settings.failUrl ?? "http://localhost")
                : request.FailUrl;

            string returnUrl = $"{returnUrlBase}?lang={request.Lang}&idsession={Uri.EscapeDataString(request.SessionId)}";
            string failUrl = $"{failUrlBase}?lang={request.Lang}&idsession={Uri.EscapeDataString(request.SessionId)}";

            // Build jsonParams with udf1..udf5
            var udf = new
            {
                force_terminal_id = _settings.force_terminal_id,
                udf1 = request.Udf1 ?? "",
                udf2 = request.Udf2 ?? "",
                udf3 = request.Udf3 ?? "",
                udf4 = request.Udf4 ?? "udf4",
                udf5 = request.Udf5 ?? "udf5"
            };
            string jsonParams = JsonConvert.SerializeObject(udf);

            string registerUrl = _settings.Register ??
                "https://satim.dz/payment/rest/register.do?";

            var urlBuilder = new StringBuilder(registerUrl);
            urlBuilder.Append("amount=").Append(amountCentimes);
            urlBuilder.Append("&language=").Append(request.Lang);
            urlBuilder.Append("&clientId=").Append(Uri.EscapeDataString(request.ClientId));
            urlBuilder.Append("&orderNumber=").Append(Uri.EscapeDataString(request.OrderNumber));
            urlBuilder.Append("&userName=").Append(Uri.EscapeDataString(_settings.SatimUser ?? ""));
            urlBuilder.Append("&password=").Append(Uri.EscapeDataString(_settings.SatimPwd ?? ""));
            urlBuilder.Append("&returnUrl=").Append(Uri.EscapeDataString(returnUrl));
            urlBuilder.Append("&failUrl=").Append(Uri.EscapeDataString(failUrl));
            urlBuilder.Append("&jsonParams=").Append(Uri.EscapeDataString(jsonParams));

            HttpResponseMessage response = await _httpClient.GetAsync(urlBuilder.ToString());
            string content = await response.Content.ReadAsStringAsync();
            var satimLink = JsonConvert.DeserializeObject<SatimLink>(content);

            if (satimLink == null)
            {
                return new RegisterPaymentResponse
                {
                    Success = false,
                    ErrorCode = "500",
                    Message = "Invalid response from SATIM."
                };
            }

            // Persist the order
            var order = new PaymentOrder
            {
                SessionId = request.SessionId,
                OrderNumber = request.OrderNumber,
                OrderId = satimLink.orderId,
                Amount = amountCentimes,
                Lang = request.Lang,
                PaymentMethod = 1,
                ClientId = request.ClientId,
                Status = 0,
                JsonParams = jsonParams,
                CreatedAt = DateTime.Now
            };
            _db.PaymentOrders.Add(order);
            await _db.SaveChangesAsync();

            return new RegisterPaymentResponse
            {
                Success = satimLink.errorCode == "0",
                ErrorCode = satimLink.errorCode,
                OrderId = satimLink.orderId,
                FormUrl = satimLink.formUrl,
                Message = satimLink.errorCode == "0"
                    ? "Payment order registered successfully."
                    : $"SATIM error code: {satimLink.errorCode}"
            };
        }

        public async Task<ConfirmPaymentResponse> ConfirmAsync(ConfirmPaymentRequest request)
        {
            string confirmUrl = _settings.ConfirmOrder ??
                "https://satim.dz/payment/rest/public/acknowledgeTransaction.do?";

            var urlBuilder = new StringBuilder(confirmUrl);
            urlBuilder.Append("language=").Append(request.Lang);
            urlBuilder.Append("&orderId=").Append(Uri.EscapeDataString(request.OrderId));
            urlBuilder.Append("&password=").Append(Uri.EscapeDataString(_settings.SatimPwd ?? ""));
            urlBuilder.Append("&userName=").Append(Uri.EscapeDataString(_settings.SatimUser ?? ""));

            HttpResponseMessage response = await _httpClient.GetAsync(urlBuilder.ToString());
            string content = await response.Content.ReadAsStringAsync();
            var satimConfirm = JsonConvert.DeserializeObject<SatimConfirm>(content);

            if (satimConfirm == null)
            {
                return new ConfirmPaymentResponse
                {
                    Success = false,
                    Message = "Invalid response from SATIM."
                };
            }

            bool paymentAccepted = satimConfirm.OrderStatus == 2
                && satimConfirm.@params?.respCode == "00";

            // Update the persisted order
            var order = await _db.PaymentOrders
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

            if (order != null)
            {
                order.OrderStatus = satimConfirm.OrderStatus;
                order.approvalCode = satimConfirm.approvalCode;
                order.respCode = satimConfirm.@params?.respCode;
                order.ErrorCode = satimConfirm.ErrorCode;
                order.actionCodeDescription = satimConfirm.actionCodeDescription;
                order.Status = paymentAccepted ? 1 : 2;
                order.ConfirmedAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            return new ConfirmPaymentResponse
            {
                Success = true,
                PaymentAccepted = paymentAccepted,
                OrderId = request.OrderId,
                OrderNumber = satimConfirm.OrderNumber,
                approvalCode = satimConfirm.approvalCode,
                respCode = satimConfirm.@params?.respCode,
                respCodeDesc = satimConfirm.@params?.respCode_desc,
                ErrorCode = satimConfirm.ErrorCode,
                ErrorMessage = satimConfirm.ErrorMessage,
                OrderStatus = satimConfirm.OrderStatus,
                Amount = satimConfirm.Amount / 100,
                actionCodeDescription = satimConfirm.actionCodeDescription,
                Message = paymentAccepted
                    ? "Votre paiement est accepté"
                    : "Votre transaction a été rejetée"
            };
        }
    }
}

using Newtonsoft.Json;

namespace QUITTANCEBE.Dtos
{
    /// <summary>
    /// Requête envoyée au middleware EPaymentMiddleware pour enregistrer un paiement.
    /// </summary>
    public class MiddlewareRegisterRequest
    {
        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;

        [JsonProperty("lang")]
        public string Lang { get; set; } = "FR";

        [JsonProperty("clientId")]
        public string ClientId { get; set; } = string.Empty;

        [JsonProperty("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        [JsonProperty("returnUrl")]
        public string? ReturnUrl { get; set; }

        [JsonProperty("failUrl")]
        public string? FailUrl { get; set; }

        [JsonProperty("udf1")]
        public string? Udf1 { get; set; }

        [JsonProperty("udf2")]
        public string? Udf2 { get; set; }

        [JsonProperty("udf3")]
        public string? Udf3 { get; set; }

        [JsonProperty("udf4")]
        public string? Udf4 { get; set; }

        [JsonProperty("udf5")]
        public string? Udf5 { get; set; }
    }

    /// <summary>
    /// Réponse du middleware après register.
    /// </summary>
    public class MiddlewareRegisterResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("errorCode")]
        public string? ErrorCode { get; set; }

        [JsonProperty("orderId")]
        public string? OrderId { get; set; }

        [JsonProperty("formUrl")]
        public string? FormUrl { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }
    }

    /// <summary>
    /// Requête envoyée au middleware pour confirmer un paiement.
    /// </summary>
    public class MiddlewareConfirmRequest
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonProperty("lang")]
        public string Lang { get; set; } = "FR";

        [JsonProperty("sessionId")]
        public string SessionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Réponse du middleware après confirm.
    /// </summary>
    public class MiddlewareConfirmResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("paymentAccepted")]
        public bool PaymentAccepted { get; set; }

        [JsonProperty("orderId")]
        public string? OrderId { get; set; }

        [JsonProperty("orderNumber")]
        public string? OrderNumber { get; set; }

        [JsonProperty("approvalCode")]
        public string? approvalCode { get; set; }

        [JsonProperty("respCode")]
        public string? respCode { get; set; }

        [JsonProperty("respCodeDesc")]
        public string? respCodeDesc { get; set; }

        [JsonProperty("errorCode")]
        public string? ErrorCode { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("orderStatus")]
        public int OrderStatus { get; set; }

        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("actionCodeDescription")]
        public string? actionCodeDescription { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }
    }
}

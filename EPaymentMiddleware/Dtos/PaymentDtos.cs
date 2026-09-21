namespace EPaymentMiddleware.Dtos
{
    /// <summary>
    /// Request sent by the calling application to register a new payment order
    /// with SATIM. The OrderNumber is provided by the caller.
    /// </summary>
    public class RegisterPaymentRequest
    {
        /// <summary>Amount in Dinars (the middleware multiplies by 100 to send centimes to SATIM).</summary>
        public int Amount { get; set; }

        /// <summary>Order number provided by the calling application (never generated here).</summary>
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>Language code: AR / FR.</summary>
        public string Lang { get; set; } = "FR";

        /// <summary>ClientId / NIN of the citizen in the calling application.</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>Unique session id from the calling application (e.g. quittance Guid).</summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Return URL base. The middleware appends ?lang=&idsession= automatically.</summary>
        public string? ReturnUrl { get; set; }

        /// <summary>Fail URL base.</summary>
        public string? FailUrl { get; set; }

        public string? Udf1 { get; set; }
        public string? Udf2 { get; set; }
        public string? Udf3 { get; set; }
        public string? Udf4 { get; set; }
        public string? Udf5 { get; set; }
    }

    /// <summary>
    /// Response returned after registering a payment order with SATIM.
    /// The calling application should redirect the citizen to FormUrl.
    /// </summary>
    public class RegisterPaymentResponse
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string? OrderId { get; set; }
        public string? FormUrl { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Request sent by the calling application to confirm (get status of) an order.
    /// </summary>
    public class ConfirmPaymentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string Lang { get; set; } = "FR";
        public string SessionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response returned after confirming a payment with SATIM.
    /// The calling application uses PaymentAccepted to decide its own business flow.
    /// </summary>
    public class ConfirmPaymentResponse
    {
        public bool Success { get; set; }
        public bool PaymentAccepted { get; set; }
        public string? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public string? approvalCode { get; set; }
        public string? respCode { get; set; }
        public string? respCodeDesc { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public int OrderStatus { get; set; }
        public int Amount { get; set; }
        public string? actionCodeDescription { get; set; }
        public string? Message { get; set; }
    }
}

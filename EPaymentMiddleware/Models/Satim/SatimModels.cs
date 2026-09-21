using Newtonsoft.Json;

namespace EPaymentMiddleware.Models.Satim
{
    public class Param
    {
        [JsonProperty("respCode")]
        public string? respCode { get; set; }

        [JsonProperty("respCode_desc")]
        public string? respCode_desc { get; set; }

        [JsonProperty("udf1")]
        public string? udf1 { get; set; }

        [JsonProperty("udf2")]
        public string? udf2 { get; set; }

        [JsonProperty("udf3")]
        public string? udf3 { get; set; }

        [JsonProperty("udf4")]
        public string? udf4 { get; set; }

        [JsonProperty("udf5")]
        public string? udf5 { get; set; }
    }

    public class PaymentAmountInfo
    {
        [JsonProperty("paymentState")]
        public string? paymentState { get; set; }

        [JsonProperty("approvedAmount")]
        public int approvedAmount { get; set; }

        [JsonProperty("depositedAmount")]
        public int depositedAmount { get; set; }

        [JsonProperty("refundedAmount")]
        public int refundedAmount { get; set; }
    }

    public class SatimConfirm
    {
        [JsonProperty("expiration")]
        public string? expiration { get; set; }

        [JsonProperty("cardholderName")]
        public string? cardholderName { get; set; }

        [JsonProperty("depositAmount")]
        public int depositAmount { get; set; }

        [JsonProperty("currency")]
        public string? currency { get; set; }

        [JsonProperty("approvalCode")]
        public string? approvalCode { get; set; }

        [JsonProperty("authCode")]
        public int authCode { get; set; }

        [JsonProperty("params")]
        public Param? @params { get; set; }

        [JsonProperty("actionCode")]
        public int actionCode { get; set; }

        [JsonProperty("actionCodeDescription")]
        public string? actionCodeDescription { get; set; }

        [JsonProperty("ErrorCode")]
        public string? ErrorCode { get; set; }

        [JsonProperty("ErrorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("OrderStatus")]
        public int OrderStatus { get; set; }

        [JsonProperty("OrderNumber")]
        public string? OrderNumber { get; set; }

        [JsonProperty("Pan")]
        public string? Pan { get; set; }

        [JsonProperty("Amount")]
        public int Amount { get; set; }

        [JsonProperty("Ip")]
        public string? Ip { get; set; }

        [JsonProperty("SvfeResponse")]
        public string? SvfeResponse { get; set; }
    }

    public class SatimLink
    {
        [JsonProperty("errorCode")]
        public string? errorCode { get; set; }

        [JsonProperty("orderId")]
        public string? orderId { get; set; }

        [JsonProperty("formUrl")]
        public string? formUrl { get; set; }
    }

    public class SatimSettings
    {
        public string? SatimUser { get; set; }
        public string? SatimPwd { get; set; }
        public string? force_terminal_id { get; set; }
        public string? Register { get; set; }
        public string? ConfirmOrder { get; set; }
        public string? returnUrl { get; set; }
        public string? failUrl { get; set; }
    }
}

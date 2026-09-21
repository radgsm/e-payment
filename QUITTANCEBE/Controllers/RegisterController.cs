using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QUITTANCEBE.Data;
using QUITTANCEBE.Dtos;
using QUITTANCEBE.Models;
using System.Net;
using System.Text;
using System.Threading;

namespace QUITTANCEBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly CitoyenDbContext _context;
        private static SemaphoreSlim oSemaphoreSlim = new SemaphoreSlim(1);
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public RegisterController(CitoyenDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        private string GetNewOrderNumber()
        {
            var result = _context.Set<MyClass>().FromSqlRaw("EXEC sp_GetNewOrderNumber").ToList();
            return result[0].ReturnedValue.ToString();
        }

        [HttpGet("Payer/{P}/{NIN}/{lang}")]
        public async Task<ActionResult> Purchase(int P, string NIN, string lang)
        {
            var quitData = _context.Quittances.Where(Q => Q.NIN == NIN).OrderByDescending(Q => Q.DateAchat).Select(Q => new { Q.Id, Q.Prix, Q.CodeQ }).FirstOrDefault();
            Guid Id = (quitData.Id);
            if (_context.TabOrderNumbers.Any(ton => ton.id == Id.ToString().ToLower()))
            {
                return BadRequest("Id en cours d'utilisation");
            }
            int amount = quitData.Prix * 100;
            string codeQ = quitData.CodeQ;
            TabOrderNumber oTabOrderNumber = new TabOrderNumber();

            string OrderNumber = GetNewOrderNumber();
            oTabOrderNumber.OrderNumber = OrderNumber;

            if (P == 1) // Payer via SATIM (via le middleware)
            {
                string middlewareUrl = _config.GetValue<string>("MiddlewareSettings:BaseUrl");
                string RIT = _config.GetValue<string>("AppSettings:RIT");
                string returnUrl = _config.GetValue<string>("SatimSettings:returnUrl") + "?lang=" + lang + "&idsession=" + Id.ToString().ToLower();
                string failUrl = _config.GetValue<string>("SatimSettings:failUrl") + "?lang=" + lang + "&idsession=" + Id.ToString().ToLower();

                var registerReq = new MiddlewareRegisterRequest
                {
                    Amount = quitData.Prix,
                    OrderNumber = OrderNumber,
                    Lang = lang,
                    ClientId = NIN,
                    SessionId = Id.ToString().ToLower(),
                    ReturnUrl = returnUrl,
                    FailUrl = failUrl,
                    Udf1 = RIT,
                    Udf2 = codeQ,
                    Udf3 = NIN,
                    Udf4 = "udf4",
                    Udf5 = "udf5"
                };

                var json = JsonConvert.SerializeObject(registerReq);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var http = _httpClientFactory.CreateClient("Middleware");
                HttpResponseMessage middlewareResponse = await http.PostAsync($"{middlewareUrl}/api/Payment/register", content);
                string stringContent = await middlewareResponse.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<MiddlewareRegisterResponse>(stringContent);

                if (result == null || !result.Success)
                {
                    return BadRequest(stringContent);
                }

                oTabOrderNumber.lang = lang;
                oTabOrderNumber.id = Id.ToString().ToLower();
                oTabOrderNumber.orderId = result.OrderId;

                _context.ChangeTracker.Clear();
                _context.Update(oTabOrderNumber);
                _context.SaveChanges();

                // On renvoie la même structure que SATIM pour que le frontend ne change pas
                var satimLikeResponse = new
                {
                    errorCode = result.ErrorCode,
                    orderId = result.OrderId,
                    formUrl = result.FormUrl
                };
                return Ok(JsonConvert.SerializeObject(satimLikeResponse));
            }
            else  // Payer via Algerie Poste (reste direct, inchangé)
            {
                string PosteUser = _config.GetValue<string>("PosteSettings:PosteUser");
                string PostePwd = _config.GetValue<string>("PosteSettings:PostePwd");
                string Register = _config.GetValue<string>("PosteSettings:Register");
                string RIT = _config.GetValue<string>("AppSettings:RIT");
                string returnUrl = _config.GetValue<string>("PosteSettings:returnUrl") + "?lang=" + lang + "&idsession=" + Id.ToString().ToLower();
                string MyDomain = _config.GetValue<string>("AppSettings:MyDomain");

                var jsonParams = "{\"udf1\":\"" + RIT + "\",\"udf2\":\"" + codeQ + "\",\"udf3\": \"" + NIN + "\",\"udf4\":\"udf4\",\"udf5\":\"udf5\"}";
                var urlPayer = Register + amount + "&language=" + lang + "&clientId=" + NIN + "&orderNumber=" + OrderNumber + "&userName=" + PosteUser + "&password=" + PostePwd + "&returnUrl=" + Uri.EscapeDataString(returnUrl) + "&jsonParams=" + jsonParams;

                HttpResponseMessage PosteResponse = null;
                string stringContent = "";

                using (var http = new HttpClient())
                {
                    PosteResponse = await http.GetAsync(urlPayer);
                    stringContent = await PosteResponse.Content.ReadAsStringAsync();
                }

                var result = JsonConvert.DeserializeObject<PosteLink>(stringContent);

                oTabOrderNumber.lang = lang;
                oTabOrderNumber.id = Id.ToString().ToLower();
                oTabOrderNumber.orderId = result.orderId;

                _context.ChangeTracker.Clear();
                _context.Update(oTabOrderNumber);
                _context.SaveChanges();
                return Ok(stringContent);
            }
        }
    }
}

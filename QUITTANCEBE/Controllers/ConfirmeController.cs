using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QUITTANCEBE.Data;
using QUITTANCEBE.Dtos;
using QUITTANCEBE.Models;
using System.Dynamic;
using System.Text;

namespace QUITTANCEBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfirmeController : ControllerBase
    {
        private readonly CitoyenDbContext _context;
        private static SemaphoreSlim oSemaphoreSlimQ = new SemaphoreSlim(1);
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public ConfirmeController(CitoyenDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        private string GenerateNewNumQuit()
        {
            var result = _context.Set<MyClass>().FromSqlRaw("EXEC sp_GetNewNumQuittance").ToList();
            return result[0].ReturnedValue.ToString();
        }

        [HttpGet("{P}/{lang}/{idsession}/{orderId}")]
        public async Task<ActionResult> Confirmer(int P, string lang, string idsession, string orderId)
        {
            Guid IdG = new Guid(idsession);
            Quittance oQuittance = new Quittance();
            DateTime DateNow = DateTime.Now;

            var quitData = _context.Quittances
                                        .Where(q => q.Id == IdG)
                                        .Join(_context.Citoyens,
                                             quittance => quittance.Id,
                                             citoyen => citoyen.Id,
                                             (quittance, citoyen) => new
                                             {
                                                 quittance.Prix,
                                                 quittance.lang,
                                                 quittance.CodeQ,
                                                 citoyen.NIN,
                                                 citoyen.Nom,
                                                 citoyen.Prenom,
                                                 citoyen.Np,
                                                 citoyen.Rs,
                                                 citoyen.Num_imm
                                             })
                                        .Join(_context.DetailQs,
                                           combined => combined.CodeQ,
                                           detailQ => detailQ.CodeQ,
                                           (combined, detailQ) => new
                                           {
                                               combined.Prix,
                                               combined.CodeQ,
                                               combined.NIN,
                                               combined.Nom,
                                               combined.Prenom,
                                               combined.lang,
                                               detailQ.LibelleQFr,
                                               detailQ.LibelleQAr,
                                               combined.Np,
                                               combined.Rs,
                                               combined.Num_imm
                                           }).FirstOrDefault();

            string codeQ = quitData.CodeQ;
            string NIN0 = quitData.NIN;
            string nom = quitData.Nom;
            string prenom = quitData.Prenom;
            string libelleqfr = quitData.LibelleQFr;
            string libelleqar = quitData.LibelleQAr;
            string Np = quitData.Np;
            string Rs = quitData.Rs;
            string Num_imm = quitData.Num_imm;

            var Tr = _context.Transactions.Where(T => T.OrderId == orderId).FirstOrDefault();

            if (Tr != null)
            {
                dynamic oTransQuit = new ExpandoObject();
                oTransQuit.OrderId = orderId;
                oTransQuit.OrderNumber = Tr.OrderNumber;
                oTransQuit.approvalCode = Tr.approvalCode;
                oTransQuit.NIN = NIN0;
                oTransQuit.NumQuit = Tr.NumQuit;
                oTransQuit.TransactionDate = Tr.TransactionDate;
                oTransQuit.respCode = Tr.respCode;
                oTransQuit.ErrorCode = Tr.ErrorCode;
                oTransQuit.OrderStatus = Tr.OrderStatus;
                oTransQuit.actionCodeDescription = "Votre paiement est accepté";
                oTransQuit.Prix = quitData.Prix;
                oTransQuit.lang = quitData.lang;
                oTransQuit.CodeQ = codeQ;
                oTransQuit.Nom = nom;
                oTransQuit.Prenom = prenom;
                oTransQuit.LibelleQFr = libelleqfr;
                oTransQuit.LibelleQAr = libelleqar;
                oTransQuit.Np = Np;
                oTransQuit.Rs = Rs;
                oTransQuit.Num_imm = Num_imm;

                return Ok(oTransQuit);
            }

            if (P == 1) // Confirmer via le middleware de paiement
            {
                string middlewareUrl = _config.GetValue<string>("MiddlewareSettings:BaseUrl");

                var confirmReq = new MiddlewareConfirmRequest
                {
                    OrderId = orderId,
                    Lang = lang,
                    SessionId = idsession
                };

                var json = JsonConvert.SerializeObject(confirmReq);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var http = _httpClientFactory.CreateClient("Middleware");
                var response = await http.PostAsync($"{middlewareUrl}/api/Payment/confirm", content);
                string responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<MiddlewareConfirmResponse>(responseContent);

                if (result == null || !result.Success)
                {
                    dynamic oTransQuitErr = new ExpandoObject();
                    oTransQuitErr.OrderId = orderId;
                    oTransQuitErr.OrderNumber = "";
                    oTransQuitErr.approvalCode = "";
                    oTransQuitErr.NIN = NIN0;
                    oTransQuitErr.NumQuit = "";
                    oTransQuitErr.TransactionDate = DateNow;
                    oTransQuitErr.respCode = "";
                    oTransQuitErr.ErrorCode = result?.ErrorCode;
                    oTransQuitErr.OrderStatus = result?.OrderStatus ?? 0;
                    oTransQuitErr.actionCodeDescription = "Erreur lors de la confirmation du paiement";
                    oTransQuitErr.Prix = quitData.Prix;
                    oTransQuitErr.lang = quitData.lang;
                    oTransQuitErr.CodeQ = codeQ;
                    oTransQuitErr.Nom = nom;
                    oTransQuitErr.Prenom = prenom;
                    oTransQuitErr.LibelleQFr = libelleqfr;
                    oTransQuitErr.LibelleQAr = libelleqar;
                    oTransQuitErr.Np = Np;
                    oTransQuitErr.Rs = Rs;
                    oTransQuitErr.Num_imm = Num_imm;
                    return Ok(oTransQuitErr);
                }

                if (result.PaymentAccepted)
                {
                    string NumQuit = GenerateNewNumQuit();
                    Transaction oTransaction = new Transaction
                    {
                        Id = IdG,
                        OrderId = orderId,
                        OrderNumber = result.OrderNumber,
                        approvalCode = result.approvalCode,
                        NumQuit = NumQuit,
                        amount = result.Amount,
                        TransactionDate = DateNow,
                        OrderStatus = result.OrderStatus,
                        respCode = result.respCode,
                        ErrorCode = result.ErrorCode,
                        etat = true,
                    };

                    _context.Transactions.Add(oTransaction);
                    _context.SaveChanges();

                    oQuittance = _context.Quittances.FirstOrDefault(V => V.Id == IdG);
                    if (oQuittance != null)
                    {
                        oQuittance.NumQuit = NumQuit;
                        oQuittance.DateAchat = DateNow;
                        oQuittance.lang = lang;
                        _context.SaveChanges();
                    }

                    dynamic oTransQuit = new ExpandoObject();
                    oTransQuit.OrderId = orderId;
                    oTransQuit.OrderNumber = result.OrderNumber;
                    oTransQuit.approvalCode = result.approvalCode;
                    oTransQuit.NIN = NIN0;
                    oTransQuit.NumQuit = NumQuit;
                    oTransQuit.TransactionDate = DateNow;
                    oTransQuit.respCode = result.respCode;
                    oTransQuit.ErrorCode = result.ErrorCode;
                    oTransQuit.OrderStatus = result.OrderStatus;
                    oTransQuit.actionCodeDescription = result.actionCodeDescription;
                    oTransQuit.Prix = quitData.Prix;
                    oTransQuit.lang = quitData.lang;
                    oTransQuit.CodeQ = codeQ;
                    oTransQuit.Nom = nom;
                    oTransQuit.Prenom = prenom;
                    oTransQuit.LibelleQFr = libelleqfr;
                    oTransQuit.LibelleQAr = libelleqar;
                    oTransQuit.Np = Np;
                    oTransQuit.Rs = Rs;
                    oTransQuit.Num_imm = Num_imm;

                    return Ok(oTransQuit);
                }
                else
                {
                    dynamic oTransQuit = new ExpandoObject();
                    oTransQuit.OrderId = orderId;
                    oTransQuit.OrderNumber = result.OrderNumber;
                    oTransQuit.approvalCode = result.approvalCode;
                    oTransQuit.NIN = NIN0;
                    oTransQuit.NumQuit = "";
                    oTransQuit.TransactionDate = DateNow;
                    oTransQuit.respCode = result.respCode;
                    oTransQuit.ErrorCode = result.ErrorCode;
                    oTransQuit.OrderStatus = result.OrderStatus;
                    oTransQuit.actionCodeDescription = "Votre transaction a été rejetée";
                    oTransQuit.Prix = quitData.Prix;
                    oTransQuit.lang = quitData.lang;
                    oTransQuit.CodeQ = codeQ;
                    oTransQuit.Nom = nom;
                    oTransQuit.Prenom = prenom;
                    oTransQuit.LibelleQFr = libelleqfr;
                    oTransQuit.LibelleQAr = libelleqar;
                    oTransQuit.Np = Np;
                    oTransQuit.Rs = Rs;
                    oTransQuit.Num_imm = Num_imm;
                    return Ok(oTransQuit);
                }
            }
            else // Confirmer via AP (reste direct, inchangé)
            {
                string PosteUser = _config.GetValue<string>("PosteSettings:PosteUser");
                string PostePwd = _config.GetValue<string>("PosteSettings:PostePwd");
                string getOrderStatusExtended = _config.GetValue<string>("PosteSettings:getOrderStatusExtended") + lang + "&orderId=";
                string urlConfim = getOrderStatusExtended + orderId + "&password=" + PostePwd + "&userName=" + PosteUser;
                var client = new HttpClient();
                var response = await client.GetAsync(urlConfim);
                string content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<PosteConfirm>(content);
                string NIN = result.merchantOrderParams.Find(p => p.name == "udf3")?.value;

                if ((result.orderStatus == 2) && (result.errorCode == "0"))
                {
                    string NumQuit = GenerateNewNumQuit();

                    Transaction oTransaction = new Transaction
                    {
                        Id = IdG,
                        OrderId = orderId,
                        OrderNumber = result.orderNumber,
                        approvalCode = result.cardAuthInfo.approvalCode,
                        NumQuit = NumQuit,
                        amount = result.amount / 100,
                        TransactionDate = DateNow,
                        OrderStatus = result.orderStatus,
                        respCode = result.actionCode,
                        ErrorCode = result.errorCode,
                        etat = true,
                    };

                    _context.Transactions.Add(oTransaction);
                    _context.SaveChanges();

                    oQuittance = _context.Quittances.FirstOrDefault(Q => Q.Id == IdG);
                    if (oQuittance != null)
                    {
                        oQuittance.NumQuit = NumQuit;
                        oQuittance.DateAchat = DateNow;
                        oQuittance.lang = lang;
                        _context.SaveChanges();
                    }

                    dynamic oTransQuit = new ExpandoObject();
                    oTransQuit.OrderId = orderId;
                    oTransQuit.OrderNumber = result.orderNumber;
                    oTransQuit.approvalCode = result.cardAuthInfo.approvalCode;
                    oTransQuit.NIN = NIN;
                    oTransQuit.NumQuit = NumQuit;
                    oTransQuit.TransactionDate = DateNow;
                    oTransQuit.respCode = result.actionCode;
                    oTransQuit.ErrorCode = result.errorCode;
                    oTransQuit.OrderStatus = result.orderStatus;
                    oTransQuit.actionCodeDescription = result.actionCodeDescription;
                    oTransQuit.Prix = quitData.Prix;
                    oTransQuit.lang = quitData.lang;
                    oTransQuit.CodeQ = codeQ;
                    oTransQuit.Nom = nom;
                    oTransQuit.Prenom = prenom;
                    oTransQuit.LibelleQFr = libelleqfr;
                    oTransQuit.LibelleQAr = libelleqar;
                    oTransQuit.Np = Np;
                    oTransQuit.Rs = Rs;
                    oTransQuit.Num_imm = Num_imm;
                    return Ok(oTransQuit);
                }
                else
                {
                    dynamic oTransQuit = new ExpandoObject();
                    oTransQuit.OrderId = orderId;
                    oTransQuit.OrderNumber = result.orderNumber;
                    oTransQuit.approvalCode = "";
                    oTransQuit.NIN = NIN;
                    oTransQuit.NumQuit = "";
                    oTransQuit.TransactionDate = "";
                    oTransQuit.respCode = result.actionCode;
                    oTransQuit.ErrorCode = result.errorCode;
                    oTransQuit.OrderStatus = result.orderStatus;
                    oTransQuit.actionCodeDescription = result.actionCodeDescription;
                    oTransQuit.Prix = 0;
                    oTransQuit.lang = "";
                    return Ok(oTransQuit);
                }
            }
        }

        [HttpGet("ConfirmeEchec/{lang}/{idsession}/{orderId}")]
        public async Task<ActionResult> ConfirmerEchec(string lang, string idsession, string orderId)
        {
            string middlewareUrl = _config.GetValue<string>("MiddlewareSettings:BaseUrl");

            var confirmReq = new MiddlewareConfirmRequest
            {
                OrderId = orderId,
                Lang = lang,
                SessionId = idsession
            };

            var json = JsonConvert.SerializeObject(confirmReq);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var http = _httpClientFactory.CreateClient("Middleware");
            var response = await http.PostAsync($"{middlewareUrl}/api/Payment/confirm", content);
            string responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<MiddlewareConfirmResponse>(responseContent);

            dynamic oTransQuit = new ExpandoObject();
            oTransQuit.OrderId = "";
            oTransQuit.OrderNumber = result?.OrderNumber ?? "";
            oTransQuit.approvalCode = "";
            oTransQuit.NumQuit = "";
            oTransQuit.TransactionDate = "";
            oTransQuit.respCode = result?.respCodeDesc;
            oTransQuit.ErrorCode = result?.ErrorCode;
            oTransQuit.OrderStatus = result?.OrderStatus ?? 0;
            oTransQuit.actionCodeDescription = result?.actionCodeDescription;
            oTransQuit.Prix = 0;
            oTransQuit.lang = "";
            oTransQuit.CodeQ = "";

            return Ok(oTransQuit);
        }
    }
}

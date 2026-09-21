# EPaymentMiddleware

Middleware de e-payment réutilisable en ASP.NET Core 6 qui communique avec l'opérateur SATIM.
Ce middleware peut être utilisé par **n'importe quelle application** (web, mobile, desktop) qui souhaite accepter des paiements SATIM.

## Architecture

```
[Application appelante]  --HTTP/JSON-->  [EPaymentMiddleware]  --HTTPS-->  [SATIM]
        (quittances, etc.)                    (API REST)                   (opérateur)
```

L'application appelante fournit l'`OrderNumber` (jamais généré par le middleware).
Le middleware se charge de :
1. Enregistrer la commande auprès de SATIM (register) → renvoie une `formUrl`
2. Confirmer le statut du paiement auprès de SATIM (confirm) → renvoie le résultat

## Configuration

### 1. Base de données SQL Server

Exécutez le script `Scripts/CreateDatabase.sql` dans SQL Server Management Studio.

Puis vérifiez la chaîne de connexion dans `appsettings.json` :
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=EPaymentMiddlewareDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 2. Paramètres SATIM

Renseignez vos identifiants SATIM dans `appsettings.json` :
```json
"SatimSettings": {
  "SatimUser": "votre_user",
  "SatimPwd": "votre_password",
  "force_terminal_id": "votre_terminal_id",
  "Register": "https://satim.dz/payment/rest/register.do?",
  "ConfirmOrder": "https://satim.dz/payment/rest/public/acknowledgeTransaction.do?",
  "returnUrl": "https://votre-application.fr/return",
  "failUrl": "https://votre-application.fr/fail"
}
```

## API Endpoints

### POST /api/Payment/register

Enregistre une commande de paiement auprès de SATIM.

**Corps de la requête :**
```json
{
  "amount": 100,
  "orderNumber": "ORD202600001",
  "lang": "FR",
  "clientId": "123456789",
  "sessionId": "guid-ou-id-unique",
  "returnUrl": "https://mon-app.fr/return",
  "failUrl": "https://mon-app.fr/fail",
  "udf1": "RIT",
  "udf2": "CodeQ",
  "udf3": "NIN",
  "udf4": "",
  "udf5": ""
}
```

> `amount` est en Dinars (le middleware multiplie par 100 pour envoyer des centimes à SATIM).
> `orderNumber` est **obligatoire** et doit être fourni par l'application appelante.
> `returnUrl` et `failUrl` sont optionnels (si non fournis, les valeurs de `appsettings.json` sont utilisées).

**Réponse :**
```json
{
  "success": true,
  "errorCode": "0",
  "orderId": "abc123...",
  "formUrl": "https://satim.dz/payment/merchants/sberbank/payment/...",
  "message": "Payment order registered successfully."
}
```

L'application appelante doit rediriger le citoyen vers `formUrl`.

---

### POST /api/Payment/confirm

Confirme le statut d'un paiement après le retour du citoyen.

**Corps de la requête :**
```json
{
  "orderId": "abc123...",
  "lang": "FR",
  "sessionId": "guid-ou-id-unique"
}
```

**Réponse :**
```json
{
  "success": true,
  "paymentAccepted": true,
  "orderId": "abc123...",
  "orderNumber": "ORD202600001",
  "approvalCode": "123456",
  "respCode": "00",
  "respCodeDesc": "",
  "errorCode": "0",
  "errorMessage": "",
  "orderStatus": 2,
  "amount": 100,
  "actionCodeDescription": "",
  "message": "Votre paiement est accepté"
}
```

> `paymentAccepted` est `true` quand `OrderStatus == 2` et `respCode == "00"`.

---

### GET /api/Payment/confirm/{lang}/{idsession}/{orderId}

Variante GET pour confirmer un paiement (utile pour les redirections).

## Exemple d'intégration côté application appelante

```csharp
// Dans votre application (ex: QUITTANCEBE)

// 1. Enregistrer le paiement via le middleware
var registerReq = new
{
    amount = quitData.Prix,
    orderNumber = orderNumber,
    lang = lang,
    clientId = NIN,
    sessionId = Id.ToString(),
    udf1 = RIT,
    udf2 = codeQ,
    udf3 = NIN
};

var json = JsonConvert.SerializeObject(registerReq);
var content = new StringContent(json, Encoding.UTF8, "application/json");

using var http = new HttpClient();
var middlewareUrl = "http://localhost:5001/api/Payment/register"; // URL du middleware
var response = await http.PostAsync(middlewareUrl, content);
var result = JsonConvert.DeserializeObject<RegisterPaymentResponse>(
    await response.Content.ReadAsStringAsync());

// result.FormUrl → rediriger le citoyen vers cette URL

// 2. Après le retour du citoyen sur returnUrl, confirmer le paiement
var confirmReq = new
{
    orderId = orderId,
    lang = lang,
    sessionId = Id.ToString()
};
var confirmJson = JsonConvert.SerializeObject(confirmReq);
var confirmContent = new StringContent(confirmJson, Encoding.UTF8, "application/json");
var confirmResponse = await http.PostAsync(
    "http://localhost:5001/api/Payment/confirm", confirmContent);
var confirmResult = JsonConvert.DeserializeObject<ConfirmPaymentResponse>(
    await confirmResponse.Content.ReadAsStringAsync());

if (confirmResult.PaymentAccepted)
{
    // Paiement accepté → générer la quittance, etc.
}
else
{
    // Paiement rejeté → afficher le message d'erreur
}
```

## Démarrage

```bash
cd EPaymentMiddleware
dotnet restore
dotnet run
```

En mode développement, Swagger est disponible sur `https://localhost:5001/swagger`.

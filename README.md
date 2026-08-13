# Ecom — E-Commerce REST API (.NET 8)

A full-featured **ASP.NET Core 8** e-commerce backend built with a clean layered architecture
(`Ecom.API`, `Ecom.infrastructure`, `Ecom.Core`). It powers the complete server side of an online
store: product catalog, customer basket, authentication & email workflows, order placement, Stripe
payments, and product ratings.

Built as a hands-on backend project focused on getting the fundamentals right — proper layering,
EF Core relationships, the Repository / Unit of Work pattern, and integrating a real third-party
payment provider end to end — rather than just shipping features.

---

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Authentication & Authorization](#authentication--authorization)
- [Database](#database)
- [Redis / Basket](#redis--basket)
- [Payments (Stripe)](#payments-stripe)
- [API Endpoints](#api-endpoints)
- [Example Requests](#example-requests)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Configuration & Secrets](#configuration--secrets)

---

## Features

**Catalog**
- Product listing with **pagination**, **multi-word search** (across name & description), **category
  filtering**, and **sorting** (price ascending / descending / name).
- Product detail with category and photo gallery.
- Full CRUD for products and categories, including **multi-image upload** (files persisted under
  `wwwroot/Images/<ProductName>/`), with old photos cleaned up from disk and DB on update/delete.

**Customer Basket (Redis)**
- Redis-backed shopping basket with a 3-day TTL, decoupled from user accounts (works for guests too).
- On every basket update, each item is validated against SQL Server and its `Name`, `Price`,
  `Description`, `CategoryName`, and `Image` are **overwritten with fresh database values** — the
  client can never dictate price or name.
- Carries the Stripe `PaymentIntentId` / `ClientSecret` once checkout starts.

**Authentication & Authorization**
- ASP.NET Core Identity with **JWT delivered inside an HttpOnly cookie**.
- Registration with **email confirmation**, account activation, password reset via email.
- Login with **account lockout** on repeated failed attempts.
- `[Authorize]`-protected order and payment endpoints.

**Orders**
- Converts a basket into an order snapshot: order items, subtotal, delivery method, and a
  shipping-address snapshot are all frozen at purchase time, so later catalog or address changes
  never rewrite order history.
- Re-submitting an order for a basket that already produced one (same Stripe `PaymentIntentId`)
  replaces the previous order instead of creating a duplicate.

**Payments (Stripe)**
- Creates/updates a Stripe **PaymentIntent** per basket (amount = basket items + delivery price).
- A signature-verified **webhook** marks the matching order `PaymentReceived` on
  `payment_intent.succeeded` or `PaymentFaild` on `payment_intent.payment_failed`.

**Ratings & Reviews**
- Users can rate products (1–5 stars) with an optional comment.
- A product's aggregate rating is recalculated after every new rating, rounded to the nearest 0.5,
  and persisted on the product itself.

**Cross-cutting concerns**
- One middleware handles **global exception handling**, per-IP **rate limiting**
  (80 requests / 30 seconds), and standard **security headers**
  (`X-Content-Type-Options`, `X-XSS-Protection`, `X-Frame-Options`).
- Swagger / OpenAPI UI in the `Development` environment.

---

## Architecture

Three projects with a strict, one-way dependency direction:

```
Ecom.API  ──►  Ecom.infrastructure  ──►  Ecom.Core
```

| Project | Responsibility |
|---|---|
| **`Ecom.Core`** | Domain layer. Entities, DTOs, repository/service interfaces, shared result & param types. No dependency on EF Core or infrastructure. |
| **`Ecom.infrastructure`** | Data access & infrastructure. `AppDbContext`, EF Core Fluent configurations, migrations, repository implementations, Redis, and Stripe/Email/JWT/Image services. |
| **`Ecom.API`** | Presentation layer. Controllers, `Program.cs` composition root, middleware, and AutoMapper profiles. |

**Patterns in use**
- **Generic Repository** (`IGenericRepository<T>`) plus concrete repositories for
  aggregate-specific queries (`ProductRepository`, `CategoryRepository`, `CustomerBasketRepository`,
  `RatingReposiroty`).
- **Unit of Work** (`IUnitOfWork`) composing repositories over a shared `AppDbContext` and exposing
  `SaveChangesAsync()` — it also exposes the `Auth` repository.
- **Service classes** for cross-cutting concerns: `EmailService` (SMTP), `GenerateToken` (JWT),
  `ImageManagementService` (file I/O), `OrderService` (order orchestration), `PaymentService` (Stripe).
- **AutoMapper** profiles for entity ⇄ DTO mapping (`Ecom.API/Mapping`).
- Centralized **DI registration** in `infrastructureRegisteration.infrastructureConfiguration(...)`,
  called once from `Program.cs`.

**Typical request flow**

```
Controller → IUnitOfWork / Service → Repository → AppDbContext (SQL Server) or Redis
```

---

## Tech Stack

| Technology | Version | Purpose |
|---|---|---|
| ASP.NET Core Web API | .NET 8 | Application framework |
| Entity Framework Core | 8.0.7 | ORM + migrations |
| SQL Server | — | Persistent relational storage |
| ASP.NET Core Identity | 8.0.7 | Users, password hashing, email confirmation, lockout |
| JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) | 8.0.7 | Token authentication |
| StackExchange.Redis | 2.8.16 | Shopping-basket cache |
| AutoMapper | 13.0.1 | Object mapping |
| MailKit / MimeKit | 4.17.0 | SMTP email sending |
| Stripe.net | 52.3.0 | PaymentIntents & webhooks |
| Swashbuckle.AspNetCore | 6.6.2 | Swagger / OpenAPI (development only) |

---

## Authentication & Authorization

**Identity setup**
- `AddIdentity<AppUser, IdentityRole>()` with `AddEntityFrameworkStores<AppDbContext>()` and default
  token providers.
- `AppUser : IdentityUser` adds `DisplayName` and a one-to-one `Address`.
- Password policy: minimum length 8, requires a digit and a lowercase letter (uppercase and special
  characters not required).
- **Email confirmation is enforced at login** — unconfirmed users are blocked and automatically
  re-sent an activation link.
- **Account lockout** is enabled on repeated failed sign-in attempts
  (`CheckPasswordSignInAsync(..., lockoutOnFailure: true)`).

**JWT**
- Generated with claims for `NameIdentifier` (user id), `Email`, and `Name` (username).
- Signed with `HmacSha256` using `Token:Secret`; issuer is `Token:Issuer`; expires in **60 minutes**;
  audience validation is disabled.
- Delivered as an **HttpOnly cookie named `token`** (`Secure`, `SameSite=None`) on login.
- The JWT bearer handler's `OnMessageReceived` reads the token from that cookie, so an authenticated
  browser session "just works" without manually attaching an `Authorization` header.
- `[Authorize]` protects the Order and Payments controllers, and the account "get-user-name" endpoint.

**Flows**
- **Register** → uniqueness checks on username/email → create user → email a confirmation token →
  confirm via `Active-Account`.
- **Login** → look up by email → require confirmed email → check password (with lockout) → issue JWT
  cookie.
- **Forgot / Reset password** → email a password-reset token → reset via `Reset-password`.
- **Logout** → deletes the `token` cookie.

Activation and password-reset emails link out to an external frontend
(`http://localhost:4200/Account/...`) with a URL-encoded token, which is expected to call the
corresponding API endpoints.

---

## Database

SQL Server via **EF Core 8**, code-first with migrations. `AppDbContext : IdentityDbContext<AppUser>`
exposes:

| DbSet | Entity |
|---|---|
| `Products` | `Product` |
| `Categories` | `Category` |
| `Photos` | `Photo` |
| `Addresses` | `Address` |
| `Orders` | `Order` |
| `OrderItems` | `OrderItem` |
| `DeliveryMethods` | `DeliveryMethod` |
| `Ratings` | `Rating` |

The standard Identity tables (`AspNetUsers`, `AspNetRoles`, etc.) are added automatically by
`IdentityDbContext`.

**Key relationships**

| Relationship | Foreign key | Delete behavior |
|---|---|---|
| `Category` 1 — N `Product` | `Product.CategoryId` | Default (Cascade) |
| `Product` 1 — N `Photo` | `Photo.ProductId` | Default (Cascade) |
| `AppUser` 1 — 1 `Address` | `Address.AppUserId` (unique index) | Cascade |
| `AppUser` N — 1 `Rating` | `Rating.AppUserId` | Default (Cascade) |
| `Product` N — 1 `Rating` | `Rating.ProductId` | Default (Cascade) |
| `Order` N — 1 `DeliveryMethod` | `Order.DeliveryMethodId` | **Restrict** (a delivery method used by an order can't be deleted) |
| `Order` 1 — N `OrderItem` | `OrderItem.OrderId` | **Cascade** |
| `Order` 1 — 1 `ShippingAddress` | Owned entity, stored as columns on `Orders` | — |

**Notes**
- `ShippingAddress` is an **owned entity** (`OwnsOne`) — a frozen snapshot of the address at order
  time, stored directly in the `Orders` table rather than a separate table.
- `Order.Status` (`enum Status { Pending, PaymentReceived, PaymentFaild }`) is persisted as a
  **string** via an EF Core value converter.
- Money columns (`Product.NewPrice`/`OldPrice`, `Order.SubTotal`, `OrderItem.Price`,
  `DeliveryMethod.Price`) are all `decimal(18,2)`.
- Indexes: unique `DeliveryMethods.Name`, `Orders.BuyerEmail`, composite `Orders(BuyerEmail,
  OrderDate)`, unique `Addresses.AppUserId`.
- Seed data (via Fluent API `HasData`, applied through migrations): a placeholder test `Category` and
  `Product`, plus two delivery methods (`DHL`, `XXX`).

**Migrations**

```
init                          Categories, Products, Photos tables
SeedDate                      Placeholder seed rows
SomeUpdates                   Category.Description made nullable
OldPriceAdded                 Price column split into OldPrice + NewPrice
ProductUpdate                 (empty / no-op migration)
Add_Identity_Tables           Identity tables + Addresses
AddOrderEntity                DeliveryMethods, Orders, OrderItems
DeliverySeed                  Seed delivery methods (DHL, XXX)
RemoveShippingAddressId       ShippingAddress ownership clean-up
AddingPaymentInit             Order.PaymentIntentId added
AddRating                     Ratings table
```

---

## Redis / Basket

- `StackExchange.Redis` is registered as a singleton `IConnectionMultiplexer`.
- `CustomerBasket` / `BasketItem` are plain POCOs, serialized to/from Redis as JSON.
- **Key** = client-supplied basket id; **TTL** = 3 days from the last update.
- `UpdateBasketAsync` re-validates every item against SQL Server on save:
  1. Loads the referenced products (with photos & category).
  2. If any requested product no longer exists, the update is rejected.
  3. Each item's `Name`, `Price` (from `Product.NewPrice`), `Description`, `CategoryName`, and `Image`
     (first photo) are overwritten from the database — never trusted from the client.
  4. The refreshed basket is written back to Redis with a renewed 3-day TTL.
- The basket also carries `PaymentIntentId` / `ClientSecret` once a Stripe PaymentIntent has been
  created for it.

---

## Payments (Stripe)

- **`POST /api/Payments?basketId=...&deliveryMethodId=...`** *(authorized)* — computes the amount from
  the basket's line items plus the selected delivery method's price, then creates a new Stripe
  PaymentIntent or updates the existing one for that basket. The resulting `PaymentIntentId` /
  `ClientSecret` are stored on the basket, and later copied onto the `Order` at checkout.
- **`POST /api/Payments/webhook`** — verifies the Stripe signature and handles two event types:
  - `payment_intent.succeeded` → matching order (by `PaymentIntentId`) set to `PaymentReceived`.
  - `payment_intent.payment_failed` → matching order set to `PaymentFaild`.
  - Any other event type is logged and ignored.
- If a new order is created for a basket whose `PaymentIntentId` already has an existing order, the
  old order is removed and replaced — avoiding duplicate orders from repeated checkout attempts on
  the same basket.

---

## API Endpoints

All routes are prefixed `api/[controller]` (controller name, singular where the class name is
singular).

### Account — `api/Account`

| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | `Register` | — | Register a new user; sends an activation email |
| POST | `Login` | — | Authenticate; sets the JWT HttpOnly cookie |
| POST | `Active-Account` | — | Confirm email via token |
| POST | `Send-email-forget-password?email=` | — | Send a password-reset email |
| POST | `Reset-password` | — | Reset password using the emailed token |
| POST | `Logout` | — | Clear the auth cookie |
| GET | `Get-user-name` | ✅ | Return the current user's name |
| GET | `IsUserAuth` | — | `200` if authenticated, `400` otherwise |

### Products — `api/Product`

| Method | Route | Purpose |
|---|---|---|
| GET | `?search=&categoryId=&sort=&pageNumber=&pageSize=` | Paginated, searchable, filterable, sortable listing |
| GET | `{Id}` | Single product with photos and category |
| POST | `` | Create a product (multipart form, with photos) |
| PUT | `{Id}` | Partial update (any field, optional photo replacement) |
| DELETE | `{Id}` | Delete a product and its photo files |

### Categories — `api/Category`

| Method | Route | Purpose |
|---|---|---|
| GET | `get-all` | List all categories |
| GET | `Get-By-Id/{Id}` | Single category |
| POST | `Add-Category` | Create a category |
| PUT | `Update-Category?Id=` | Update a category (body must match the `Id`) |
| DELETE | `Delete-Category/{Id}` | Delete a category |

### Basket — `api/Basket`

| Method | Route | Purpose |
|---|---|---|
| GET | `{Id}` | Get a basket (or an empty one if none exists yet) |
| PUT | `` | Validate, refresh from DB, and persist the basket |
| DELETE | `{Id}` | Delete a basket |

### Orders — `api/Order` *(all endpoints require auth)*

| Method | Route | Purpose |
|---|---|---|
| POST | `Create-order` | Create an order from a basket |
| GET | `Get-Orders-for-User` | List the current user's orders, most recent first |
| GET | `Get-order-by-id/{id}` | A single order, scoped to the current user |
| GET | `Get-delivery` | Available delivery methods |

### Payments — `api/Payments` *(create endpoint requires auth)*

| Method | Route | Purpose |
|---|---|---|
| POST | `?basketId=&deliveryMethodId=` | Create/update the Stripe PaymentIntent for a basket |
| POST | `webhook` | Stripe → server webhook for payment status updates |

### Ratings — `api/Ratings`

| Method | Route | Purpose |
|---|---|---|
| GET | `get-rating/{productId}` | All ratings for a product |
| POST | `add-rating` | Submit a rating (stars 1–5 + optional comment) for the current user |

### Diagnostics

| Method | Route | Purpose |
|---|---|---|
| GET | `api/Bug/not-found` | Forces a 404, for testing the error pipeline |
| GET | `api/Bug/server-error` | Forces a 500 |
| GET | `api/Bug/bad-request/{Id}` / `api/Bug/bad-request/` | Debug/test endpoints |
| GET | `errors/{statusCode}` | Status-code re-execute handler behind `UseStatusCodePagesWithReExecute` |

---

## Example Requests

**Register**
```
POST api/Account/Register
{
  "userName": "john_doe",
  "displayName": "John Doe",
  "email": "john@example.com",
  "password": "Password1"
}
```

**Login** (sets the `token` HttpOnly cookie)
```
POST api/Account/Login
{
  "email": "john@example.com",
  "password": "Password1"
}
```

**Browse products** (page 1, 6 per page, price descending)
```
GET api/Product?pageNumber=1&pageSize=6&sort=PriceDce
```

**Update a basket** (server refreshes item data from the database)
```
PUT api/Basket
{
  "id": "some-client-guid",
  "basketItems": [
    { "id": 1, "productId": 1, "quantity": 2 }
  ]
}
```

**Create an order** *(requires the auth cookie)*
```
POST api/Order/Create-order
{
  "basketId": "some-client-guid",
  "deliveryMethodId": 1,
  "shippingAddressDto": {
    "firstName": "John",
    "lastName": "Doe",
    "city": "Cairo",
    "zipCode": "12345",
    "street": "Main St 1",
    "state": "Cairo"
  }
}
```

**Create a Stripe PaymentIntent** *(requires the auth cookie)*
```
POST api/Payments?basketId=some-client-guid&deliveryMethodId=1
```

**Add a rating**
```
POST api/Ratings/add-rating
{
  "productId": 1,
  "stars": 5,
  "comment": "Excellent laptop"
}
```

---

## Project Structure

```
Solution1/
├─ Solution1.sln
├─ Ecom.API/                        # Presentation layer
│  ├─ Controllers/
│  ├─ Extensions/MiddlewareExtensions.cs
│  ├─ Helper/                       # ResponseAPI, ApiExceptions, Pagination
│  ├─ Mapping/                      # AutoMapper profiles
│  ├─ Middleware/ExceptionMiddleware.cs   # exceptions + rate limiting + security headers
│  ├─ wwwroot/Images/                # uploaded product photos
│  ├─ Program.cs
│  └─ appsettings.json
├─ Ecom.infrastructure/             # Infrastructure / data access
│  ├─ Data/
│  │  ├─ AppDbContext.cs
│  │  ├─ Config/                    # EF Core Fluent API configurations
│  │  ├─ Migrations/
│  │  └─ Seed/ProductSeed.cs        # optional manual seeding helper (not wired into startup)
│  ├─ Repositires/                  # Repository + Unit of Work implementations
│  ├─ Repositires/Service/          # Email, JWT, image storage, orders, Stripe
│  └─ infrastructureRegisteration.cs   # DI registration
└─ Ecom.Core/                       # Domain layer
   ├─ Entities/
   │  ├─ Product/                   # Category, Product, Photo, Rating
   │  └─ Order/                     # Order, OrderItem, DeliveryMethod, ShippingAddress, Status
   ├─ DTO/
   ├─ interfaces/                   # Repository, UnitOfWork, Auth contracts
   ├─ Services/                     # Service interfaces
   └─ Sharing/                      # AuthResult, OrderResult, ProductParams, EmailStringBody
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (the default connection string targets a local `SQLEXPRESS` instance)
- Redis (locally, or via Docker: `docker run -d -p 6379:6379 redis`)
- A [Stripe](https://dashboard.stripe.com/register) account (test-mode keys are enough)

### 1. Clone & restore

```bash
git clone <your-repo-url>
cd Solution1
dotnet restore
```

### 2. Configure secrets

Add the required values (see [Configuration & Secrets](#configuration--secrets)) to
`Ecom.API/appsettings.json`, or — preferably — via `dotnet user-secrets`.

### 3. Apply EF Core migrations

```bash
dotnet ef database update --project Ecom.infrastructure --startup-project Ecom.API
```

This creates the database, all domain and Identity tables, and applies the seed data (test
category/product, delivery methods).

### 4. Run

```bash
dotnet run --project Ecom.API
```

The API listens on `http://localhost:5249` / `https://localhost:7198` (per `launchSettings.json`) and
exposes Swagger at `/swagger` in the `Development` environment.

### 5. Test Stripe webhooks locally

Use the [Stripe CLI](https://stripe.com/docs/stripe-cli) to forward events to your local API:

```bash
stripe listen --forward-to https://localhost:7198/api/payments/webhook
```

Copy the signing secret it prints and update the webhook secret used in `PaymentsController` (see
below).

---

## Configuration & Secrets

`appsettings.json` should **never** contain real secrets in a committed repo. Prefer environment
variables or `dotnet user-secrets` locally:

```bash
cd Ecom.API
dotnet user-secrets init
dotnet user-secrets set "Token:Secret" "a-long-random-secret-at-least-32-characters"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "EmailSetting:Password" "your-app-password"
```

| Key | Purpose |
|---|---|
| `ConnectionStrings:EcomDatabase` | SQL Server connection string |
| `ConnectionStrings:redis` | Redis endpoint (e.g. `localhost`) |
| `EmailSetting:From` / `Smtp` / `Port` / `Username` / `Password` | SMTP settings for MailKit |
| `Token:Secret` | Symmetric signing key for JWT |
| `Token:Issuer` | JWT issuer |
| `CookieSettings:Domain` | Domain for the auth cookie |
| `Stripe:SecretKey` | Stripe secret API key, read by `PaymentService` |

One thing to change before publishing this repo: the Stripe **webhook signing secret** is currently a
hardcoded `const string` inside `PaymentsController` rather than read from configuration — move it
into `appsettings`/user-secrets (e.g. `Stripe:WebhookSecret`) and read it via `IConfiguration`, the
same way `Stripe:SecretKey` is handled in `PaymentService`.

For deployment, use environment variables or a secret manager (Azure Key Vault, AWS Secrets Manager,
etc.) rather than editing `appsettings.json` directly.

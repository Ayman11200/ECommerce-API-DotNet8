# ECommerce API

A full-featured **ASP.NET Core 8** e-commerce Web API built with a clean layered architecture (`Ecom.API`, `Ecom.infrastructure`, `Ecom.Core`). It provides the complete server side for an online store: product catalog, customer basket, authentication & email workflows, order placement, Stripe payments, and product ratings.

---

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Technologies Used](#technologies-used)
- [Authentication & Authorization](#authentication--authorization)
- [Database](#database)
- [Redis / Basket](#redis--basket)
- [Payment](#payment)
- [API Endpoints](#api-endpoints)
- [Example Requests](#example-requests)
- [Project Structure](#project-structure)
- [Setup & Installation](#setup--installation)
- [Configuration / Secrets](#configuration--secrets)
- [Running the Project](#running-the-project)

---

## Features

**Catalog**

- Product listing with **pagination**, **full-text search** (multi-word, across name & description), **category filtering**, and **sorting** (price ascending / descending / name).
- Product detail with category and photo gallery.
- Full CRUD for products and categories, including **multi-image upload** (files persisted under `wwwroot/Images/<ProductName>/`).
- Product photo replacement / cleanup on update and delete.

**Customer Basket (Redis)**

- Redis-backed shopping basket with a 3-day TTL.
- On every basket update, product data (name, price, description, image, category) is **refreshed from SQL Server**, so the server always controls the authoritative price/name.
- Stripe `PaymentIntentId` / `ClientSecret` stored alongside the basket for checkout.

**Authentication & Authorization**

- ASP.NET Core Identity with **JWT delivered inside an HttpOnly cookie**.
- Registration with **email confirmation**, account activation, password reset via email.
- Login with **account lockout** on repeated failures.
- `[Authorize]`-protected order and payment endpoints.

**Email Service**

- SMTP email sending via **MailKit / MimeKit** (HTML body, styled activation / reset buttons).

**Orders**

- Authenticated users can create orders from their basket.
- Order items, subtotal, delivery method selection, shipping address snapshot (owned entity), and order history per user.

**Payments (Stripe)**

- Stripe **PaymentIntent** creation and update (amount = basket items + delivery price).
- Payment **webhook** that marks orders `PaymentReceived` or `PaymentFaild` based on the Stripe event.

**Ratings & Reviews**

- Users can rate products (1–5 stars) with an optional comment.
- A product's aggregate rating is recalculated (rounded to nearest 0.5) and persisted on the product.

**Plumbing**

- Custom exception middleware with **global exception handling**, per-IP **rate limiting** (80 requests / 30 seconds), and **security headers** (`X-Content-Type-Options`, `X-XSS-Protection`, `X-Frame-Options`).
- Swagger / OpenAPI UI in development.

---

## Architecture

The solution is split into three projects with a strict one-way dependency direction:

```
Ecom.API ──► Ecom.infrastructure ──► Ecom.Core
```

| Project | Responsibility |
|---|---|
| **`Ecom.Core`** | Domain layer. Entities, DTOs, repository & service interfaces, shared request/response helpers. No project references. |
| **`Ecom.infrastructure`** | Data-access & infrastructure layer. `AppDbContext`, EF Core configurations, migrations, seed data, repository implementations, service implementations, Redis, and the single DI registration extension. |
| **`Ecom.API`** | Presentation layer. Controllers, request pipeline, middleware, helpers, and AutoMapper profiles. |

**Patterns used**

- **Generic Repository** (`IGenericRepository<T>`) with concrete repositories (`ProductRepository`, `CategoryRepository`, `CustomerBasketRepository`, `RatingRepository`).
- **Unit of Work** (`IUnitOfWork`) that composes repositories over a single shared `AppDbContext` and exposes `SaveChangesAsync()`; it also exposes the `Auth` repository.
- **Service classes** for cross-cutting concerns: `EmailService` (SMTP), `GenerateToken` (JWT), `ImageManagementService` (file I/O), `OrderService` (order orchestration), `PaymentService` (Stripe).
- **AutoMapper** profiles for DTO ⇄ entity mapping (profiles live in `Ecom.API/Mapping`).
- **Dependency injection** registered centrally in `infrastructureRegisteration.infrastructureConfiguration(...)`, called from `Program.cs`.

**Request flow**

```
Controller → IUnitOfWork / Service → Repository → AppDbContext (SQL Server) or Redis
```

---

## Technologies Used

| Technology | Version | Purpose |
|---|---|---|
| .NET / ASP.NET Core Web API | 8.0 | Application framework |
| Entity Framework Core | 8.0.7 | ORM + migrations |
| SQL Server (SqlClient provider) | — | Persistent relational storage |
| ASP.NET Core Identity | 8.0.7 | Users, password hashing, email confirmation, lockout |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.7 | JWT bearer token authentication |
| StackExchange.Redis | 2.8.16 | Shopping-basket cache |
| AutoMapper | 13.0.1 | Object mapping |
| MailKit / MimeKit | 4.17.0 | SMTP email sending |
| Stripe.net | 52.3.0 | Stripe PaymentIntents & webhooks |
| Swashbuckle.AspNetCore | 6.6.2 | Swagger / OpenAPI (development) |

---

## Authentication & Authorization

**Identity setup**

- `AddIdentity<AppUser, IdentityRole>()` with `AddEntityFrameworkStores<AppDbContext>()` and default token providers.
- `AppUser : IdentityUser` adds `DisplayName` and a one-to-one `Address`.
- Password policy: min length 8, requires a digit and a lowercase letter.
- **Email confirmation is enforced at login** — unconfirmed users are blocked and re-sent an activation link.
- **Account lockout** is enabled via `CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)`.

**JWT**

- Tokens are generated by `GenerateToken` with claims for `NameIdentifier` (user id), `Email`, and `Name` (username).
- Signed with `HmacSha256` using `Token:Secret`, issuer `Token:Issuer`, expiry **60 minutes**, no audience validation.
- The token is written to an **HttpOnly cookie named `token`** (Secure, SameSite=None) at login.
- JwtBearer's `OnMessageReceived` reads the token from that cookie, so the browser is authenticated automatically.
- `[Authorize]` protects order, payment, and account ("get-user-name") endpoints.

**Flows**

- **Register** → duplicate user/email checks → create user → send email-confirmation token → activate via `Active-Account`.
- **Login** → find by email → confirm email → check password/lockout → return JWT (cookie).
- **Forget / Reset password** → email a password-reset token (`ResetPasswordAsync`).
- **Logout** → delete the `token` cookie.

Activation and reset emails link to an external front-end at `http://localhost:4200/Account/...` (with a URL-encoded token), which is expected to invoke the corresponding account endpoints.

---

## Database

**Technology:** SQL Server with **EF Core 8** (code-first, migrations).

`AppDbContext : IdentityDbContext<AppUser>` registers the following DbSets:

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

The standard Identity tables (`AspNetUsers`, `AspNetRoles`, `AspNetRoleClaims`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserRoles`, `AspNetUserTokens`) are also created by the `IdentityDbContext` base.

**Key relationships**

| Relationship | Foreign Key | Delete behavior |
|---|---|---|
| `Category` 1 — N `Product` | `Product.CategoryId` | Cascade |
| `Product` 1 — N `Photo` | `Photo.ProductId` | Cascade |
| `AppUser` 1 — 1 `Address` | `Address.AppUserId` (unique) | Cascade |
| `AppUser` N — 1 `Rating` | `Rating.AppUserId` | Cascade |
| `Product` N — 1 `Rating` | `Rating.ProductId` | Cascade |
| `Order` N — 1 `DeliveryMethod` | `Order.DeliveryMethodId` | Restrict |
| `Order` 1 — N `OrderItem` | `OrderItem.OrderId` | Cascade |
| `Order` 1 — 1 `ShippingAddress` (owned) | stored in `Orders` | Owned |

**Notes**

- `ShippingAddress` is an **owned entity** (`OwnsOne`) stored as columns in the `Orders` table — a snapshot of the address at order time.
- `Order.Status` (enum `Status { Pending, PaymentReceived, PaymentFaild }`) is persisted as a **string** via a value converter.
- Money columns (`Product.NewPrice`, `Product.OldPrice`, `Order.SubTotal`, `OrderItem.Price`, `DeliveryMethod.Price`) are `decimal(18,2)`.
- Indexes: unique `DeliveryMethods.Name`, `Orders.BuyerEmail`, composite `Orders(BuyerEmail, OrderDate)`, unique `Addresses.AppUserId`.
- Seeded data: a test `Category` and `Product` via Fluent API `HasData`; two delivery methods (`DHL`, `XXX`) seeded by the `DeliverySeed` migration.

**Migrations (11)**

```
20260725115212_init                 Categories, Products, Photos
20260726051717_SeedDate             Test seed rows
20260727151001_SomeUpdates          Category.Description nullable, max 500
20260729135121_OldPriceAdded        Price → OldPrice + NewPrice
20260730103729_ProductUpdate        Empty migration
20260801192821_Add_Identity_Tables  Identity tables + Addresses
20260808222239_AddOrderEntity       DeliveryMethods, Orders, OrderItems
20260810160312_DeliverySeed         Seed delivery methods
20260810221836_RemoveShippingAddressId  ShippingAddress clean-up
20260811021709_AddingPaymentInit    Order.PaymentIntentId
20260811161435_AddRating            Rating table
```

---

## Redis / Basket

- **StackExchange.Redis** is registered as a singleton (`ConnectionMultiplexer`).
- `CustomerBasket` and `BasketItem` are plain POCOs stored in Redis as JSON via `System.Text.Json`.
- **Key** = client-supplied basket `Id`; **TTL** = 3 days from last update.
- `CustomerBasketRepository.UpdateBasketAsync` validates every item against SQL Server:
  1. Loads the referenced products (with photos + category) from the database.
  2. If any requested product does not exist, the basket is rejected (`400`).
  3. Item fields (`ProductId`, `Name`, `Price` = `NewPrice`, `Description`, `CategoryName`, `Image` = first photo) are overwritten with fresh database values — the client can never dictate price or name.
  4. The refreshed basket is saved back to Redis.
- The basket also carries Stripe `PaymentIntentId` / `ClientSecret` for checkout.

---

## Payment

Stripe is integrated via **PaymentIntents**:

- **Create / update intent** — `POST api/Payments?basketId=...&deliveryMethodId=...` computes the amount from the basket line items plus the selected delivery method's price, and either creates or updates the Stripe PaymentIntent. The `PaymentIntentId` / `ClientSecret` are stored on the basket (Redis) and on the resulting `Order`.
- **Webhook** — `POST api/Payments/webhook` verifies the Stripe signature and handles:
  - `payment_intent.succeeded` → order status set to `PaymentReceived`.
  - `payment_intent.payment_failed` → order status set to `PaymentFaild`.
- `Order.Status` reflects the lifecycle: `Pending → PaymentReceived` (success) or `PaymentFaild` (failure).
- When an order already exists for a basket's `PaymentIntentId`, it is removed and recreated with the same payment intent.
- The Stripe API secret key is read from configuration (`Stripe:SecretKey`).

---

## API Endpoints

Route convention: `api/[controller]`.

### Account — `api/Account`

| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | `api/Account/Register` | — | Register a new user; sends activation email |
| POST | `api/Account/Login` | — | Login; sets the JWT HttpOnly cookie |
| POST | `api/Account/Active-Account` | — | Confirm email with token |
| POST | `api/Account/Send-email-forget-password?email=` | — | Send password-reset email |
| POST | `api/Account/Reset-password` | — | Reset password with token |
| POST | `api/Account/Logout` | — | Delete the auth cookie |
| GET | `api/Account/Get-user-name` | Authorize | Return current user name |
| GET | `api/Account/IsUserAuth` | — | `200` if authenticated, otherwise `400` |

### Products — `api/Product`

| Method | Route | Purpose |
|---|---|---|
| GET | `api/Product?sort=&categoryId=&search=&pageNumber=&pageSize=` | Paginated list with search / filter / sort |
| GET | `api/Product/{Id}` | Single product (with photos + category) |
| POST | `api/Product` | Create product (multipart, with photos) |
| PUT | `api/Product/{Id}` | Partial update (optional photo replacement) |
| DELETE | `api/Product/{Id}` | Delete product + photo files |

### Categories — `api/Category`

| Method | Route | Purpose |
|---|---|---|
| GET | `api/Category/get-all` | List categories |
| GET | `api/Category/Get-By-Id/{Id}` | Single category |
| POST | `api/Category/Add-Category` | Create category |
| PUT | `api/Category/Update-Category?Id=` | Update category (body `CategoryDto`, Id must match) |
| DELETE | `api/Category/Delete-Category/{Id}` | Delete category |

### Basket — `api/Basket`

| Method | Route | Purpose |
|---|---|---|
| GET | `api/Basket/{Id}` | Get basket (or empty basket if none) |
| PUT | `api/Basket` | Validate + refresh + save basket in Redis |
| DELETE | `api/Basket/{Id}` | Delete basket |

### Orders — `api/Order` *(Authorize)*

| Method | Route | Purpose |
|---|---|---|
| POST | `api/Order/Create-order` | Create an order from a basket |
| GET | `api/Order/Get-Orders-for-User` | Orders for the current user |
| GET | `api/Order/Get-order-by-id/{id}` | A single order by id (scoped to the user) |
| GET | `api/Order/Get-delivery` | Available delivery methods |

### Payments — `api/Payments` *(Authorize)*

| Method | Route | Purpose |
|---|---|---|
| POST | `api/Payments?basketId=&deliveryMethodId=` | Create or update the Stripe PaymentIntent |
| POST | `api/Payments/webhook` | Stripe webhook — update order status on payment events |

### Ratings — `api/Ratings`

| Method | Route | Purpose |
|---|---|---|
| GET | `api/Ratings/get-rating/{productId}` | Ratings for a product |
| POST | `api/Ratings/add-rating` | Add a rating (stars + optional comment) |

### Diagnostics

| Method | Route | Controller | Purpose |
|---|---|---|---|
| GET | `api/Bug/not-found` | BugController | Forced 404 |
| GET | `api/Bug/server-error` | BugController | Forced 500 |
| GET | `api/Bug/bad-request/{Id}` | BugController | Debug |
| GET | `api/Bug/bad-request/` | BugController | Forced 400 |
| GET | `errors/{statusCode}` | ErrorController | Status-code page re-execute handler |

---

## Example Requests

**Register a user**

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

**List products** (page 1, page size 6, sorted by price descending)

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

**Create an order** *(requires auth cookie)*

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

**Create a Stripe PaymentIntent** *(requires auth cookie)*

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
├─ Ecom.API/                       # Presentation layer
│  ├─ Controllers/
│  │  ├─ AccountController.cs
│  │  ├─ BaseController.cs
│  │  ├─ BasketController.cs
│  │  ├─ BugController.cs
│  │  ├─ CategoryController.cs
│  │  ├─ ErrorController.cs
│  │  ├─ OrderController.cs
│  │  ├─ PaymentsController.cs
│  │  ├─ ProductController.cs
│  │  └─ RatingController.cs
│  ├─ Extensions/MiddlewareExtensions.cs
│  ├─ Helper/                      # ResponseAPI, ApiExceptions, Pagination
│  ├─ Mapping/                     # AutoMapper profiles (Product, Category, Order, ShippingAddress)
│  ├─ Middleware/ExceptionMiddleware.cs   # exception handling + rate limiting + security headers
│  ├─ wwwroot/Images/              # uploaded product photos
│  ├─ Program.cs
│  ├─ appsettings.json
│  └─ Properties/launchSettings.json
├─ Ecom.infrastructure/            # Infrastructure / data access
│  ├─ Data/
│  │  ├─ AppDbContext.cs
│  │  ├─ Config/                   # Fluent API configurations
│  │  ├─ Migrations/               # 11 EF Core migrations + snapshot
│  │  └─ Seed/ProductSeed.cs
│  ├─ Repositires/                 # GenericRepository, Product, Category, CustomerBasket, Auth, Rating, UnitOfWork
│  ├─ Repositires/Service/         # EmailService, GenerateToken, ImageManagementService, OrderService, PaymentService
│  └─ infrastructureRegisteration.cs  # DI registration
└─ Ecom.Core/                      # Domain layer
   ├─ Entities/                    # BaseEntity, AppUser, Address, CustomerBasket, BasketItem
   │  ├─ Product/                  # Category, Product, Photo, Rating
   │  └─ Order/                    # Order, OrderItem, DeliveryMethod, ShippingAddress, Status
   ├─ DTO/                         # Register/Login/Product/Category/Order/Rating/Email DTOs
   ├─ interfaces/                  # Repository, UnitOfWork, Auth contracts
   ├─ Services/                    # Service interfaces
   └─ Sharing/                     # AuthResult, OrderResult, ProductParams, EmailStringBody
```

---

## Setup & Installation

**Prerequisites**

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (the default connection string targets a local `SQLEXPRESS` instance)
- Redis server (default `localhost:6379`)
- A [Stripe](https://stripe.com) account for payments and a MailKit-compatible SMTP account for email

**Clone**

```bash
git clone <repository-url>
cd Solution1
```

**Restore packages**

```bash
dotnet restore Solution1.sln
```

**Configure secrets** — add the required values to `Ecom.API/appsettings.json` (or better, use user-secrets / environment variables). See [Configuration / Secrets](#configuration--secrets).

**Apply database migrations**

```bash
dotnet ef database update --project Ecom.infrastructure --startup-project Ecom.API
```

This creates the database, all domain and Identity tables, and seeds the test category/product and delivery methods.

**Run**

```bash
dotnet run --project Ecom.API
```

The API listens on `http://localhost:5249` / `https://localhost:7198` and exposes Swagger at `/swagger`.

---

## Configuration / Secrets

All configuration values below are **required** by the application. Never commit real secrets — provide them via environment variables, user-secrets, or your secrets manager.

| Key | Description | Placeholder |
|---|---|---|
| `ConnectionStrings:EcomDatabase` | SQL Server connection string | `Server=YOUR_SQL_SERVER;Database=YOUR_DATABASE;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true` |
| `ConnectionStrings:redis` | Redis endpoint | `YOUR_REDIS_ENDPOINT` (e.g. `localhost`) |
| `EmailSetting:From` | From email address shown on sent emails | `YOUR_FROM_EMAIL` |
| `EmailSetting:Smtp` | SMTP host | `YOUR_SMTP_HOST` |
| `EmailSetting:Port` | SMTP port (SSL) | `YOUR_SMTP_PORT` |
| `EmailSetting:Username` | SMTP username | `YOUR_SMTP_USERNAME` |
| `EmailSetting:Password` | SMTP password / app password | `YOUR_SMTP_PASSWORD` |
| `Token:Secret` | Symmetric signing key for JWT | `YOUR_JWT_SECRET` |
| `Token:Issuer` | JWT issuer | `YOUR_JWT_ISSUER` |
| `CookieSettings:Domain` | Domain for the auth cookie | `localhost` (or your domain) |
| `Stripe:SecretKey` | Stripe secret API key (read by `PaymentService`) | `YOUR_STRIPE_SECRET_KEY` |

The Stripe **webhook signing secret** used to validate webhook signatures is set in `PaymentsController` — replace it with your own `whsec_...` value from the Stripe dashboard / CLI.

---

## Running the Project

```bash
# Build the whole solution
dotnet build Solution1.sln

# Apply migrations to create/update the database
dotnet ef database update --project Ecom.infrastructure --startup-project Ecom.API

# Run the API
dotnet run --project Ecom.API
```

Open `https://localhost:7198/swagger` (or `http://localhost:5249/swagger`) in development to explore and test the endpoints. Use the **Account → Register / Login** endpoints first, then the authenticated order and payment endpoints.

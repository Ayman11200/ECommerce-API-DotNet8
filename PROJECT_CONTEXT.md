# PROJECT_CONTEXT.md

> **Documentation generated from the actual source code.** Any statement that could not be
> confirmed from the source is explicitly marked **"Not confirmed from current source code."**
>
> **Status of this project: NOT FINISHED.**
> - Authentication / Authorization: **COMPLETED**
> - Users / Identity: **COMPLETED**
> - Products / Categories: **COMPLETED**
> - Customer Basket / BasketItems / Redis: **COMPLETED**
> - Email service: **COMPLETED**
> - JWT authentication: **COMPLETED**
> - Orders: **IN PROGRESS** (partially implemented, not finished, build currently broken)
> - Payments: **PLANNED** (not implemented)
> - Rating / Reviews: **PLANNED** (not implemented)
>
> Future AI assistants must read the section *"Instructions for Future AI Assistants"* at the end.

---

## 1. PROJECT OVERVIEW

### What the project is

`Solution1` is a back-end **E-Commerce Web API** built with **ASP.NET Core 8** (.NET 8), organized into a
three-project solution (`Ecom.Core`, `Ecom.infrastructure`, `Ecom.API`).

### Main purpose

Provide the server-side API for an online store: manage products and categories, maintain a customer
shopping basket, authenticate/register users, send emails, and (in progress / planned) process orders,
payments, and product ratings.

### Current architectural approach

A pragmatic **layered architecture** rather than a strict Clean/Onion architecture:

- **`Ecom.Core`** — domain entities, DTOs, repository/service *interfaces*, and shared params/results.
- **`Ecom.infrastructure`** — implementations: EF Core `DbContext`, migrations, repositories, services,
  Redis integration, and a single DI registration extension.
- **`Ecom.API`** — presentation: controllers, `Program.cs`, middleware, helper types, and AutoMapper profiles.

Key patterns in use:
- **Generic Repository + concrete repositories** (`IGenericRepository<T>`, `IProductRepository`, ...).
- **Unit of Work** (`IUnitOfWork`) which also exposes `Auth`.
- **Services** for cross-cutting concerns: email, JWT token generation, image file management, and orders.
- **EF Core (SQL Server)** for persistent data; **Redis** for the shopping basket.
- **ASP.NET Core Identity** for users; **JWT** delivered inside an **HttpOnly cookie** for authentication.
- **AutoMapper** for DTO ⇄ entity mapping (profiles live in the API project).

### Current project status

- The last **committed** milestone is the *Auth feature* (`git log` top commit: `End of Auth Feature`).
- The **Order module is actively being developed and is uncommitted working-tree changes**.
- The solution **does not currently compile**: `OrderService.GetAllOrdersForUserAsync` has an empty body
  and produces the only compile error (`CS0161`). See section 12.
- There are **no test projects** (confirmed via glob for `*Tests*.csproj`).

### Main technologies

| Technology | Purpose |
|---|---|
| .NET 8 / ASP.NET Core Web API | Application framework |
| EF Core 8.0.7 | ORM, migrations |
| SQL Server | Persistent relational storage |
| ASP.NET Core Identity 8.0.7 | Users, password hashing, email confirmation |
| JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.7) | Token authentication |
| StackExchange.Redis 2.8.16 | Shopping-basket cache |
| AutoMapper 13.0.1 | Object mapping |
| MailKit 4.17.0 / MimeKit 4.17.0 | SMTP email sending |
| Swashbuckle 6.6.2 | Swagger / OpenAPI (development only) |

### Current implemented modules

- Products & Categories (incl. photo file upload/delete).
- Customer Basket (Redis-based).
- Authentication / Authorization (Identity + JWT in cookie + email confirmation).
- Email service (MailKit/MimeKit).
- Order *domain model + create flow* (partial — see section 12).

### Modules currently being developed

- **Order module** (entities, DTOs, configuration, `OrderService.CreateOrderAsync`, EF migration are drafted;
  DI registration, controller, and remaining service methods are missing).

### Planned modules

- **Payment** (only a hint exists: `Status` enum members `Pending`, `PaymentReceived`, `PaymentFaild`).
- **Rating / Reviews** (no code exists; described conceptually in section 14).

---

## 2. SOLUTION / PROJECT STRUCTURE

Solution file: `Solution1.sln` — contains a solution folder `src` hosting the three projects.

### Dependency direction

```
Ecom.API  ──►  Ecom.infrastructure  ──►  Ecom.Core
                     ▲                       ▲
                     │                       └── (Ecom.Core has NO project references)
                     └── references Ecom.Core
```

- `Ecom.Core` references **nothing** (packages only).
- `Ecom.infrastructure` references `Ecom.Core`.
- `Ecom.API` references `Ecom.infrastructure` only (transitively gets `Ecom.Core`).
- No project depends on `Ecom.API`, so presentation concerns never leak downward.

---

### 2.1 `Ecom.Core` — Domain layer

- **Responsibility:** Entities, DTOs, repository & service contracts, shared request/response helpers.
- **Allowed to depend on:** nothing internal (only framework packages below).
- **Packages:** `Microsoft.AspNetCore.Http.Features` 5.0.17, `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.7.
- **Target:** `net8.0`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`.

Folders:
| Folder | Contents |
|---|---|
| `Entities/` | Domain entities (see section 4). Sub-folders `Product/` and `Order/`. |
| `Entities/Address.cs`, `AppUser.cs`, `BaseEntity.cs`, `BasketItem.cs`, `CustomerBasket.cs` | Shared entities. |
| `DTO/` | DTOs. Namespace **inconsistent**: mostly `Ecom.Core.Dto`, but order DTOs use `Ecom.Core.DTO` (capital). |
| `interfaces/` | Repository & UnitOfWork interfaces (`IAuth`, `ICategoryRepository`, `ICustomerBasketRepository`, `IGenericRepository<T>`, `IProductRepository`, `IUnitOfWork`). |
| `Services/` | Service contracts (`IEmailService`, `IGenerateToken`, `IImageManagementService`, `IOrderService`). |
| `Sharing/` | Shared helpers (`AuthResult`, `EmailStringBody`, `ProductParams`). |

Important classes/interfaces:
- `BaseEntity<T>` — generic Id base (line: `Ecom.Core/Entities/BaseEntity.cs`).
- `IGenericRepository<T>`, `IProductRepository`, `ICategoryRepository`, `ICustomerBasketRepository`, `IUnitOfWork`, `IAuth`.
- `IOrderService` (order — IN PROGRESS).

**Quirks to be aware of:**
- The `interfaces` folder/namespace is **lowercase** (`Ecom.Core.interfaces`).
- `DTO/ProductDto.cs` declares records **without any namespace** (global namespace).
- `DTO/OrderDto.cs` uses namespace `Ecom.Core.DTO` while all other DTOs use `Ecom.Core.Dto`.
- `Class1.cs` (junk) files exist in `Ecom.Core/` and `Ecom.Core/Entities/Product/`.

---

### 2.2 `Ecom.infrastructure` — Data access & infrastructure layer

- **Responsibility:** EF Core `DbContext`, entity configurations, migrations, seed data, repository
  implementations, service implementations, and the single DI registration extension.
- **Allowed to depend on:** `Ecom.Core` (and NuGet packages).
- **Packages:** `AutoMapper` 13.0.1, `MailKit` 4.17.0, `MimeKit` 4.17.0,
  `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.7, `Microsoft.AspNetCore.Identity` 2.3.11,
  `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.7, `Microsoft.EntityFrameworkCore` 8.0.7
  (+ `Design`, `SqlServer`, `Tools`), `Microsoft.Extensions.FileProviders.Abstractions/Physical`,
  `StackExchange.Redis` 2.8.16.

Folders:
| Folder | Contents |
|---|---|
| `Data/` | `AppDbContext.cs` |
| `Data/Config/` | `CategoryConfigration.cs`, `DeliveryMethodConfiguration.cs`, `OrderConfiguration.cs`, `ProductConfigration.cs` |
| `Data/Migrations/` | 7 migrations + `AppDbContextModelSnapshot.cs` |
| `Data/Seed/` | `ProductSeed.cs` (seed helper — **not invoked from `Program.cs`**) |
| `Repositires/` | `GenericRepository<T>`, `ProductRepository`, `CategoryRepository`, `CustomerBasketRepository`, `AuthRepository`, `UnitOfWork` (note: folder misspelled "Repositires") |
| `Repositires/Service/` | `EmailService`, `GenerateToken`, `ImageManagementService`, `OrderService` |
| root | `infrastructureRegisteration.cs` (DI) |

Important classes/interfaces: see sections 3, 8, 9, 10, 11, 12.

---

### 2.3 `Ecom.API` — Presentation layer

- **Responsibility:** HTTP API surface: controllers, request pipeline, middleware, helpers, AutoMapper profiles, static image hosting.
- **Allowed to depend on:** `Ecom.infrastructure` (transitively `Ecom.Core`).
- **Packages:** `AutoMapper` 13.0.1, `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.7,
  `Microsoft.EntityFrameworkCore.Design` 8.0.7, `StackExchange.Redis` 2.8.16, `Swashbuckle.AspNetCore` 6.6.2.

Folders:
| Folder | Contents |
|---|---|
| `Controllers/` | `AccountController`, `BaseController`, `BasketController`, `BugController`, `CategoryController`, `ErrorController`, `ProductController` (no `OrderController` yet) |
| `Extensions/` | `MiddlewareExtensions.cs` |
| `Helper/` | `ApiExceptions.cs`, `Pagination.cs`, `ResponseAPI.cs` |
| `Mapping/` | AutoMapper profiles: `CategoryMapping.cs`, `ProductMapping.cs`, `ShippingAddressMapping.cs` |
| `Middleware/` | `ExceptionMiddleware.cs` (exception handling + rate limiting + security headers) |
| `wwwroot/Images/` | Uploaded product photos |

How projects communicate:
- Controllers receive **`IUnitOfWork` + `IMapper`** from DI (base controller).
- Controllers call repository/unit-of-work methods directly; there is **no service layer in front of most
  controllers**. `OrderService` is the exception and it is **not yet wired into DI**.

---

## 3. ARCHITECTURE

### Actual architecture in use

**Layered, repository-centric API.** There is no CQRS, no MediatR, no separate Application layer, and no
Result-object pattern beyond `AuthResult`. The layers:

1. **API / Presentation (`Ecom.API`)**
   - Controllers inherit `BaseController` (`[Route("api/[controller]")]`, `[ApiController]`).
   - `Program.cs` builds the pipeline: custom exception middleware → authentication → authorization →
     Swagger (dev) → status-code pages re-execute → HTTPS redirect → controllers.

2. **Core (`Ecom.Core`)**
   - Entities, DTOs, repository interfaces, service interfaces, `Sharing` params/results.

3. **Infrastructure (`Ecom.infrastructure`)**
   - `AppDbContext`, EF configurations, migrations, repository + service implementations, Redis, DI registration.

### Component interaction summary

- **Controllers** → `IUnitOfWork` → repositories → `AppDbContext` / Redis.
- **Repositories** encapsulate data access; controllers call `work.SaveChangesAsync()` to persist.
- **Unit of Work** owns repository instances and a single `AppDbContext`; `SaveChangesAsync()` delegates to
  `_context.SaveChangesAsync()`.
- **Services**:
  - `IEmailService` — SMTP sending (used by `AuthRepository`).
  - `IGenerateToken` — JWT creation (used by `AuthRepository`).
  - `IImageManagementService` — file save/delete (used by `ProductRepository`).
  - `IOrderService` / `OrderService` — order creation (IN PROGRESS, not registered in DI).
- **EF Core** — `IdentityDbContext<AppUser>`; configurations applied via
  `modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly())`.
- **Dependency Injection** — everything is registered in one place:
  `services.infrastructureConfiguration(configuration)` (in `infrastructureRegisteration.cs`), called from
  `Program.cs`.
- **AutoMapper** — registered via `AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies())`; profiles in
  `Ecom.API/Mapping`.
- **Redis** — `IConnectionMultiplexer` singleton; basket repository uses it.
- **Identity** — `AddIdentity<AppUser, IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders()`.
- **JWT** — configured with `JwtBearer` + a companion `Cookie` scheme (details in section 6).

---

## 4. DOMAIN / ENTITIES

Namespace notes:
- Product entities: `Ecom.Core.Entities.Product`
- Order entities: `Ecom.Core.Entities.Order`
- Shared entities: `Ecom.Core.Entities`

### 4.1 `BaseEntity<T>` (`Ecom.Core.Entities`)

- Property: `T Id { get; set; }` (generic primary key; all concrete entities use `int`, except Identity users which use `string`).
- Used by: every EF entity except `AppUser` (Identity) and the Redis-only basket types.

### 4.2 `AppUser` (`Ecom.Core.Entities`)

- **Base class:** `Microsoft.AspNetCore.Identity.IdentityUser` (PK `string Id`).
- Properties: `string DisplayName` (required); navigation `Address? Address` (1:1).
- Table: `AspNetUsers` (Identity default).
- Not configured with data annotations beyond Identity defaults.

### 4.3 `Address` (`Ecom.Core.Entities`)

- **Base class:** `BaseEntity<int>`.
- Properties: `FirstName`, `LastName`, `City`, `ZipCode`, `Street`, `State` (all `string`); FK `string AppUserId`.
- Navigation: `AppUser AppUser` (`[ForeignKey(nameof(AppUserId))]`).
- Relationship: **1:1** with `AppUser` (unique index on `AppUserId`; `DeleteBehavior.Cascade`).
- Own table: `Addresses`. Created by migration `Add_Identity_Tables`.
- Business meaning: the user's saved address (updated via `IAuth.UpdateAddress` — no controller endpoint exposes it yet).

### 4.4 `Product` (`Ecom.Core.Entities.Product`)

- **Base class:** `BaseEntity<int>`.
- Properties:
  - `string Name`, `string Description` (required).
  - `decimal NewPrice` (`[Required]`), `decimal OldPrice`.
  - FK `int CategoryId` (`[Required]`), `[ForeignKey(nameof(CategoryId))]`.
- Navigations: `Category Category`; `List<Photo> Photos`.
- Own table: `Products`.
- Configuration (`ProductConfigration.cs`): `Name`/`Description` required; `NewPrice`/`OldPrice`
  `HasColumnType("decimal(18,2)")`; seeded test row via `HasData`.
- Constraints: prices are `decimal(18,2)`.

### 4.5 `Category` (`Ecom.Core.Entities.Product`)

- **Base class:** `BaseEntity<int>`.
- Properties: `string Name`; `string? Description` (optional); navigation `ICollection<Product> Products` (`HashSet<Product>`).
- Own table: `Categories`.
- Configuration (`CategoryConfigration.cs`): `Name` required, max 30; `Description` optional, max 500; test row via `HasData`.
- Note: `Category` is also used for the BasketItem's `CategoryName` display string (basket stores a denormalized copy).

### 4.6 `Photo` (`Ecom.Core.Entities.Product`)

- **Base class:** `BaseEntity<int>`.
- Properties: `string ImageName` (a URL path like `/Images/<ProductName>/<file>.jpg`); FK `int ProductId`; navigation `Product Product`.
- Own table: `Photos`; FK to `Products` with `DeleteBehavior.Cascade`.
- Business meaning: product gallery images stored physically in `wwwroot/Images/<ProductName>/` and referenced by URL in the DB.

### 4.7 `CustomerBasket` (`Ecom.Core.Entities`)

- **NOT an EF entity — no DbSet, not in SQL Server. Redis only.**
- Properties: `string Id`; `List<BasketItem> BasketItems`.
- Constructors: parameterless, and `CustomerBasket(string id)`.
- Stored in Redis as JSON; key = basket `Id`.

### 4.8 `BasketItem` (`Ecom.Core.Entities`)

- **NOT an EF entity — Redis only.**
- Properties: `int Id` (mirrors `Product.Id`), `int ProductId`, `string Name`, `string Description`,
  `int Quantity`, `string Image`, `decimal Price`, `string CategoryName`.
- Business meaning: a denormalized snapshot of a product inside the basket. Fresh values are written from SQL Server
  on every basket update (see section 8).

---

### 4.9 Order entities — **IN PROGRESS**

All under `Ecom.Core.Entities.Order`. Entities exist and a migration exists, but the module is not finished
(no controller, no DI registration, build error in `OrderService`).

#### `Order`

- **Base class:** `BaseEntity<int>`.
- Constructor: `Order(string buyerEmail, decimal subTotal, DeliveryMethod deliveryMethod, List<OrderItem> orderItems, ShippingAddress shippingAddress)`.
- Properties:
  - `string BuyerEmail` (`[Required]`, `[MaxLength(256)]`) — the ordering user's email.
  - `DateTime OrderDate` (`[Required]`, defaults `DateTime.Now`).
  - `Status Status` (`[Required]`, defaults `Pending`) — stored as string (see config).
  - `decimal SubTotal` (`[Required]`, `decimal(18,2)`).
  - FK `int DeliveryMethodId` (`[Required]`).
  - Navigation `DeliveryMethod DeliveryMethod`.
  - `List<OrderItem> OrderItems`.
  - Owned `ShippingAddress ShippingAddress`.
  - **Computed** `decimal Total => SubTotal + DeliveryMethod.Price`.
- Table: `Orders`.
- Configuration (`OrderConfiguration.cs`): FK to `DeliveryMethod` **Restrict**; `OrderItems` cascade;
  `ShippingAddress` is **owned** (`OwnsOne`) and required; indexes on `BuyerEmail` and `(BuyerEmail, OrderDate)`;
  `Status` stored as **string** via value converter.

#### `OrderItem`

- **Base class:** `BaseEntity<int>`.
- Constructor: `OrderItem(int productItemId, string productName, string mainImage, int quantity, decimal price)`.
- Properties: `int ProductItemId` (`[Required]`), `string ProductName` (`[Required]`, max 200),
  `string MainImage` (`[Required]`, max 300), `int Quantity` (`[Range(1, int.MaxValue)]`),
  `decimal Price` (`decimal(18,2)`), FK `int OrderId`.
- Table: `OrderItems`; cascade delete from `Order`. **No navigation property back to `Order`** (configured `.WithOne()`).

#### `DeliveryMethod`

- **Base class:** `BaseEntity<int>`.
- Constructor: `DeliveryMethod(string name, string description, decimal price, string deliveryTime)`.
- Properties: `string Name` (max 100), `string Description` (max 500), `decimal Price` (`decimal(18,2)`),
  `string DeliveryTime` (max 50).
- Table: `DeliveryMethods`; **unique index on `Name`** (`DeliveryMethodConfiguration.cs`).
- Business meaning: shipping option (name, price, delivery time).

#### `ShippingAddress`

- **Base class:** `BaseEntity<int>` (this adds an `Id` even though it is owned).
- Constructor: `ShippingAddress(string firstName, string lastName, string city, string zipCode, string street, string state)`.
- Properties: `FirstName` (max 100), `LastName` (max 100), `City` (max 100), `ZipCode` (max 20), `Street` (max 200), `State` (max 100) — all `[Required]`.
- **Owned entity** — no own table; its columns are stored in the `Orders` table as `ShippingAddress_*`.

#### `Status` (enum, `Ecom.Core.Entities.Order`)

```csharp
public enum Status { Pending, PaymentReceived, PaymentFaild }
```

- Stored as **string** (max 20) in the DB via the value converter in `OrderConfiguration`.
- **Note the typo `PaymentFaild`** (should be `PaymentFailed`) — preserve it or fix deliberately; it is part of the current code.

---

## 5. DATABASE / EF CORE

### DbContext

`Ecom.infrastructure.Data.AppDbContext : IdentityDbContext<AppUser>`

- Registered with `UseSqlServer(configuration.GetConnectionString("EcomDatabase"))`.
- Configurations loaded via `ApplyConfigurationsFromAssembly`.

### DbSets

| DbSet | Entity | Notes |
|---|---|---|
| `Products` | `Product` | |
| `Categories` | `Category` | |
| `Photos` | `Photo` | |
| `Addresses` | `Address` | |
| `Orders` | `Order` | IN PROGRESS |
| `OrderItems` | `OrderItem` | IN PROGRESS |
| `DeliveryMethods` | `DeliveryMethod` | IN PROGRESS |

(`CustomerBasket` / `BasketItem` are **not** DbSets — Redis only.)

### EF Core version & provider

- EF Core **8.0.7**, provider **Microsoft.EntityFrameworkCore.SqlServer**.

### Migrations (chronological)

1. `20260725115212_init` — `Categories`, `Products`, `Photos` + FKs.
2. `20260726051717_SeedDate` — inserts test category/product rows.
3. `20260727151001_SomeUpdates` — `Category.Description` → nullable, max 500.
4. `20260729135121_OldPriceAdded` — rename `Price` → `OldPrice`, add `NewPrice` (decimal(18,2)).
5. `20260730103729_ProductUpdate` — **empty** migration (no operations).
6. `20260801192821_Add_Identity_Tables` — Identity tables (`AspNetUsers`, `AspNetRoles`, claims, logins,
   roles, tokens) **and** `Addresses` (1:1 FK to `AspNetUsers`).
7. `20260808222239_AddOrderEntity` — `DeliveryMethods`, `Orders`, `OrderItems` (Order module — **uncommitted IN PROGRESS**).

### Fluent API configurations

**`ProductConfigration.cs`** — `Name`/`Description` required; `NewPrice`/`OldPrice` `decimal(18,2)`; seed row.
**`CategoryConfigration.cs`** — `Name` required max 30; `Description` optional max 500; seed row.
**`DeliveryMethodConfiguration.cs`** — unique index on `Name`.
**`OrderConfiguration.cs`** — the most important:

```csharp
builder.Property(o => o.Status)
    .HasConversion(
        s => s.ToString(),
        s => (Status)Enum.Parse(typeof(Status), s))
    .HasMaxLength(20)
    .IsRequired();

builder.HasOne(o => o.DeliveryMethod)
    .WithMany()
    .HasForeignKey(o => o.DeliveryMethodId)
    .IsRequired()
    .OnDelete(DeleteBehavior.Restrict);

builder.HasMany(o => o.OrderItems)
    .WithOne()
    .HasForeignKey(oi => oi.OrderId)
    .IsRequired()
    .OnDelete(DeleteBehavior.Cascade);

builder.OwnsOne(o => o.ShippingAddress).WithOwner();
builder.Navigation(o => o.ShippingAddress).IsRequired();

builder.HasIndex(o => o.BuyerEmail);
builder.HasIndex(o => new { o.BuyerEmail, o.OrderDate });
```

### Relationships, FKs, delete behaviors

| Relationship | FK | Delete behavior |
|---|---|---|
| `Category` 1 — N `Product` | `Product.CategoryId` | Cascade |
| `Product` 1 — N `Photo` | `Photo.ProductId` | Cascade |
| `AppUser` 1 — 1 `Address` | `Address.AppUserId` (unique) | Cascade |
| `Order` N — 1 `DeliveryMethod` | `Order.DeliveryMethodId` | **Restrict** |
| `Order` 1 — N `OrderItem` | `OrderItem.OrderId` | Cascade |
| `Order` 1 — 1 `ShippingAddress` (owned) | stored in `Orders` | Owned |

### Owned entities

- `ShippingAddress` is owned by `Order` (`OwnsOne`); stored as `ShippingAddress_*` columns in `Orders`.
  Because `ShippingAddress` derives from `BaseEntity<int>`, an extra `ShippingAddress_Id` column is also
  present in the migration.

### Enum conversions

- `Order.Status` stored as a **string** (max 20) via the value converter in `OrderConfiguration`.

### Indexes & unique constraints

- `DeliveryMethods.Name` — **unique**.
- `Orders.BuyerEmail` — index.
- `Orders(BuyerEmail, OrderDate)` — composite index.
- `Addresses.AppUserId` — **unique** (enforces 1:1 with user).
- Identity-standard indexes on `AspNet*` tables (e.g., unique `NormalizedUserName`, `NormalizedEmail`).

### Decimal precision

All monetary columns (`Product.NewPrice`, `Product.OldPrice`, `Order.SubTotal`, `OrderItem.Price`,
`DeliveryMethod.Price`) are `decimal(18,2)`.

### Required / optional

- Most string/business columns required. Optional: `Category.Description`, and the standard nullable
  Identity columns (`Email`, `PhoneNumber`, etc. are nullable at DB level).
- `ShippingAddress` navigation is required on `Order`.

### Why important configurations exist

- `decimal(18,2)` avoids float rounding for money.
- `Status` as string keeps the DB human-readable and avoids enum-int fragility.
- `Restrict` on `DeliveryMethod` prevents deleting a delivery method referenced by orders.
- `Cascade` on `OrderItems`/`Photos` cleans up children when the parent is removed.
- Indexes on `BuyerEmail`/`OrderDate` support order-history lookups by user.
- Unique index on `DeliveryMethod.Name` enforces one row per delivery option.
- Owned `ShippingAddress` snapshots the address at order time (immune to later user-address edits).

---

## 6. AUTHENTICATION / AUTHORIZATION

All implemented. No authorization roles are seeded or enforced in practice.

### Identity setup

- `AddIdentity<AppUser, IdentityRole>()` → `.AddEntityFrameworkStores<AppDbContext>()` → `.AddDefaultTokenProviders()`.
- Password policy (`IdentityOptions`):
  - `RequireDigit = true`
  - `RequireUppercase = false`
  - `RequireLowercase = true`
  - `RequireNonAlphanumeric = false`
  - `RequiredLength = 8`
- Email confirmation: **enforced at login** (unconfirmed users cannot log in and are re-sent an activation email).
- Lockout: `signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)` — lockout message handled.

### `AppUser`

`AppUser : IdentityUser` with `DisplayName` (required) and `Address?` (1:1).

### `UserManager` / `SignInManager`

Used inside `AuthRepository` (`Ecom.infrastructure/Repositires/AuthRepository.cs`):
- `UserManager<AppUser>` — create user, find by name/email, generate/confirm email tokens, password reset.
- `SignInManager<AppUser>` — `CheckPasswordSignInAsync`.

### `IdentityRole`

`IdentityRole` is registered but no roles are seeded and no `[Authorize(Roles=...)]` exists.
**"Not confirmed from current source code":** any role-based authorization flow.

### JWT token generation — `GenerateToken` (`IGenerateToken`)

- Claims added:
  - `ClaimTypes.NameIdentifier` → `user.Id`
  - `ClaimTypes.Email` → `user.Email`
  - `ClaimTypes.Name` → `user.UserName`
- Signing: `HmacSha256`, `SymmetricSecurityKey` from `Token:Secret`.
- Issuer: `Token:Issuer`; **no audience** configured.
- Expiry: `DateTime.UtcNow.AddMinutes(60)`.

### Token validation — `infrastructureRegisteration`

```csharp
services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(x =>
{
    x.Cookie.Name = "token";
    x.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Token:Secret"])),
        ValidateIssuer = true,
        ValidIssuer = configuration["Token:Issuer"],
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["token"];
            context.Token = token;
            return Task.CompletedTask;
        }
    };
});
```

### Cookie / JWT interaction

- The JWT is delivered in an **HttpOnly cookie named `token`** (set by `AccountController.Login`).
- JwtBearer's `OnMessageReceived` reads the token from that cookie → the browser is authenticated automatically.
- `DefaultScheme = Cookie`, but `DefaultAuthenticateScheme`/`DefaultChallengeScheme = JwtBearer` (a mixed setup; the cookie scheme mainly supplies the 401 redirect override).

### Middleware configuration (in `Program.cs`)

```csharp
app.UseCustomExceptionMiddleware();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseStatusCodePagesWithReExecute("/errors/{0}");
app.UseHttpsRedirection();
app.MapControllers();
```

### Endpoints (all implemented)

| Route | Method | Auth | Purpose |
|---|---|---|---|
| `api/Account/Register` | POST | None | Register new user; sends activation email |
| `api/Account/Login` | POST | None | Login; sets JWT cookie |
| `api/Account/Active-Account` | POST | None | Confirm email with token |
| `api/Account/Send-email-forget-password?email=` | POST | None | Send password reset email |
| `api/Account/Reset-password` | POST | None | Reset password with token |
| `api/Account/Logout` | POST | None | Deletes the JWT cookie |
| `api/Account/Get-user-name` | GET | `[Authorize]` | Returns `User.Identity.Name` |
| `api/Account/IsUserAuth` | GET | None | 200 if authenticated, else 400 |

### Registration flow

1. Client → `POST api/Account/Register` (`RegisterDto`: `UserName`, `DisplayName`, `Email`, `Password`).
2. `AccountController.Register` → `work.Auth.RegisterAsync`.
3. `AuthRepository.RegisterAsync`:
   - Rejects null request; rejects duplicate username and duplicate email.
   - Creates `AppUser` with `UserName`, `Email`, `DisplayName`.
   - `userManager.CreateAsync(user, password)`.
   - On success generates an email-confirmation token and calls `SendEmail` (subject "ActiveEmail").
4. Controller returns `200` on success, `400` with `ResponseAPI` message on failure.

### Login flow

1. Client → `POST api/Account/Login` (`LoginDto`: `Email`, `Password`).
2. `AuthRepository.Login`:
   - Finds user by email (generic "Invalid email or password" on miss).
   - If `!EmailConfirmed` → re-sends activation email, returns failure message.
   - `CheckPasswordSignInAsync(..., true)`; if locked out → failure message.
   - On success → `generateToken.GetAndCreateToken(user)`.
3. `AccountController.Login` writes cookie: `HttpOnly=true`, `Secure=true`, `SameSite=None`, `IsEssential=true`,
   `Domain = configuration["CookieSettings:Domain"]`, `Expires = now + 60 min`.

### Logout flow

`POST api/Account/Logout` → `Response.Cookies.Delete("token")` → 200.

### Email confirmation

- Token generated with `GenerateEmailConfirmationTokenAsync`, emailed via `EmailStringBody` HTML with a link to
  `http://localhost:4200/Account/Active?email=...&code=...` (Angular front-end on port 4200).
- Confirmed via `POST api/Account/Active-Account` (`ActiveAccountDto`); on failure re-sends a new activation email.

### Password reset

- `POST api/Account/Send-email-forget-password?email=` → `GeneratePasswordResetTokenAsync` → email with
  `ResetPassword` component link.
- `POST api/Account/Reset-password` (`PasswordDto`: `Email`, `Password`, `Token`) → `userManager.ResetPasswordAsync`.

---

## 7. PRODUCT / CATEGORY MODULE

### Product endpoints (all implemented, none require auth)

| Route | Method | Purpose |
|---|---|---|
| `api/Product?sort=&categoryId=&search=&pageNumber=&pageSize=` | GET | Paginated/searchable/filterable/sortable list |
| `api/Product/{Id}` | GET | Single product incl. photos + category |
| `api/Product` | POST | Create product (multipart with photos) |
| `api/Product/{Id}` | PUT | Partial update incl. optional photo replacement |
| `api/Product/{Id}` | DELETE | Delete product + its photo files |

### Category endpoints (all implemented, none require auth)

| Route | Method | Purpose |
|---|---|---|
| `api/Category/get-all` | GET | List categories → `CategoryDto` |
| `api/Category/Get-By-Id/{Id}` | GET | Single category |
| `api/Category/Add-Category` | POST | Create category |
| `api/Category/Update-Category?Id=` | PUT | Update category (body `CategoryDto`; Id must match) |
| `api/Category/Delete-Category/{Id}` | DELETE | Delete category |

### DTOs

- `ProductDto` (Name, Description, NewPrice, OldPrice, CategoryName, Photos) — response.
- `PhotoDto` (ImageName, ProductId) — response, nested.
- `AddProductDto` (Name, Description, NewPrice, OldPrice, CategoryId, `IFormFileCollection Photos`) — request.
- `UpdateProductDto` (all nullable + optional `IFormFileCollection? Photos`) — request.
- `AddCategoryDto` / `CategoryDto`.

### Repository methods

`ProductRepository` (extends `GenericRepository<Product>`):
- `GetAllAsync(ProductParams)` — query incl. `Photos`, `Category`, **AsNoTracking**; search/filter/sort/paginate; sets `TotalCount`; projects to `ProductDto` via AutoMapper.
- `AddAsync(AddProductDto)` — maps to entity, saves photos through `IImageManagementService`.
- `UpdateAsync(int Id, UpdateProductDto)` — patch-like update (only non-null fields); on photo change deletes old files/rows and saves new ones.
- `DeleteAsync(Product)` — deletes photo files, photo rows, and the product.
- Inherited generic methods (`GetByIdAsync`, etc.).

`CategoryRepository` — inherits `GenericRepository<Category>` with no extra methods.

### Business logic — request → database flow

1. `ProductController.GetAll` → `work.ProductRepository.GetAllAsync(productParams)`.
2. Repository builds an IQueryable, applies:
   - **Search:** splits on spaces; product matches when **all** words appear in `Name` OR `Description` (case-insensitive).
   - **Filter:** `CategoryId` equality.
   - **Sort:** `"PriceAce"` → `OrderBy(NewPrice)`; `"PriceDce"` → `OrderByDescending(NewPrice)`; otherwise `OrderBy(Name)`.
   - **Pagination:** `PageSize` default **3**, max **6** (`ProductParams`); `TotalCount` set as side-effect.
3. Result projected to `ProductDto` and wrapped in `Pagination<ProductDto>`.

### Product photos

- Uploaded via `IImageManagementService.AddImageAsync(files, productName)` → saved to
  `wwwroot/Images/<ProductName>/<filename>`; DB stores the URL `/Images/<ProductName>/<file>` in `Photo.ImageName`.
- Deleted via `DeleteImage(imageName)` (uses `IFileProvider` to resolve the physical path).
- `IImageManagementService` is registered as **singleton**; `IFileProvider` as singleton rooted at `wwwroot`.

### Category relationships

`Category` 1—N `Product`; a product's `CategoryName` is flattened into `ProductDto` in the AutoMapper profile.

---

## 8. CUSTOMER BASKET

### Storage

- Redis via `StackExchange.Redis`. `IConnectionMultiplexer` singleton (connection string `redis`, default `localhost`).
- `CustomerBasket` / `BasketItem` are **plain POCOs**, serialized with `System.Text.Json`.

### Redis key structure

- Key = `basket.Id` (a client-supplied string, typically a GUID from the front-end).
- Value = JSON of `CustomerBasket`.
- TTL: `TimeSpan.FromDays(3)` set on update.

### Repository — `CustomerBasketRepository`

- Constructor takes `IConnectionMultiplexer` + `AppDbContext` (so it can refresh product data from SQL Server).
- `GetBasketAsync(string Id)` → `StringGetAsync(Id)` → deserialize JSON → `null` if missing.
- `UpdateBasketAsync(CustomerBasket)` → **validates and refreshes** then stores:
  1. Collects `item.Id` values.
  2. Loads matching products from SQL Server (incl. `Photos` and `Category`), keyed by product Id.
  3. If any requested product is missing (`ids.Count() != products.Count()`) → returns `null` (invalid basket).
  4. Overwrites each basket item with **fresh** product data: `ProductId`, `Name`, `Price` (= `NewPrice`),
     `Description`, `CategoryName`, `Image` (= first photo or `""`).
  5. `StringSetAsync(basket.Id, json, TimeSpan.FromDays(3))`.
- `DeleteBasketAsync(string Id)` → `KeyDeleteAsync(Id)`.

### Why product information is refreshed from SQL Server

- Basket only stores `ProductId` + client quantities. The authoritative price/name/image/category lives in SQL Server.
- Refreshing guarantees the basket shows current data and never lets the client dictate price, name, etc.

### Expiration policy

- 3 days from last update (set in `UpdateBasketAsync`). No explicit background cleanup — Redis TTL handles it.

### Full basket lifecycle

1. Client generates a basket Id (e.g. GUID) and stores it (front-end concern).
2. `GET api/Basket/{Id}` → returns stored basket or a fresh empty `CustomerBasket(Id)`.
3. `PUT api/Basket` (body `CustomerBasket`) → server validates items against SQL Server, refreshes fields, saves to Redis, returns the saved basket or `400`.
4. `DELETE api/Basket/{Id}` → removes the Redis key (also used by the order flow after checkout).

---

## 9. EMAIL SYSTEM

### `EmailDto` (`Ecom.Core.Dto`)

- Constructor `EmailDto(string to, string from, string subject, string content)`; properties `To`, `From`, `Subject`, `Content`.

### `IEmailService` / `EmailService`

- `EmailService` depends on `IConfiguration`.
- Builds a `MimeMessage` (`MimeKit`): sender "My Ecom" from `EmailSetting:From`; HTML body.
- Sends via `MailKit.Net.Smtp.SmtpClient`:
  - `ConnectAsync(EmailSetting:Smtp, int.Parse(EmailSetting:Port), useSsl: true)`.
  - `AuthenticateAsync(EmailSetting:Username, EmailSetting:Password)`.
- Config keys (see section 18): `EmailSetting:From`, `Smtp`, `Port`, `Username`, `Password`.

### Email templates

- `EmailStringBody.send(email, token, component, message)` (`Ecom.Core.Sharing`) generates an HTML body with a
  gradient-styled button linking to `http://localhost:4200/Account/{component}?email={email}&code={encodedToken}`.
  The token is URL-encoded (`Uri.EscapeDataString`).
- Used for: account activation (`Active`), password reset (`ResetPassword`).

### Email confirmation / reset flow

- `AuthRepository.SendEmail` builds `EmailDto` from configuration and delegates to `IEmailService`.
- See sections 6 (registration/login flows) for the end-to-end sequence.

---

## 10. REPOSITORY / UNIT OF WORK

### Interfaces

**`IGenericRepository<T>`** (all async unless noted):
- `Task<IReadOnlyList<T>> GetAllAsync()`
- `Task<IReadOnlyList<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)`
- `Task<T?> GetByIdAsync(int Id)`
- `Task<T?> GetByIdAsync(int Id, params Expression<Func<T, object>>[] includes)`
- `void Update(T entity)` — marks `Modified` (attaches if detached)
- `Task<bool> DeleteAsync(int Id)`
- `Task AddAsync(T entity)`
- `Task<int> CountAsync()`

**`IProductRepository : IGenericRepository<Product>`** — adds paginated/query `GetAllAsync(ProductParams)`,
`DeleteAsync(Product)`, `UpdateAsync(int, UpdateProductDto)`, `AddAsync(AddProductDto)`.
**`ICategoryRepository : IGenericRepository<Category>`** — empty marker.
**`ICustomerBasketRepository`** — `GetBasketAsync`, `UpdateBasketAsync`, `DeleteBasketAsync` (Redis).
**`IUnitOfWork`** — exposes `CategoryRepository`, `ProductRepository`, `CustomerBasketRepository`, `Auth`,
and `Task<int> SaveChangesAsync()`.

### `UnitOfWork` implementation

- Constructs each repository with the required dependencies (single `AppDbContext` shared everywhere).
- `SaveChangesAsync()` → `_context.SaveChangesAsync()`.
- `Auth` is exposed through the UoW: `new AuthRepository(_userManager, _emailService, _signInManager, _generateToken, _context, _configuration)`.

### Why repositories exist

- Encapsulate EF Core / Redis query details behind contracts (`Core` defines them, `infrastructure` implements).
- Centralize includes, AsNoTracking, mapping, and image handling.
- Enable the UnitOfWork to share one `DbContext` across a request.

### Responsibilities split

- **Repositories:** data access + data-shaping (querying, includes, pagination, projection, image file ops for products).
- **Services (`*Service` in `Repositires/Service/`):** cross-cutting/domain orchestration not tied to a single entity table —
  email sending, JWT generation, image files, and (in progress) order creation.
- **UnitOfWork:** composition of repositories + transaction boundary via `SaveChangesAsync`.

### Real usage examples

- `ProductController` calls `work.ProductRepository.GetAllAsync(productParams)` then `work.SaveChangesAsync()` after adds/updates/deletes.
- `BasketController` calls `work.CustomerBasketRepository.*`.
- `AccountController` calls `work.Auth.*`.
- `OrderService.CreateOrderAsync` (IN PROGRESS) calls `work.CustomerBasketRepository.GetBasketAsync(...)` and
  `work.CustomerBasketRepository.DeleteBasketAsync(...)`.

---

## 11. SERVICES

### `IEmailService` / `EmailService` — COMPLETED
- **Responsibility:** send HTML emails over SMTP.
- **Dependencies:** `IConfiguration`.
- **Why a service:** cross-cutting concern used by auth flows; not tied to an entity repository.
- **Methods:** `Task SendEmailAsync(EmailDto)`.

### `IGenerateToken` / `GenerateToken` — COMPLETED
- **Responsibility:** create JWT tokens for `AppUser`.
- **Dependencies:** `IConfiguration`.
- **Why a service:** token creation is a distinct, reusable concern consumed by `AuthRepository`.
- **Methods:** `string GetAndCreateToken(AppUser user)` (claims + HmacSha256 + 60 min expiry).

### `IImageManagementService` / `ImageManagementService` — COMPLETED
- **Responsibility:** persist/delete product images in `wwwroot/Images/<ProductName>/`.
- **Dependencies:** `IFileProvider` (singleton).
- **Why a service:** file IO is not a repository responsibility; shared by product add/update/delete.
- **Methods:** `Task<List<string>> AddImageAsync(IFormFileCollection files, string src)`, `void DeleteImage(string src)`.

### `IOrderService` / `OrderService` — **IN PROGRESS**
- **Responsibility:** orchestrate order creation from a basket.
- **Dependencies:** constructor takes concrete `UnitOfWork` (not `IUnitOfWork`), `AppDbContext`, `IMapper`.
- **Methods:**
  - `Task<Order> CreateOrderAsync(OrderDto orderDto, string email)` — implemented (see section 12).
  - `GetAllOrdersForUserAsync(string email)` — **empty body; causes the current compile error (CS0161)**.
- **Not registered in DI** — no `AddScoped<IOrderService, OrderService>()` exists. **No `OrderController` exists.**

---

## 12. ORDER MODULE — CURRENT / IN PROGRESS

> The Order module is **not finished**. The working tree contains uncommitted changes for it.

### What has already been implemented (source code confirms)

- Entities: `Order`, `OrderItem`, `DeliveryMethod`, `ShippingAddress`, enum `Status` (`Ecom.Core.Entities.Order`).
- DTOs: `OrderDto`, `ShippingAddressDto`, `OrderToReturnDto` (`Ecom.Core.DTO`, namespace with capitals).
- Interface: `IOrderService` (`CreateOrderAsync` only).
- `OrderService.CreateOrderAsync` implemented (uses basket from Redis, products from SQL, delivery method, shipping address, saves, deletes basket).
- EF configurations: `OrderConfiguration`, `DeliveryMethodConfiguration`.
- Migration `20260808222239_AddOrderEntity` (creates `DeliveryMethods`, `Orders`, `OrderItems`).
- AutoMapper profile: `ShippingAddressMapping` (`ShippingAddressDto ⇄ ShippingAddress`).

### What is NOT done / missing

- `OrderService` **not registered in DI**.
- **No `OrderController`** (no HTTP endpoints).
- `GetAllOrdersForUserAsync` is empty → **the solution currently does not compile**.
- `OrderToReturnDto` exists but is not used anywhere.
- No order retrieval endpoints, no basket-ownership validation, no stock checks, no payment integration.

### Existing `OrderService.CreateOrderAsync` logic (actual code flow)

```csharp
var basket = await work.CustomerBasketRepository.GetBasketAsync(orderDto.BasketId);

// ids from basket items
var products = await context.Products
    .Where(p => ids.Contains(p.Id))
    .ToDictionaryAsync(p => p.Id);

foreach (var item in basket.BasketItems)
{
    var product = products[item.ProductId];
    var orderItem = new OrderItem
    {
        ProductItemId = product.Id,
        ProductName = product.Name,
        Price = item.Price,
        MainImage = item.Image,
        Quantity = item.Quantity
    };
    orderItems.Add(orderItem);
}

var subTotal = orderItems.Sum(oi => oi.Price * oi.Quantity);
var deliveryMethod = await context.DeliveryMethods.FindAsync(orderDto.DeliveryMethodId);
var shippingAddress = mapper.Map<ShippingAddress>(orderDto.ShippingAddressDto);

var order = new Order(email, subTotal, deliveryMethod, orderItems, shippingAddress);

await context.Orders.AddAsync(order);
await context.SaveChangesAsync();
await work.CustomerBasketRepository.DeleteBasketAsync(orderDto.BasketId);
```

### Intended order flow (based on existing code & architecture)

```
Basket (Redis) ──► validate products (SQL) ──► create OrderItems
  ──► calculate SubTotal ──► select DeliveryMethod ──► snapshot ShippingAddress (owned)
  ──► create Order ──► save to SQL Server ──► clear Basket (Redis)
```

### PaymentIntent role

**There is no `PaymentIntent` in the code** (grep for `Stripe|Payment|PayPal|PaymentIntent` only matches the
`Status` enum). Payments are a **planned** feature (section 13).

### Recommended next steps for a future AI (do not implement without instruction)

1. Register `IOrderService`/`OrderService` in DI and fix `GetAllOrdersForUserAsync`.
2. Add an `OrderController` (create order, get orders for user, get order by id) following the existing
   controller conventions (`BaseController`, `IUnitOfWork`, `ResponseAPI`, `Pagination`).
3. Map `Order → OrderToReturnDto` (AutoMapper profile needed; none exists).
4. Validate basket ownership / user association before creating an order.
5. Decide error handling for missing/invalid delivery method and missing products.

---

## 13. PAYMENT — PLANNED / UPCOMING

### Planned / Upcoming Feature

**Status: PLANNED — not implemented.**

- The only payment-related artifact in the code is the order `Status` enum:
  `Pending`, `PaymentReceived`, `PaymentFaild` (typo in source; preserved intentionally).
- **No** payment provider (e.g. Stripe), no payment service, no `PaymentIntent`, no payment endpoints,
  no webhook handlers, and no payment configuration exist (verified by search).

### Intended direction (based on existing architecture; NOT implemented)

- A payment service (`IPaymentService`-style) would sit in `Ecom.infrastructure` next to the other services,
  with its interface in `Ecom.Core/Services` — consistent with existing conventions.
- Order ↔ Payment relationship would be expressed by extending `Order` (e.g., a `PaymentIntentId`) and/or by
  evolving the `Status` enum lifecycle: `Pending → PaymentReceived` (success) or `PaymentFaild` (failure).
- Successful payment → update order status; failed payment → mark order failed.
- Webhook handling would need a controller endpoint (e.g., a `PaymentsController` or `WebhooksController`)
  following `BaseController` conventions.
- Baskets are already deleted at order creation, so a "clear basket on success" step is already handled.

**Do not describe any of the above as implemented.**

---

## 14. RATING / REVIEW — PLANNED / UPCOMING

### Planned / Upcoming Feature

**Status: PLANNED — not implemented.** No `Rating`/`Review` code exists (verified by search).

### Intended feature (conceptual, based on typical e-commerce design and the existing domain)

- A `Rating`/`Review` entity linked to `Product` (N reviews per product) and to `AppUser` (N reviews per user).
- Rating value (e.g., 1–5) and optional review text.
- Business rule (proposed): a user may only review products they actually purchased (would require joining
  `AppUser` → `Order` → `OrderItem.ProductItemId`).
- Aggregate: average product rating, computed on retrieval.
- Future endpoints would follow existing conventions:
  - `POST api/Product/{productId}/rating` (authenticated).
  - `GET api/Product/{productId}/ratings`.
- Where it belongs: entity in `Ecom.Core/Entities` (possibly `Entities/Product/`), a repository/service in
  `Ecom.infrastructure`, DTOs in `Ecom.Core/Dto`, controller in `Ecom.API/Controllers`.

**Do not describe any of the above as implemented.**

---

## 15. API ENDPOINTS

Route convention: `[Route("api/[controller]")]` (from `BaseController`). Error route: `errors/{statusCode}`.

### IMPLEMENTED ENDPOINTS

#### AccountController (`api/Account`)

| Method | Route | Auth | Request DTO | Response | Purpose / behavior |
|---|---|---|---|---|---|
| POST | `Register` | No | `RegisterDto` | `200` / `400 ResponseAPI` | Register; sends activation email |
| POST | `Login` | No | `LoginDto` | `200 ResponseAPI` + sets `token` cookie / `400` | Login, writes HttpOnly JWT cookie |
| POST | `Active-Account` | No | `ActiveAccountDto` | `200` / `400` | Confirm email |
| POST | `Send-email-forget-password` | No | `email` (query) | `200` / `400` | Send password-reset email |
| POST | `Reset-password` | No | `PasswordDto` | `200` / `400` | Reset password |
| POST | `Logout` | No | — | `200 ResponseAPI` | Deletes `token` cookie |
| GET | `Get-user-name` | Yes (`[Authorize]`) | — | `200 ResponseAPI(User.Identity.Name)` | Return current user name |
| GET | `IsUserAuth` | No | — | `200` / `400` | Auth check |

#### BasketController (`api/Basket`)

| Method | Route | Auth | Request | Response | Purpose |
|---|---|---|---|---|---|
| GET | `{Id}` | No | — | `CustomerBasket` (or empty new basket) | Get basket from Redis |
| PUT | `` (root) | No | `CustomerBasket` | `CustomerBasket` / `400 ResponseAPI` | Validate + refresh + save basket in Redis |
| DELETE | `{Id}` | No | — | `200 ResponseAPI` / `404` | Delete basket key |

#### ProductController (`api/Product`)

| Method | Route | Auth | Request | Response | Purpose |
|---|---|---|---|---|---|
| GET | `` | No | `ProductParams` (query) | `Pagination<ProductDto>` | List with search/filter/sort/paging |
| GET | `{Id}` | No | — | `ProductDto` / `404 ResponseAPI` | Single product |
| POST | `` | No | `AddProductDto` (multipart) | `201 ProductDto` | Create product + photos |
| PUT | `{Id}` | No | `UpdateProductDto` | `200 ResponseAPI` / `404` | Patch update (optional photo replace) |
| DELETE | `{Id}` | No | — | `204` / `404` | Delete product + photos |

#### CategoryController (`api/Category`)

| Method | Route | Auth | Request | Response | Purpose |
|---|---|---|---|---|---|
| GET | `get-all` | No | — | `IReadOnlyList<CategoryDto>` | List categories |
| GET | `Get-By-Id/{Id}` | No | — | `CategoryDto` / `404` | Single category |
| POST | `Add-Category` | No | `AddCategoryDto` | `201 CategoryDto` | Create category |
| PUT | `Update-Category` | No | `CategoryDto` (+ `Id` query) | `200` / `400` / `404` | Update category |
| DELETE | `Delete-Category/{Id}` | No | — | `204` / `404` | Delete category |

#### Miscellaneous

| Method | Route | Controller | Purpose |
|---|---|---|---|
| GET | `api/Bug/not-found` | BugController | Debug: forced 404 |
| GET | `api/Bug/server-error` | BugController | Debug: forced 500 |
| GET | `api/Bug/bad-request/{Id}` | BugController | Debug |
| GET | `api/Bug/bad-request/` | BugController | Debug: 400 |
| GET | `errors/{statusCode}` | ErrorController | Status-code re-execute handler (currently always returns `ResponseAPI(404)`) |

### PLANNED / UPCOMING ENDPOINTS

- **Order endpoints** (module IN PROGRESS, none exist yet): e.g., create order (`POST api/Order`),
  get user's orders (`GET api/Order`), get order by id.
- **Payment endpoints** (PLANNED): e.g., payment intent creation, payment webhook.
- **Rating/Review endpoints** (PLANNED): e.g., submit rating, fetch product ratings/average.

---

## 16. DTOs

> Note: `DTO/ProductDto.cs` records are declared in the **global namespace** (no `namespace` block) — a known quirk.

### Request DTOs

| DTO | Namespace | Properties | Used by |
|---|---|---|---|
| `RegisterDto : LoginDto` | `Ecom.Core.Dto` | + `UserName`, `DisplayName` | `POST api/Account/Register` |
| `LoginDto` | `Ecom.Core.Dto` | `Email`, `Password` | `POST api/Account/Login` |
| `PasswordDto : LoginDto` | `Ecom.Core.Dto` | + `Token` | `POST api/Account/Reset-password` |
| `ActiveAccountDto` | `Ecom.Core.Dto` | `Email`, `Token` | `POST api/Account/Active-Account` |
| `AddProductDto` | *(global)* | `Name`, `Description`, `NewPrice`, `OldPrice`, `CategoryId`, `IFormFileCollection Photos` | `POST api/Product` |
| `UpdateProductDto` | *(global)* | all nullable + `IFormFileCollection? Photos` | `PUT api/Product/{Id}` |
| `AddCategoryDto` | `Ecom.Core.Dto` | `Name` (req, max 100), `Description?` (max 500) | `POST api/Category/Add-Category` |
| `OrderDto` (IN PROGRESS) | `Ecom.Core.DTO` | `BasketId`, `DeliveryMethodId`, `ShippingAddressDto` | (future) create order |
| `ShippingAddressDto` (IN PROGRESS) | `Ecom.Core.DTO` | `FirstName`, `LastName`, `City`, `ZipCode`, `Street`, `State` | mapped to owned `ShippingAddress` |

### Response DTOs

| DTO | Namespace | Properties | Used by |
|---|---|---|---|
| `ProductDto` | *(global)* | `Name`, `Description`, `NewPrice`, `OldPrice`, `CategoryName`, `IReadOnlyList<PhotoDto> Photos` | product endpoints |
| `PhotoDto` | *(global)* | `ImageName`, `ProductId` | nested in `ProductDto` |
| `CategoryDto` | `Ecom.Core.Dto` | `Id`, `Name`, `Description?` | category endpoints |
| `OrderToReturnDto` (IN PROGRESS) | `Ecom.Core.DTO` | `OrderDate`, `Status`, `DeliveryMethod`, `List<OrderItem> OrderItems`, `Total` | planned (unused) |
| `ResponseAPI` | `Ecom.API.Helper` | `statuscode`, `Message` | standardized API responses |
| `ApiExceptions : ResponseAPI` | `Ecom.API.Helper` | + `Details` | exception middleware output |
| `Pagination<T>` | `Ecom.API.Helper` | `PageNumber`, `PageSize`, `TotalCount`, `Data` | paged product list |
| `AuthResult` | `Ecom.Core.Sharing` | `Success`, `Message`, `Token`; static `Ok(...)`, `Fail(...)` | auth operations |

### Validation

- Data-annotation validation exists on DTOs (`[Required]`, `[MaxLength]`); model binding validation is implicit
  via `[ApiController]`.
- `ProductParams.PageSize` is capped at 6; `PageNumber` defaults to 1.
- Password rules enforced by `IdentityOptions` at registration/reset.

### Why DTOs instead of entities

- Avoid leaking EF navigations/owned types and internal columns to clients.
- Control the shape of responses (e.g., flattened `CategoryName`, nested `Photos`).
- Match the request contract (multipart, patch-style updates) to the domain.
- The `CategoryController` file even carries the comment: *"Never expose your Domain Entity to the outside world."*

---

## 17. DATA FLOW

### Registration
```
Client ──POST api/Account/Register──► AccountController ──► work.Auth.RegisterAsync (AuthRepository)
   ──► UserManager.CreateAsync(user, password) ──► Identity (SQL AspNetUsers)
   ──► GenerateEmailConfirmationTokenAsync ──► SendEmail ──► IEmailService (MailKit/SMTP) ──► User inbox
   ◄── AuthResult ──► Controller 200/400
```

### Login
```
Client ──POST api/Account/Login──► AccountController ──► work.Auth.Login (AuthRepository)
   ──► UserManager.FindByEmailAsync ──► EmailConfirmed check (resend activation if false)
   ──► SignInManager.CheckPasswordSignInAsync ──► IGenerateToken.GetAndCreateToken (JWT, 60 min)
   ◄── AuthResult(Token) ──► Controller sets HttpOnly cookie "token"
```

### Basket
```
Client ──PUT api/Basket──► BasketController ──► work.CustomerBasketRepository.UpdateBasketAsync
   ──► AppDbContext (validate + refresh product info from SQL Server)
   ──► Redis StringSetAsync(key=basket.Id, json, TTL=3 days) ──► stored basket
```

### Order (IN PROGRESS — service exists, no controller yet)
```
(Client ── future POST api/Order──► OrderController ──►) OrderService.CreateOrderAsync(orderDto, email)
   ──► work.CustomerBasketRepository.GetBasketAsync(BasketId)  (Redis)
   ──► context.Products (validate products) ──► build List<OrderItem>
   ──► SubTotal = Σ(Price × Quantity) ──► context.DeliveryMethods.FindAsync(DeliveryMethodId)
   ──► mapper.Map<ShippingAddress>(ShippingAddressDto) ──► new Order(email, ...)
   ──► context.Orders.AddAsync ──► context.SaveChangesAsync (SQL Server)
   ──► work.CustomerBasketRepository.DeleteBasketAsync(BasketId) (Redis)
```

### Future Payment (PLANNED — no code)
```
Client ──► Order/Payment Service ──► Payment Provider (e.g., Stripe) ──► PaymentIntent
   ──► success/failure ──► update Order.Status (PaymentReceived / PaymentFaild) ──► Order in SQL Server
```

---

## 18. CONFIGURATION

File: `Ecom.API/appsettings.json` (and `appsettings.Development.json`).

> Real secret values are NOT reproduced here. Replacements: `<SECRET>`, `<PASSWORD>`, `<CONNECTION_STRING>`.

```jsonc
{
  "Logging": { /* default */ },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "EcomDatabase": "<CONNECTION_STRING>",   // SQL Server (FirstEcom db, local SQLEXPRESS)
    "redis": "localhost"                      // Redis host
  },
  "EmailSetting": {
    "Port": 465,
    "From": "<EMAIL>",
    "Username": "<EMAIL>",
    "Password": "<PASSWORD>",                 // SMTP app password
    "Smtp": "smtp.gmail.com"
  },
  "Token": {
    "Secret": "<SECRET>",                     // JWT signing key
    "Issuer": "https://localhost:44344"
  },
  "CookieSettings": {
    "Domain": "localhost"                     // cookie domain
  }
}
```

Notes:
- `EmailSetting.Port` 465 = implicit TLS; the SMTP client connects with `useSsl: true`.
- `Token:Secret` is read both by token generation (`GenerateToken`) and validation (`infrastructureRegisteration`).
- `CookieSettings:Domain` is used for the auth cookie `Domain` property.
- **No payment configuration exists** (planned feature; `Payment`/`Stripe` keys absent).
- `appsettings.json` currently contains **real-looking credentials** (Gmail password, JWT secret). These are
  development values; future AI work must keep such values out of documentation and commits.

---

## 19. IMPORTANT DESIGN DECISIONS

| Decision | Reason (as determined from code) |
|---|---|
| Redis for baskets | Baskets are volatile, high-frequency, session-like data; `CustomerBasket` is a POCO serialized with `System.Text.Json`; 3-day TTL with no SQL table. |
| SQL Server for persistent data | Products, categories, users, orders, photos need durable, relational storage (EF Core 8.0.7, SqlServer provider, migrations). |
| ASP.NET Core Identity | Full-featured user management: hashing, lockout, email confirmation, token providers, default `AspNet*` tables. |
| JWT in an HttpOnly cookie | Combines token auth with browser-friendly transport; `OnMessageReceived` reads `token` cookie; `HttpOnly`/`Secure`/`SameSite=None`; cookie deleted on logout. |
| Email confirmation required before login | Enforced in `AuthRepository.Login` (`!EmailConfirmed` → re-send activation, refuse login). |
| Repository pattern + generic base | `IGenericRepository<T>` centralizes CRUD; concrete repositories add product/basket specifics; `AsNoTracking` on reads. |
| UnitOfWork | Shares a single `AppDbContext` per request across repositories; `SaveChangesAsync` is the commit point called by controllers. |
| AutoMapper | Profiles in `Ecom.API/Mapping` flatten domain → DTO (e.g., `Category.Name` → `ProductDto.CategoryName`, owned `ShippingAddressDto ⇄ ShippingAddress`). |
| Owned `ShippingAddress` | Snapshots the address at order time inside the `Orders` table (immutable after order creation). |
| `Status` enum stored as string | Human-readable DB values; converter `s.ToString()/Enum.Parse`; max length 20. |
| Indexes | `BuyerEmail` and `(BuyerEmail, OrderDate)` for order history queries; unique `DeliveryMethods.Name`; unique `Addresses.AppUserId` for 1:1. |
| DTO separation | Prevent domain exposure ("Never expose your Domain Entity to the outside world" — code comment in `CategoryController`). |
| Basket product refresh from SQL | Server authoritatively rewrites name/price/image/category on every basket update, preventing client-side tampering and stale prices. |
| Rate limiting + security headers in `ExceptionMiddleware` | In-memory `IMemoryCache` rate limit (80 req / 30 s per IP → `429`) and `X-Content-Type-Options`/`X-XSS-Protection`/`X-Frame-Options` headers. |
| `Pagination<T>` + `ProductParams` | Consistent paging envelope; `PageSize` default 3, max 6; `TotalCount` populated as a side effect by the repository. |
| Image management service | Decouples file IO (`wwwroot/Images/<ProductName>/`) from repositories; singleton `IFileProvider`. |

---

## 20. CURRENT DEVELOPMENT STATE

### COMPLETED (confirmed by source)

- Solution scaffolding (3 projects, dependency direction API → infrastructure → Core).
- Identity / users (`AppUser`, `AspNet*` tables).
- Authentication / authorization (register, login, email confirmation, JWT + cookie, logout, reset password, `[Authorize]`).
- Email service (MailKit/MimeKit) + HTML template helper.
- Products & categories (CRUD, photos, pagination, search, filter, sort).
- Customer basket on Redis (get/update/delete, product refresh, 3-day TTL).
- Generic repository, product/category/basket repositories, `IUnitOfWork`.
- Exception middleware (handling + rate limiting + security headers), `ResponseAPI`/`ApiExceptions`/`Pagination` helpers.
- AutoMapper profiles (product, category, shipping-address).
- EF Core schema + migrations up to `Add_Identity_Tables` (products, categories, photos, addresses, Identity tables).

### IN PROGRESS

- **Order module** — entities/DTOs/config/migration drafted; `OrderService.CreateOrderAsync` written;
  **missing:** DI registration, `OrderController`, `GetAllOrdersForUserAsync` body (causes compile error),
  order return mapping. The solution currently **does not build**.

### PLANNED (no implementation in source)

- **Payments** — only `Status` enum hints (`Pending`, `PaymentReceived`, `PaymentFaild`).
- **Ratings / Reviews** — nothing in source.
- (Possible) role-based authorization, order retrieval endpoints, webhooks — not implemented.

---

## 21. FUTURE DEVELOPMENT DIRECTION

The roadmap implied by commit history and code:

```
E-Commerce API
    └─► Authentication / Identity            (done)
        └─► Products / Categories             (done)
            └─► Basket / Redis                (done)
                └─► Orders                    (IN PROGRESS)
                    └─► Payments              (planned)
                        └─► Ratings / Reviews (planned)
```

Expected near-term work (consistent with the current architecture):
1. **Finish the Order module**: register `IOrderService`, implement `GetAllOrdersForUserAsync`, add
   `OrderController` (create + list + detail), add `Order → OrderToReturnDto` mapping, wire into DI.
2. **Payments**: add a payment service + provider (e.g., Stripe), `PaymentIntent` on `Order`, extend `Status`
   lifecycle, webhook endpoint.
3. **Ratings/Reviews**: `Rating` entity on `Product`/`AppUser`, average rating, endpoints, purchase-verified reviews.

Possible future extensions *supported by existing architecture* (not currently present): role-based
authorization, order detail/status endpoints, customer order history, email notifications for orders,
pagination reuse, centralized service layer for domain orchestration (currently only `OrderService` exists as a service).

---

## 22. INSTRUCTIONS FOR FUTURE AI ASSISTANTS

- **Treat this document as project context / source of truth.** It was generated from the actual source code.
- **Do not assume unfinished modules are implemented.** Orders are IN PROGRESS (and currently break the build);
  Payments and Ratings are PLANNED and have no code.
- **Before suggesting code, understand the existing architecture**: Core (entities/DTOs/interfaces) →
  Infrastructure (EF/repositories/services/DI) → API (controllers/middleware/mappings).
- **Preserve existing naming and architectural conventions** unless there is a strong reason to change them.
  This includes: repository pattern + UnitOfWork, `BaseController` with `IUnitOfWork`+`IMapper`, `ResponseAPI`
  responses, `Pagination<T>`, AutoMapper profiles in `Ecom.API/Mapping`, DI registration in
  `infrastructureRegisteration.cs`, EF configurations in `Ecom.infrastructure/Data/Config`.
- **Do not introduce unnecessary patterns** (no CQRS/MediatR unless asked; no new layers without justification).
- **Prefer consistency with the existing project.** Follow the current (sometimes imperfect) conventions
  (e.g., `interfaces` lowercase namespace, `Repositires` folder spelling, `Dto` vs `DTO` namespaces) or flag
  the inconsistency before changing it.
- **Clearly distinguish between existing code and proposed code.** Mark suggestions as NEW/PROPOSED.
- **When modifying an existing module, inspect related entities, interfaces, repositories, services, DTOs,
  configurations, migrations, and controllers first** (they are interdependent).
- **Do not expose secrets.** `appsettings.json` contains development credentials; never document or commit real
  values. Use placeholders.
- **When suggesting new features, explain where they belong in the existing architecture** (which project/folder,
  which interfaces, how it will be registered in DI).
- **Known gotchas:**
  - The solution currently fails to compile (`OrderService.GetAllOrdersForUserAsync` — CS0161). Fixing this is a prerequisite for any verification.
  - `OrderService` takes concrete `UnitOfWork`, not `IUnitOfWork`, and is not registered in DI.
  - `ProductDto`/`AddProductDto`/`UpdateProductDto` are in the **global namespace**.
  - `OrderDto`/`ShippingAddressDto`/`OrderToReturnDto` use namespace `Ecom.Core.DTO` (capital) unlike other DTOs.
  - `Status.PaymentFaild` contains a typo (`Faild`).
  - `ErrorController` always returns `ResponseAPI(404)` regardless of the incoming status code.
  - `ProductSeed.SeedAsync` exists but is **not called** from `Program.cs`.
  - No tests exist; verify changes by building (`dotnet build Ecom.API/Ecom.API.csproj`) and, if possible, running the API.
  - No role seeding exists; `IdentityRole` is registered but unused.

---

## QUICK PROJECT MAP

```
Client (web front-end)
        │  JSON / multipart / JWT-in-cookie
        ▼
API Controllers (Ecom.API)  ── BaseController: IUnitOfWork + IMapper
        │
        ▼
Services / UnitOfWork / Repositories
   ── IUnitOfWork (CategoryRepository, ProductRepository, CustomerBasketRepository, Auth)
   ── Services: IEmailService, IGenerateToken, IImageManagementService, IOrderService (IN PROGRESS)
   ── Repositories: GenericRepository<T>, ProductRepository, CategoryRepository, CustomerBasketRepository
        │
        ▼
EF Core (AppDbContext)  /  Redis (IConnectionMultiplexer)  /  External Services
   ── SQL Server (products, categories, photos, users/identity, addresses, orders)
   ── Redis (customer baskets, 3-day TTL)
   ── SMTP / MailKit (emails)   |   Payment Provider (PLANNED)   |   Image files (wwwroot/Images)
```

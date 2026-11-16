# E-Commerce Project Learnings & Troubleshooting Log

This document records technical issues, environment hurdles, root causes, and verified fixes encountered during the development of the E-Commerce learning application (.NET 10 Web API + PostgreSQL + Angular 21 Standalone).

---

## 1. Environment & Setup Issues

### 1.1 PowerShell Execution Policy Blocks `npm.ps1`
* **Symptom / Error**: Running `npm` in PowerShell fails with an execution policy error stating `npm.ps1 cannot be loaded because running scripts is disabled on this system`.
* **Cause**: Windows PowerShell restricts running unsigned `.ps1` script wrappers by default under standard restricted execution policies.
* **Fix**: Use `cmd /c` to run npm commands or call the Windows command batch script directly:
  ```powershell
  cmd /c "npm --prefix frontend run build"
  # or
  npm.cmd install --legacy-peer-deps
  ```

### 1.2 Windows Command Setup: `ng` CLI Not on System PATH
* **Symptom / Error**: Running `ng` produces `'ng' is not recognized as an internal or external command`.
* **Cause**: Global npm bin folder was not added to the user's system `PATH` environment variable.
* **Fix**: Use the local project CLI via `npx ng`, invoke via npm scripts (`npm run build`), or call the local `frontend/node_modules/.bin/ng.cmd`.

### 1.3 Windows Security Blocks Project Files in `Documents` Folder
* **Symptom / Error**: Access denied errors or file lock warnings when restoring or running .NET or Node tools inside user profile folders (such as `C:\Users\<Name>\Documents`).
* **Cause**: Windows Controlled Folder Access (Ransomware Protection) often restricts developer tools, background compilers, and package managers from writing into `Documents` or `Desktop`.
* **Fix**: Relocated the workspace to a dedicated developer root directory: `C:\dev\e-commerce`.

### 1.4 Permission Denied Running .NET Native Host Executable
* **Symptom / Error**: Direct invocation of `backend.exe` fails or is blocked by system policies.
* **Cause**: Execution restrictions on newly compiled native binary wrappers in certain Windows configurations.
* **Fix**: Execute the compiled assembly with the .NET runtime directly:
  ```powershell
  dotnet C:\dev\e-commerce\backend\bin\Debug\net10.0\backend.dll
  ```

### 1.5 Git Not on PATH in Fresh PowerShell Sessions
* **Symptom / Error**: Running `git` fails with `'git' is not recognized as the name of a cmdlet...`.
* **Cause**: Git was installed via winget to `%LOCALAPPDATA%\Programs\Git\cmd`, which was not refreshed into active PowerShell sessions without restarting or updating `$env:Path`.
* **Fix**: Add Git's command path to the session environment:
  ```powershell
  $env:Path += ";$env:LOCALAPPDATA\Programs\Git\cmd"
  ```

### 1.6 File Lock Error During `dotnet build` (MSB3027 / MSB3021)
* **Symptom / Error**:
  ```
  error MSB3027: Could not copy "obj\Debug\net10.0\backend.dll" to "bin\Debug\net10.0\backend.dll". Exceeded retry count of 10. Failed. The file is locked by: ".NET Host (<PID>)"
  ```
* **Cause**: A running background instance of the API application holds an open file handle on `backend.dll`.
* **Fix**: Terminate the active dotnet process before building:
  ```powershell
  Stop-Process -Id <PID> -Force
  # or find and kill dotnet processes:
  Get-Process -Name dotnet | Stop-Process -Force
  ```
  Alternatively, compile to a temporary output folder for testing: `dotnet build backend /p:OutDir=bin\TestBuild\`.

### 1.7 EF Core Migrations Command Execution Context
* **Symptom / Error**: `dotnet ef` fails if executed without context or fails to locate project files.
* **Cause**: EF Core commands must either be run inside the project directory or point explicitly to the target project using the `--project` flag.
* **Fix**: Always specify the project flag or navigate into the backend folder:
  ```powershell
  cd C:\dev\e-commerce
  dotnet ef migrations add <MigrationName> --project backend
  dotnet ef database update --project backend
  ```

### 1.8 npm Dependency Resolution (`edgesOut` Error)
* **Symptom / Error**: `npm install` failed with peer dependency resolution conflicts (`unable to resolve dependency tree`).
* **Cause**: Peer dependency mismatches between cutting-edge packages.
* **Fix**: Run npm install with legacy peer dependency resolution:
  ```powershell
  npm.cmd install --legacy-peer-deps
  ```

### 1.9 Stale Background Dev Server Serving Outdated Build on Port 4200
* **Symptom / Error**: After implementing new templates, components, and routing (such as auth links and navigation), the browser continues to display old pre-auth pages without the new links.
* **Cause**: An earlier `ng serve` process started hours prior remained running in the background holding port 4200, serving stale cached pre-rendered pages instead of current code.
* **Fix**: Inspect listening ports with `netstat -ano | findstr 4200` or `Get-NetTCPConnection -LocalPort 4200`, terminate the stale process (`Stop-Process -Id <PID> -Force`), and restart the dev server with `npm start` (or `cmd /c "npm --prefix frontend start"`).

---

## 2. Application & Architecture Issues

### 2.1 Angular Zoneless UI Rendering Delay (Button Clicks Needed Twice)
* **Symptom / Error**: In both Seller and Buyer pages, changes (such as saving an edited product, filtering products, or placing an order) only showed up after clicking a button a second time or refreshing the page.
* **Cause**: The application uses Angular 21 without `zone.js` (`provideClientHydration(withEventReplay())`, zoneless reactivity). In zoneless mode, Angular does not automatically trigger change detection when asynchronous `HttpClient` callbacks (RxJS `subscribe` callbacks) complete.
* **Fix**: Injected `ChangeDetectorRef` into the component and invoked `cdr.markForCheck()` inside all asynchronous callbacks (`next` and `error`):
  ```typescript
  private cdr = inject(ChangeDetectorRef);

  this.productService.getProducts().subscribe({
    next: (products) => {
      this.products = products;
      this.cdr.markForCheck(); // Notifies Angular to render the update
    },
    error: () => {
      this.errorMessage = 'Could not load products.';
      this.cdr.markForCheck();
    }
  });
  ```

### 2.2 Angular Service Duplicate Generation Conflict
* **Symptom / Error**: Attempting to generate a new service caused file overwrite conflicts or duplicate service files with differing names (`product.service.ts` vs `product.ts`).
* **Cause**: Generating files blindly without inspecting existing project layout.
* **Fix**: Always inspect existing services and models before generating, and extend or import existing classes and interfaces (`product.ts`).

### 2.3 `cancelEdit()` Overwriting Success Message in Seller Component
* **Symptom / Error**: After successfully updating a product, the seller success message ("Product updated successfully.") disappeared immediately.
* **Cause**: `cancelEdit()` resets `this.message = ''`. Calling `this.cancelEdit()` *after* setting `this.message = 'Product updated successfully.'` wiped out the feedback.
* **Fix**: Call `this.cancelEdit(fileInput)` *first*, then set `this.message = 'Product updated successfully.'`.

### 2.4 Overwriting Zero Stock (Quantity Validation Logic)
* **Symptom / Error**: When setting a product stock quantity to 0, the backend was defaulting it back to 10.
* **Cause**: Code used a fallback expression: `Quantity = dto.Quantity > 0 ? dto.Quantity : 10`, which treated a valid quantity of `0` (out-of-stock) as missing or invalid.
* **Fix**: Preserve valid zero values by assigning `Quantity = dto.Quantity` and validating ranges explicitly (`[Range(0, 100000)]`).

### 2.5 Angular SSR Safe Token Storage
* **Symptom / Error**: Direct usage of `window.localStorage` throws a `ReferenceError: localStorage is not defined` during Angular Server-Side Rendering (SSR prerender bundle generation).
* **Cause**: Node.js server prerendering environments do not possess browser globals like `localStorage` or `window`.
* **Fix**: Inject `PLATFORM_ID` and guard `localStorage` access using `isPlatformBrowser(this.platformId)`:
  ```typescript
  private platformId = inject(PLATFORM_ID);

  getToken(): string | null {
    if (!isPlatformBrowser(this.platformId)) {
      return null;
    }
    return localStorage.getItem('auth_token');
  }
  ```

### 2.6 Secure Password Hashing & Secret Management (JWT Auth)
* **Symptom / Error**: Storing plain-text passwords or committing JWT secret keys directly into source code introduces critical security vulnerabilities.
* **Fix**:
  1. Utilized ASP.NET Core's standard `PasswordHasher<User>` (PBKDF2 with HMAC-SHA256/SHA512) for salting, hashing, and verifying user passwords.
  2. Stored the JWT secret securely in .NET Secret Manager (`dotnet user-secrets set "JwtSettings:Secret" "<secret>"`), keeping sensitive credentials out of `appsettings.json` and version control.
  3. Added role claims to the JWT payload (`ClaimTypes.Role`) so backend `[Authorize(Roles = "Seller")]` and `[Authorize(Roles = "Buyer")]` attributes enforce role authorization deterministically.

### 2.7 Resource Ownership & Safe Migration for Legacy Data
* **Symptom / Error**: Products created prior to authentication had no seller association. Introducing a non-nullable foreign key `SellerId` would break existing records and fail database migrations if no default seller existed. Additionally, relying solely on `[Authorize(Roles = "Seller")]` allowed horizontal privilege escalation (any authenticated seller could modify any other seller's products).
* **Fix**:
  1. **Safe Migration**: Defined `SellerId` as a nullable `int?` with a database index and foreign key (`OnDelete: SetNull`). Existing records remain intact with `NULL` seller IDs and continue appearing in the public buyer catalog without data loss or foreign key errors.
  2. **JWT-Based Ownership Assignment**: In `AddProduct`, retrieved the seller's user ID directly from validated JWT claims (`sub` / `NameIdentifier`) and assigned `product.SellerId = sellerId`. Browser inputs for owner IDs are strictly ignored.
  3. **Ownership Verification on Edits**: In `UpdateProduct`, compared the authenticated seller ID against `product.SellerId`. If the IDs do not match (or if the product has no assigned seller), the API rejects the edit with `403 Forbidden`.
  4. **Scoped Seller Queries**: Added `GET /api/products/seller` to return only products belonging to the logged-in seller, while keeping public endpoints (`GET /api/products`) unpartitioned for buyers.

### 2.8 Product Reviews Architecture: Identity Isolation & Uniqueness Constraints
* **Symptom / Error**: Product reviews required capturing buyer identity without allowing client-side impersonation (spoofing `buyerId`), enforcing single-review policies, and safeguarding seller privacy so sellers cannot snoop on reviews of competing sellers' products via backend endpoints. Additionally, orders did not record buyer IDs, meaning reviews cannot claim verified purchaser status.
* **Fix**:
  1. **Server-Side Buyer Extraction**: Extracted `buyerId` exclusively from the validated JWT claims (`sub` / `NameIdentifier`) in `POST /api/reviews`. The request DTO only takes `productId`, `rating` (1-5), and `comment`.
  2. **Database-Level Duplicate Prevention**: Added a composite unique index `IX_Reviews_ProductId_BuyerId` on `[ProductId, BuyerId]` in PostgreSQL, paired with an API pre-check returning `409 Conflict` if the buyer already reviewed the product.
  3. **Seller Ownership Authorization on Reviews**: Implemented `GET /api/reviews/seller/{productId}` verifying that `product.SellerId == currentSellerId`, returning `403 Forbidden` if an unauthorized seller queries reviews for a product they do not own.
  4. **Public Catalog Access**: Kept `GET /api/reviews/product/{productId}` open for public browsing on buyer product cards.

### 2.9 Order History Architecture: JWT-Based Buyer Attribution, Seller Scoping, and Safe Migration
* **Symptom / Requirements**: Historical orders lacked buyer attribution (`Order` entity only tracked `ProductId`, `Quantity`, `UnitPrice`, `TotalPrice`, and `OrderDate`). Introducing order histories required attributing new orders to the logged-in buyer without client-side spoofing, enforcing seller scoping so sellers only see orders placed for their own products, protecting buyer privacy (no buyer email exposed to sellers), and preserving legacy orders without data loss.
* **Fix**:
  1. **Safe Migration for Legacy Orders**: Added `public int? BuyerId { get; set; }` and navigation `public User? Buyer { get; set; }` to `Order.cs`, configured in EF Core with `OnDelete: DeleteBehavior.SetNull`. Older orders persist with `NULL` `BuyerId` and remain intact in PostgreSQL.
  2. **Server-Side Buyer Attribution**: In `POST /api/orders`, extracted the buyer's user ID directly from validated JWT claims (`ClaimTypes.NameIdentifier` / `sub`). The client request body supplies only `productId` and `quantity`.
  3. **Privacy-Conscious History DTO**: Created `OrderHistoryDto` returning only transactional data (`Id`, `ProductId`, `ProductName`, `Quantity`, `UnitPrice`, `TotalPrice`, `OrderDate`). The buyer's email and user identity are intentionally omitted, ensuring sellers can review fulfillment data without accessing buyer personal data.
  4. **Scoped Query Endpoints**:
     - `GET /api/orders/buyer` (`[Authorize(Roles = "Buyer")]`): Filters orders strictly by `o.BuyerId == currentBuyerId`.
     - `GET /api/orders/seller` (`[Authorize(Roles = "Seller")]`): Joins on `Product` and filters by `o.Product.SellerId == currentSellerId`, preventing cross-seller order visibility.
  5. **Zoneless Angular Re-rendering**: Injected `ChangeDetectorRef` and invoked `markForCheck()` inside `loadMyOrders()` and `loadSellerOrders()` async subscriptions, ensuring newly placed orders and historical tables update immediately.



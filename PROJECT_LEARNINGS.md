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
* **Fix**: Always specify the project flag or project file path explicitly from the repository root:
  ```powershell
  cd C:\dev\e-commerce
  dotnet ef database update --project .\backend\backend.csproj
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
* **Fix**:
  1. During normal development, always stop a running Angular dev server by pressing `Ctrl + C` in its dedicated terminal window (allowing Node and the Angular CLI to release the port gracefully), rather than closing or killing all Node processes system-wide.
  2. If an orphaned process is holding port 4200, locate and terminate only that specific PID (`Get-NetTCPConnection -LocalPort 4200 | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }`), then restart cleanly with `npm.cmd start`.

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

### 2.10 Size-Specific Inventory Architecture: Variant Data Modeling, Safe Backfill Migration, and Stock Synchronization
* **Symptom / Requirements**: Products previously tracked only a single flat stock quantity. Introducing clothing sizes required managing independent inventory counts per size (e.g., S, M, L, XL, XXL, One Size), letting sellers add/edit size options, requiring buyers to select an available size before ordering, atomically reducing only that size's stock, and safely migrating legacy products and orders that had no size data without deleting or resetting the database.
* **Fix**:
  1. **Relational Variant Modeling (`ProductSize`)**: Created dedicated `ProductSize` entity (`Id`, `ProductId`, `Size`, `Quantity`) with composite unique index on `[ProductId, Size]` and cascade delete on `Product`. Added `[JsonIgnore]` on `ProductSize.Product` to prevent System.Text.Json circular reference cycles during API serialization.
  2. **Safe Migration & Backfill Strategy**: In migration `AddProductSizesAndOrderSize`, added `Size` column to `Orders` (nullable) and created `ProductSizes`. In the migration's `Up()` method, executed SQL backfilling existing product records with a default `'One Size'` entry mapped to their existing `Quantity`, and set existing orders' `Size` to `'One Size'`. Existing products and orders are preserved without data loss.
  3. **Atomic Stock Decrement & Aggregate Synchronization**: In `POST /api/orders`, validated size presence, existence on the product, and stock sufficiency inside a database transaction. Decremented the specific size's stock (`sizeOption.Quantity -= dto.Quantity`) and recalculated the aggregate product stock (`product.Quantity = product.Sizes.Sum(s => s.Quantity)`), keeping public catalog filters and out-of-stock badges consistent.
  4. **Multi-Size Seller Management & FormData Serialization**: Allowed sellers to configure sizes with quick presets and custom sizes. Serialized size structures via `sizesJson` alongside image uploads in `multipart/form-data`, deserialized and validated on the backend.
  5. **Interactive Buyer Selection & Stock Bounds**: Added interactive size chips and size dropdown selection on product cards, dynamically setting the maximum orderable quantity to the selected size's remaining stock and disabling checkout until a valid size is selected.

### 2.11 Responsive UI/UX Polish: Mobile Usability, CSS Grid Layouts & Style Budgets
* **Symptom / Requirements**: Earlier revisions of the buyer and seller pages relied on unclassed block elements and fixed-width cards (`max-width: 280px`), causing inconsistent card alignment on wide desktop monitors, awkward wrapping on mobile phones, and unstyled toolbars. In addition, growing component styles triggered Angular CLI production budget warnings (`anyComponentStyle exceeded maximum budget`).
* **Fix**:
  1. **Responsive CSS Grid Architecture**: Upgraded product listings to CSS Grid (`grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 1.5rem;`), with mobile media queries (`@media (max-width: 640px)`) adjusting to single-column flows for clean vertical scrolling.
  2. **Horizontal Table Scroll Wrappers**: Wrapped both Buyer and Seller order history tables in `.orders-table-wrapper` with `overflow-x: auto; min-width: 580px;`, preserving table column alignment and readability on small mobile screens without causing page-level layout overflow.
  3. **Touch Target Sizing & Interactive Feedback**: Increased button and chip padding to provide comfortable touch targets on mobile devices. Styled interactive states (`:hover`, `:focus`, `:active`, and `:disabled`) with distinct visual cues.
  4. **Angular Style Budget Calibration (`angular.json`)**: Adjusted `anyComponentStyle` production budgets in `angular.json` (`maximumWarning: 15kB`, `maximumError: 30kB`) to cleanly accommodate self-contained responsive CSS rules without triggering build warnings.

### 2.12 Seller Area Redesign: Dashboard Navigation, Child Routes, and Inline SVG Architecture
* **Symptom / Requirements**: Previously, the Seller page placed the product creation form, full product catalog, reviews drawers, and order history into one long monolithic page. The user requested redesigning the Seller area into a clean Seller Dashboard (`/seller`) with three primary icon navigation options (Add Product, My Products, Orders for My Products) leading to dedicated seller views (`/seller/add-product`, `/seller/products`, `/seller/orders`), keeping all routes protected by the Seller auth guard, with clear navigation to return to the dashboard and visible active route indicators.
* **Fix**:
  1. **Hierarchical Child Route Architecture (`app.routes.ts`)**:
     - Configured `/seller` as a parent layout route loading `SellerLayout` with `canActivate: [sellerGuard]`.
     - Defined child routes for `''` (`SellerDashboard`), `'add-product'` (`SellerAddProduct`), `'products'` (`SellerProducts`), and `'orders'` (`SellerOrders`), each explicitly guarded by `sellerGuard` for complete route protection.
  2. **Seller Sub-Navigation & Active State**:
     - Built a responsive seller sub-navigation bar in `SellerLayout` using `routerLinkActive="active"` and `[routerLinkActiveOptions]="{ exact: true }"` for the Dashboard tab.
     - Provided a prominent `← Back to Dashboard` navigation link across all sub-views (`/seller/add-product`, `/seller/products`, `/seller/orders`), giving sellers two direct ways to return to the dashboard.
  3. **Accessible Inline SVG Icon Buttons**:
     - Standardized on lightweight, dependency-free inline SVG icons with `aria-hidden="true"`, stroke-based styling (`viewBox="0 0 24 24"`), and accompanying visible text labels for accessibility (WCAG AA compliant).
  4. **Seamless Edit Flow via Query Params**:
     - Updated the "✎ Edit" action on product cards in `/seller/products` to navigate to `/seller/add-product?edit={productId}`.
     - `SellerAddProduct` reads the query param, pre-populates product details and clothing size inventories, provides a "Cancel edit" option returning to `/seller/products`, and confirms updates with clear feedback.
  5. **Business Logic & Service Preservation**:
     - Reused `ProductService` methods (`getSellerProducts`, `createProduct`, `updateProduct`, `getSellerOrders`, `getSellerProductReviews`) across all child views, preserving all size inventory, reviews, and ordering functionality without code duplication.





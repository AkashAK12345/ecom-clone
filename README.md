# 🛍️ StyleStore - Full-Stack E-Commerce Clothing Platform

A modern, responsive e-commerce web application built with an **ASP.NET Core .NET 10 Web API**, **PostgreSQL with EF Core**, and an **Angular 21 Standalone** frontend.

Designed with complete buyer-seller role isolation, size-specific clothing inventory, customer reviews, and transactional order fulfillment.

---

## 🌟 Core Features

- **Clothing Catalog & Category Filtering**: Browse clothes across categories (`Men`, `Women`, `Kids`) with instant updates.
- **Size-Specific Inventory**: Track independent stock for clothing sizes (`XS`, `S`, `M`, `L`, `XL`, `XXL`, `One Size`). Stock is validated and deducted atomically per size.
- **Role-Based Authentication (JWT)**: Register and log in as either **Buyer** or **Seller**. Passwords are encrypted with PBKDF2 (`PasswordHasher<User>`).
- **Seller Product Ownership**: Sellers only see and edit their own products. Public browsing shows all sellers' products.
- **Product Reviews & Ratings**: Authenticated buyers can write 1-to-5 star reviews (one review per product per buyer).
- **Order Histories with Privacy Protection**:
  - Buyers view their personal order history.
  - Sellers view orders containing their products with size and fulfillment details, without exposing buyer emails or sensitive personal info.
- **Mobile & Desktop Responsive UI**: Clean card grids, interactive size chips, responsive data tables, and touch-friendly controls.

---

## 📋 Prerequisites

Before running the application, ensure the following are installed on your machine:

1. **.NET 10 SDK**:
   - Verify by running: `dotnet --version`
2. **Node.js (v20+ recommended) & npm**:
   - Verify by running: `node -v` and `npm -v`
3. **PostgreSQL Database (v14+)**:
   - Running locally on `localhost:5432`.
   - Default database: `EcommerceDb`.
4. **EF Core CLI tool** (optional, recommended):
   - Install globally: `dotnet tool install --global dotnet-ef`

---

## 🗄️ Database Setup & Migration

The project uses Entity Framework Core with Code-First migrations in PostgreSQL.

1. Ensure PostgreSQL is running on `localhost:5432` with credentials matching `backend/appsettings.json` (or configured via environment variables / user secrets):
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=EcommerceDb;Username=postgres;Password=postgres"
   }
   ```

2. Open a terminal in `C:\dev\e-commerce` and apply all pending database migrations:
   ```powershell
   cd C:\dev\e-commerce
   dotnet ef database update --project .\backend\backend.csproj
   ```
   > **Note**: This creates all required tables (`Products`, `ProductSizes`, `Orders`, `Users`, `Reviews`) and safely backfills legacy records with a default `'One Size'` variant.

---

## 🚀 Running the Application

Open **two separate terminal windows** to run the backend and frontend simultaneously. The backend terminal must stay open while using or demoing the app.

### Terminal 1: Backend (.NET 10 Web API)
From `C:\dev\e-commerce\backend`, build the project, set the environment variable `ASPNETCORE_ENVIRONMENT` to `Development`, and run the compiled assembly:

**PowerShell**:
```powershell
cd C:\dev\e-commerce\backend
dotnet build
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet .\bin\Debug\net10.0\backend.dll
```

**Command Prompt (cmd)**:
```cmd
cd C:\dev\e-commerce\backend
dotnet build
set ASPNETCORE_ENVIRONMENT=Development
dotnet .\bin\Debug\net10.0\backend.dll
```

- API Base URL: `http://localhost:5000`
- Swagger UI (Development): `http://localhost:5000/swagger`
- **Important**: Keep this terminal window open. Closing it will shut down the Web API.

---

### Terminal 2: Frontend (Angular 21)
Open a **separate terminal window** for the frontend and start the Angular development server using `npm.cmd` (which avoids Windows PowerShell `.ps1` execution policy restrictions):

```powershell
cd C:\dev\e-commerce\frontend
npm.cmd start
```

- When the compilation completes, open the URL printed by Angular in your browser (usually **`http://localhost:4200`**).
- **Stopping the dev server**: To stop the running Angular development server, press `Ctrl + C` in its own terminal window (and type `Y` if prompted). Do not kill all Node processes system-wide.

---

## 🎯 Mentor Demo Walkthrough

Follow these steps to demonstrate the full application workflow:

### 1. Register & Login as a Seller
1. Open `http://localhost:4200/register`.
2. Register an account with role **Seller** (e.g., `seller@example.com`, password `Password123!`).
3. You will be redirected to the **Seller portal** (`/seller`).
4. In the **Add product** form:
   - Enter a Name (e.g. `Denim Jacket`), Category (`Men`), and Price (`1299`).
   - In **Sizes & Stock Inventory**, use the quick-add buttons to set sizes:
     - `S` = 5
     - `M` = 10
     - `L` = 0 (out of stock)
   - Select an image and click **Add product**.
   - Observe the product card rendering the live stock breakdown for each size.

### 2. Browse & Order as a Buyer
1. Log out from the navigation bar.
2. Register or log in as a **Buyer** (e.g., `buyer@example.com`, password `Password123!`).
3. Navigate to **Buyer: browse products** (`/buyer`).
4. Locate the newly created product:
   - Notice the interactive size chips: `S (5)`, `M (10)`, and `L (Out of stock)`.
   - Size `L` is disabled.
   - Click chip `S`: the size dropdown updates and the quantity input caps at `5`.
   - Select quantity `2` and click **Place order**.
   - Notice the stock for size `S` immediately decreases from `5` to `3`.
   - Scroll down to **My orders** to see the order recorded with `Size: S`, `Qty: 2`, and total price calculated by the server.

### 3. Submit a Product Review
1. On the same product card, click **▼ Reviews**.
2. Select a rating (e.g., `5 - Excellent`), write a comment, and click **Submit Review**.
3. The review appears immediately in the review list, and duplicate submissions are prevented.

### 4. Verify Seller Fulfillment
1. Switch back to the **Seller** account.
2. Navigate to `/seller`.
3. Scroll to **Orders for my products**:
   - The order placed by the buyer appears with `Product: Denim Jacket`, `Size: S`, and `Qty: 2`.
   - The buyer's email and private details are withheld, protecting buyer privacy.

---

## 📁 Project Architecture

```
e-commerce/
├── backend/                  # ASP.NET Core .NET 10 Web API
│   ├── Controllers/          # REST API endpoints (Auth, Products, Orders, Reviews)
│   ├── Data/                 # AppDbContext & EF Core model mapping
│   ├── Models/               # Domain entities & request/response DTOs
│   ├── Migrations/           # EF Core database migrations
│   └── Program.cs            # App pipeline, JWT auth & CORS configuration
├── frontend/                 # Angular 21 Standalone App
│   ├── src/app/
│   │   ├── guards/           # Route guards for Buyer & Seller pages
│   │   ├── interceptors/     # JWT Bearer token HTTP interceptor
│   │   ├── pages/            # Buyer, Seller, Login, Register components
│   │   └── services/         # AuthService & ProductService
│   ├── src/styles.css        # Global CSS resets and typography
│   └── angular.json          # Angular CLI build & style budget settings
├── PROJECT_LEARNINGS.md      # Troubleshooting notes, errors & architectural decisions
└── README.md                 # Setup, configuration & demo guide
```


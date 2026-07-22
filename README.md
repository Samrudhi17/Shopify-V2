# QR Based Digital Shop Catalog Platform

Digital catalog platform for local businesses. Vendors register a shop, add
products/inventory, and get a unique **catalog URL** (`domain/shopname`) plus a
**QR code** customers scan to browse the shop on their phones.

## Tech stack

| Layer      | Technology                                    |
| ---------- | --------------------------------------------- |
| Frontend   | React (Vite), React Router, Axios             |
| Backend    | .NET 10 Web API, EF Core                       |
| Database   | SQL Server                                     |
| Auth       | Firebase Authentication                        |
| Storage    | Backend local storage (wwwroot/uploads, served as static URLs) |
| QR codes   | QRCoder (.NET)                                  |

## Project structure

```
QRBasedDigitalShop/
├── backend/
│   └── QRShop.API/          # .NET Web API
│       ├── Controllers/     # AuthController (register/me) + future controllers
│       ├── Models/Entities/ # EF entities mapping the ER diagram
│       ├── Data/            # AppDbContext
│       ├── DTOs/            # Request/response models
│       ├── Migrations/      # EF Core migrations
│       └── Program.cs
├── frontend/                # React (Vite) app
│   └── src/
│       ├── services/        # firebase.js, api.js (axios)
│       ├── context/         # AuthContext (user + role)
│       ├── components/      # ProtectedRoute
│       └── pages/
│           ├── public/      # Home (About/Services/Contact)
│           ├── auth/        # Login, Register, ForgotPassword
│           ├── admin/       # AdminDashboard
│           ├── vendor/      # Sidebar layout + Dashboard/Profile/Products/...
│           └── shop/        # ShopCatalog (public customer page)
├── database/
│   └── schema.sql           # Generated SQL Server schema
└── docs/
```

## Roles

- The **first** registered user becomes the **Admin** (stored in `Admins`).
- Every subsequent registration is a **Vendor** (stored in `Vendors`).
- Name/email/password live in **Firebase**; full profiles live in **SQL Server**.

## Deployment

Containerised with Docker. See **[DEPLOY.md](DEPLOY.md)** for the full Hetzner guide.

```bash
cp .env.example .env   # fill in secrets + Firebase config
docker compose up -d --build
```

nginx serves the React build and proxies `/api` and `/uploads` to the .NET
container, so everything runs on one origin (no CORS). SQL Server data and
uploaded files live in named volumes.

## Local development

### 1. Database (SQL Server)

SQL Server isn't installed locally yet. Options on macOS:

- Run SQL Server in Docker:
  ```bash
  docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Your_password123" \
    -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
  ```
  Then set the connection string in `backend/QRShop.API/appsettings.json`:
  ```
  Server=localhost,1433;Database=QRShopDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;
  ```

Apply the schema:
```bash
cd backend/QRShop.API
dotnet ef database update
```
(or run `database/schema.sql` directly against your server).

### 2. Backend

```bash
cd backend/QRShop.API
dotnet run
```

### 3. Frontend

```bash
cd frontend
cp .env.example .env       # fill in Firebase config + API URL
npm install
npm run dev                # http://localhost:5173
```

## Status

Scaffolding complete: solution structure, EF entities + migration, routing, auth
context, role-based routes, and page stubs. Next: implement remaining API
controllers (shops, products, inventory, admin), Firebase Storage uploads, QR
generation on shop creation, and JWT verification of Firebase tokens.

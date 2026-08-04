# 🛒 E-Commerce & Customer Portal Platform

A full-stack, production-ready e-commerce platform featuring an ASP.NET Core Web API backend, an Angular frontend, live customer support chat, Cloudflare R2 media storage, AI integration, and secure authentication.

---

## 🚀 Features

* **Authentication & Authorization:** Secure JWT and cookie-based authentication with role management (Admin/User) and OTP email verification.
* **Product Dashboard:** Complete CRUD functionality for product management, inventory tracking, and image uploads.
* **Live Support Chat:** Real-time messaging hub between customers and admin representatives powered by ASP.NET Core SignalR.
* **Cloud Media Storage:** Fast and scalable product image hosting powered by Cloudflare R2 object storage.
* **AI Integration:** Integrated Groq API (`llama-3.1-8b-instant`) for dynamic AI support and backend automation.
* **Modern Frontend:** Single Page Application (SPA) built with Angular and TypeScript.

---

## 🛠️ Tech Stack

### Backend (.NET)
* **Framework:** ASP.NET Core Web API (.NET 8+)
* **Database:** SQL Server / Entity Framework Core
* **Real-time Services:** SignalR
* **Storage Provider:** Cloudflare R2 (Amazon S3-compatible API)
* **Email & Security:** SMTP / Gmail App Passwords & OTP logic
* **AI Integration:** Groq API

### Frontend (Angular)
* **Framework:** Angular
* **Languages:** TypeScript, HTML5, CSS3/SCSS, Tailwind
* **HTTP Client:** Angular `HttpClient` & SignalR Client library

---

## 📁 Repository Structure

```text
Dotnet_test1_authentication_authorization_with_product/
│
├── angular-ecom-frontend/     # Angular Client Application (VS Code)
│   ├── src/                  # Components, Services, and Guards
│   ├── package.json
│   └── angular.json
│
├── Controllers/               # ASP.NET Core API Endpoints (Visual Studio)
├── Hubs/                      # SignalR Real-Time Chat Hubs
├── Services/                  # Core Business Logic & External API Services
├── Properties/                # Launch settings and environment configs
├── appsettings.Example.json   # Configuration template (No real secrets)
├── Program.cs                 # API Startup and Dependency Injection
├── .gitignore
└── README.md
⚙️ Getting Started
Prerequisites
.NET 8 SDK or higher

Node.js (v18 or higher) & npm

Angular CLI (npm install -g @angular/cli)

SQL Server Express / LocalDB

🔧 Installation & Setup
1. Clone the Repository
Bash
git clone [https://github.com/usmanrafiq319/Dotnet_test1_authentication_authorization_with_product.git](https://github.com/usmanrafiq319/Dotnet_test1_authentication_authorization_with_product.git)
cd Dotnet_test1_authentication_authorization_with_product
2. Backend Setup (.NET)
Open the backend project in Visual Studio or your preferred editor.

Initialize and configure local secrets using .NET User Secrets (so sensitive keys stay off Git):

Bash
dotnet user-secrets init
dotnet user-secrets set "AppSettings:Token" "YOUR_JWT_SECRET_KEY"
dotnet user-secrets set "EmailSettings:Password" "YOUR_GMAIL_APP_PASSWORD"
dotnet user-secrets set "R2Storage:AccessKeyId" "YOUR_R2_ACCESS_KEY"
dotnet user-secrets set "R2Storage:SecretAccessKey" "YOUR_R2_SECRET_KEY"
dotnet user-secrets set "Groq:ApiKey" "YOUR_GROQ_API_KEY"
Apply Entity Framework migrations to update your local database:

Bash
dotnet ef database update
Run the API (press F5 in Visual Studio or execute dotnet run).

3. Frontend Setup (Angular)
Navigate to the Angular project directory:

Bash
cd angular-ecom-frontend
Install dependencies:

Bash
npm install
Start the Angular dev server:

Bash
ng serve
Open your browser and go to http://localhost:4200.

🔒 Security & Privacy Note
No sensitive API credentials, access tokens, or private keys are stored in this repository. Local development relies on .NET User Secrets and .gitignore policies to ensure configurations remain private.
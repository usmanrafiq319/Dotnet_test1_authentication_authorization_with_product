# 🛒 E-Commerce & Customer Portal Platform

A modern, full-stack e-commerce platform featuring an ASP.NET Core Web API backend, an Angular single-page application styled with Tailwind CSS, live customer support chat, Cloudflare R2 media storage, AI integration, and secure JWT authentication.

---

## 🚀 Features

* **Authentication & Authorization:** Secure JWT and cookie-based authentication with role management (Admin/User), HMAC-SHA512 token signing, and OTP email verification.
* **Product Management:** Complete CRUD functionality for product management, inventory tracking, and high-performance image uploads.
* **Interactive API Documentation:** Integrated with **Scalar** for an ultra-modern, interactive API testing UI.
* **Live Support Chat:** Real-time messaging hub between customers and admin representatives powered by ASP.NET Core SignalR.
* **Cloud Media Storage:** Fast and scalable product image hosting powered by Cloudflare R2 via the AWS S3 SDK.
* **Email Service:** MailKit integration for secure SMTP delivery and OTP generation.
* **AI Integration:** Integrated Groq API (`llama-3.1-8b-instant`) for dynamic AI support and automation.
* **Modern UI:** Responsive, utility-first frontend built with Angular and Tailwind CSS.

---

## 🛠️ Tech Stack & Key Packages

### Backend (.NET Core Web API)
* **Framework:** ASP.NET Core Web API (.NET 10)
* **Database & ORM:** SQL Server / Entity Framework Core 10.0
* **API Documentation:** Scalar (`Scalar.AspNetCore 2.16.5`)
* **Security & Auth:** `Microsoft.AspNetCore.Authentication.JwtBearer` & `System.IdentityModel.Tokens.Jwt`
* **Cloud Storage:** AWS SDK S3 (`AWSSDK.S3` connected to Cloudflare R2)
* **Email Engine:** MailKit (`MailKit 4.17.0`)
* **Real-time Services:** ASP.NET Core SignalR
* **AI Provider:** Groq API

### Frontend (Angular)
* **Framework:** Angular
* **Styling:** Tailwind CSS
* **Languages:** TypeScript, HTML5, SCSS/CSS
* **HTTP & Real-Time:** Angular `HttpClient` & `@microsoft/signalr`

---

## 📁 Repository Structure

```text
Dotnet_test1_authentication_authorization_with_product/
├── Controllers/               # ASP.NET Core API Endpoints
├── Hubs/                      # SignalR Real-Time Chat Hubs
├── Services/                  # Core Business Logic (MailKit, R2 Storage, Groq)
├── Properties/                # Launch settings and environment configs
├── appsettings.Example.json   # Configuration template (No real secrets)
├── Program.cs                 # API Startup and Dependency Injection
├── .gitignore
└── README.md
⚙️ Getting Started
Prerequisites
.NET 10 SDK (or .NET 8+)

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

Open your browser to view the Scalar API Docs at /scalar/v1.

🔒 Security & Privacy Note
No sensitive API credentials, access tokens, or private keys are stored in this repository. Local development relies on .NET User Secrets and .gitignore policies to ensure configurations remain private.


***

### Push the updated README to GitHub:

Open your terminal and run:

```powershell
git add README.md
git commit -m "Update README with Tailwind, Scalar, MailKit, and .NET packages"
git push origin master

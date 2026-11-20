# 💰 Martin SpendWise

**Martin SpendWise** is a full-stack personal finance, budgeting and grocery management application developed using .NET MAUI, ASP.NET Core Web API, ASP.NET Core MVC and MySQL.

The application helps users manage monthly budgets, record purchases, monitor spending, organise grocery lists and make informed financial decisions.

The current system focuses on personal budgeting, purchase tracking, spending monitoring and grocery management, with future plans to introduce grocery price comparison across major South African retailers.

---

## 🚀 Project Overview

Martin SpendWise combines financial tracking and grocery planning in one application.

The solution consists of:

- **.NET MAUI application** – Customer-facing application.
- **ASP.NET Core Web API** – Backend services and business logic.
- **ASP.NET Core MVC Admin Portal** – Administrator interface hosted within the API project.
- **MySQL** – Persistent data storage.
- **Entity Framework Core** – Database operations and migrations.
- **JWT authentication** – Customer authentication and protected API requests.
- **Cookie-based authentication** – Separate administrator authentication.

The application follows a client-server architecture, with the frontend communicating with the backend through REST API endpoints.

The Windows desktop application is the current development and testing target.

---

## ✨ Current Features

### 🔐 Customer Authentication and Account Management

- Customer registration.
- Secure login through the authentication API.
- JWT-based authentication.
- Password hashing.
- Email-based password recovery.
- Password reset code verification.
- Password reset token validation.
- Token versioning for invalidating existing sessions after password changes.
- Authenticated access to protected API endpoints.
- Authentication information stored by the application.

**Latest frontend improvements:**

- Redesigned login interface.
- SpendWise branded S emblem.
- Email and password input fields.
- Show and hide password functionality.
- Forgot Password navigation.
- Create Account navigation.
- Login validation and error messages.
- Personalised welcome modal following successful authentication.
- Continue button to enter the customer dashboard.

### 💵 Budget Management

- Create monthly budgets.
- Manage budget categories.
- Allocate funds to spending categories.
- Monitor available funds.
- Track spending against budgets.
- View remaining budget balances.
- Monitor budget utilisation.
- Receive notifications when spending reaches configured thresholds.

### 💳 Transaction Management

- Record financial transactions.
- Record purchases and expenses.
- Categorise spending.
- View transaction history.
- Monitor financial activity.
- View spending summaries.
- Filter spending history.
- Review purchase descriptions, amounts and dates.

### 🧾 Purchase Recording

- Record purchases against monthly budgets.
- Select the relevant spending category.
- Enter purchase amounts and descriptions.
- Monitor how purchases affect category spending.
- View recorded purchases in Spending History.
- Receive budget usage notifications.

### 🛒 Grocery Management

- Create grocery lists.
- Add grocery products.
- Manage grocery list items.
- Specify grocery quantities.
- Estimate grocery expenses.
- Organise grocery purchases.
- Monitor grocery-related spending.

### 📊 Financial Summaries and Insights

- Spending summaries.
- Budget utilisation information.
- Category-based spending.
- Transaction counts.
- Average purchase amounts.
- Financial activity overviews.

---

## 🖥️ Customer Application – Frontend Development

The customer-facing application is developed using .NET MAUI and XAML.

The frontend is currently undergoing improvements to its visual design, navigation, maintainability and integration with the backend.

### Login Interface

The latest LoginPage design includes:

- Dark green SpendWise branding.
- Overlapping circular S emblem.
- Email and password fields.
- Show/Hide password functionality.
- Forgot Password navigation.
- Create Account navigation.
- Login button with loading state.
- Login error messages.
- Personalised welcome modal.
- Emerald checkmark badge.
- Continue button for dashboard navigation.

The login process continues to use the existing authentication API rather than a simulated login.

### Dashboard Interface

The DashboardPage is being aligned with the SpendWise visual identity.

Current interface elements include:

- Dark green dashboard header.
- Personalised welcome area.
- Notification bell and unread notification badge.
- Profile navigation.
- Bank Account information card.
- Monthly Budget card.
- Quick Actions section.
- My Grocery Lists shortcut.
- Recent Spending section.
- Secure Banking information.

**Dashboard integration work includes:**

- Retrieving monthly budget information through the existing API.
- Displaying total spending.
- Displaying remaining budget.
- Showing budget utilisation.
- Displaying recent purchase transactions dynamically.
- Retrieving unread notification counts.
- Navigating to the relevant application pages.

**Bank integration status:** Not connected. The dashboard does not claim to display a real bank balance or provide live banking services.

### Frontend Maintainability

The customer-facing application is also undergoing a frontend review to improve maintainability and reduce unnecessary duplication.

Current work includes:

- Identifying repeated interface elements across pages.
- Reviewing duplicated XAML layouts and styling.
- Identifying repeated frontend logic.
- Reviewing navigation consistency.
- Reducing unnecessary or redundant code.
- Improving reusable UI components.
- Consolidating repeated interface patterns where appropriate.
- Improving consistency in buttons, forms, cards and page layouts.
- Improving loading indicators and error messages.
- Reviewing page structure to make future maintenance easier.

**Current status: In progress.**

The objective is to make the frontend easier to maintain and extend without unnecessarily changing existing application functionality.

---

## 🚧 Current Development Progress

### 1. SpendWise Administration Portal

The SpendWise Administration Portal is developed inside the existing `SpendWise.API` project using ASP.NET Core MVC.

The portal shares the existing backend and database infrastructure while maintaining separate administrator authentication.

#### Completed development work

**Administrator authentication and access control**

- Created the Admin MVC Area.
- Created the administrator login interface.
- Implemented `AdminAccountController`.
- Configured the separate `AdminCookie` authentication scheme.
- Implemented administrator password hashing and verification.
- Added administrator role claims.
- Implemented administrator session creation and logout.
- Restricted administrator pages to authenticated users with the Administrator role.
- Added administrator account management through the `AdminUsers` database table.

**Administrator dashboard and management**

- Created `AdminDashboardController`.
- Implemented the administrator dashboard interface.
- Created the administrator profile page.
- Implemented User Management.
- Implemented Ledger Management.
- Created the Audit Logs administration section.
- Added audit log searching and filtering.
- Added audit log details viewing.
- Improved administrator navigation and page layouts.

**Audit logging improvements**

- Expanded the `AuditLog` model to support different activity actors.
- Added `ActorType`, `ActorId`, `ActorName` and `ActorEmail`.
- Retained the original administrator identity fields for historical compatibility.
- Updated `IAuditLogService` and `AuditLogService`.
- Added support for customer, administrator and system audit actors.
- Created and applied the `AddAuditLogActorDetails` Entity Framework migration.
- Backfilled existing administrator audit records to preserve their identities.
- Integrated customer registration, successful login and password-reset logging into `AuthController`.

#### Current development status

| Component | Status |
|---|---|
| Administrator authentication | Implemented |
| Administrator dashboard | Initial functionality implemented |
| User Management | Implemented |
| Ledger Management | Implemented |
| Audit Logs | Initial functionality implemented; testing ongoing |
| Administrator profile | Implemented |
| Security Settings | Informational interface created; functional controls in progress |
| Notification Settings | Informational interface created; functional preferences and delivery in progress |

#### In progress

- Implementing administrator password management.
- Configuring administrator session timeout.
- Strengthening administrator login security.
- Connecting Security Settings to the authentication system.
- Implementing notification preferences and delivery.
- Extending audit logging to additional customer and administrator activities.
- Testing audit log filtering, searching and historical records.
- Improving administrator dashboard functionality.
- Reviewing security and authorisation across administrative operations.

#### Planned Admin Portal Functionality

- Administrator password recovery.
- Advanced dashboard analytics.
- Wallet management improvements.
- Transaction monitoring.
- Payment management.
- Transfer monitoring.
- Reconciliation.
- Financial reports.
- Additional security controls.
- System notification management.
- Customer account deletion auditing.

---

### 2. Purchase Deletion

A Delete Purchase feature is being developed.

The intended functionality includes:

- Displaying a Delete Purchase button in Spending History.
- Requesting confirmation before deletion.
- Deleting purchases through authenticated API endpoints.
- Refreshing transaction history after successful deletion.
- Updating spending summaries.
- Restoring the appropriate budget balance.
- Preventing users from deleting purchases belonging to other accounts.

**Current status: In progress.**

Deletion functionality still requires integration testing and verification of budget restoration and database consistency.

---

### 3. Transaction Management Improvements

Ongoing improvements include:

- Improving transaction handling.
- Strengthening input validation.
- Improving error messages.
- Ensuring accurate spending calculations.
- Testing API communication.
- Improving the consistency of displayed financial information.
- Reviewing transaction deletion and update behaviour.

**Current status: In progress.**

---

### 4. Testing and Debugging

Development testing focuses on:

- Customer authentication and authorisation.
- Administrator authentication and authorisation.
- Budget calculations.
- Purchase recording.
- Transaction retrieval.
- Purchase deletion.
- Database consistency.
- Audit logging.
- Error handling.
- Frontend and backend integration.
- Administrator session management.
- Notification functionality.

**Current status: Ongoing.**

---

### 5. Development Environment – November 2026

#### Visual Studio startup configuration

- The API and Admin Portal are hosted within the same `SpendWise.API` project.
- The API can be started through Visual Studio or `dotnet run`.
- Swagger and the Admin Portal are available through the API host.
- The .NET MAUI application can be started separately.
- Multiple startup project configuration has been investigated.

#### Android SDK installation

Visual Studio previously reported missing Android SDK components required for building `SpendWise.App` for Android:

- Android SDK Build-Tools 34 (`build-tools;34.0.0`).
- Android SDK Command-line Tools (`cmdline-tools;11.0`).

Installation attempts encountered network connectivity issues.

This affects Android-targeted builds, not necessarily the Windows desktop application or backend.

#### Visual Studio spelling messages

Spelling warnings were reported for words such as:

- Organise.
- Utilisation.
- Categorise.
- Optimisation.

These are valid British English spellings. The messages are spelling-checker warnings rather than C# compilation errors.

---

## 6. SpendWise Pay – Future Expansion

SpendWise is being considered for expansion into a financial platform supporting:

- Person-to-person payments.
- Merchant payments.
- Direct bank payments.
- A SpendWise digital wallet.
- Wallet deposits and withdrawals.
- Payment history.
- Automatic transaction recording.

The payment functionality requires further technical, regulatory and provider research before implementation.

Initial development and testing will use a controlled test environment. Real customer funds will not be handled until the necessary approvals, provider arrangements and safeguards are in place.

**Status: Planned.**

---

## 🔮 Future Development

### 🏪 Grocery Retailer Price Comparison

Potential retailer integrations include:

- Shoprite.
- Checkers.
- Pick n Pay.
- SPAR.
- Woolworths Food.

Planned features:

- Search for grocery products across supported retailers.
- Compare prices for similar products.
- View grocery specials and promotions.
- Compare shopping basket totals.
- Identify cheaper product alternatives.
- View product price history.
- Recommend cost-effective shopping options.
- Estimate how a grocery basket fits within a user's budget.

Retailer integration will depend on access to reliable, permitted pricing data.

**Status: Planned. Live retailer price comparison is not yet available.**

### 🛍️ Smart Grocery Basket

- Build grocery baskets from shopping lists.
- Estimate total basket costs.
- Compare basket costs across supported retailers.
- Identify potential savings.
- Suggest cheaper product alternatives.
- Help users stay within grocery budgets.

### 📈 Advanced Financial Analytics

- Interactive financial dashboards.
- Bar charts, pie charts and line charts.
- Monthly spending trends.
- Category spending comparisons.
- Budget performance analysis.
- Spending forecasts.
- Financial recommendations.
- Exportable financial reports.

### 📱 Cross-Platform Expansion

- Android deployment.
- iOS deployment.
- Improved mobile layouts.
- Enhanced cross-platform compatibility.

### ☁️ Cloud and Production Deployment

- Cloud-hosted backend services.
- Production database hosting.
- Secure production configuration.
- Improved application performance.
- Automated testing.
- Deployment pipelines.
- Production monitoring.

---

## 🏗️ System Architecture

Martin SpendWise follows a client-server architecture.

### Current Architecture

```text
                    MARTIN SPENDWISE
                           |
             +-------------+-------------+
             |                           |
             v                           v
       .NET MAUI App                SpendWise.API
       Customer Interface          ASP.NET Core
                                   Web API + MVC
                                        |
                              +---------+---------+
                              |                   |
                              v                   v
                         REST API           Admin Portal
                              |              (MVC Area)
                              |                   |
                              +---------+---------+
                                        |
                                        v
                              Entity Framework Core
                                        |
                                        v
                                      MySQL
                                        |
                                        v
                                Persistent Storage
```

### Frontend Layer

The .NET MAUI application is responsible for:

- Displaying the customer interface.
- Handling user interaction.
- Managing navigation.
- Presenting budgets and transactions.
- Displaying grocery lists.
- Communicating with the backend API.

The Admin Portal provides a separate interface for authorised administrators.

### Backend Layer

The ASP.NET Core Web API handles:

- User authentication.
- Administrator authentication.
- Authorisation.
- Business logic.
- Budget calculations.
- Transaction processing.
- Purchase management.
- Grocery management.
- Database operations.
- REST API endpoints.
- Administrator dashboard operations.
- Audit logging.

### Authentication Architecture

Customer and administrator authentication are separated.

**Customer authentication:**

- JWT authentication.
- Protected API endpoints.
- Customer identity and authorisation.
- Token versioning.

**Administrator authentication:**

- `AdminCookie` authentication scheme.
- Administrator login and logout.
- Administrator claims.
- Administrator role-based authorisation.
- Password hashing and verification.
- Cookie-based administrator sessions.

### Database Layer

MySQL stores:

- User accounts.
- Administrator accounts.
- Budgets.
- Budget categories.
- Transactions.
- Purchases recorded as transactions.
- Grocery lists and items.
- Budget notifications.
- Audit logs.
- Password reset records.
- Other application records.

Entity Framework Core is used to communicate with the database.

---

## 🛠️ Technology Stack

### Frontend

- C#.
- .NET MAUI.
- XAML.
- HTML.
- CSS.

### Backend

- C#.
- ASP.NET Core Web API.
- ASP.NET Core MVC.
- .NET.
- REST APIs.

### Database

- MySQL.
- Entity Framework Core.
- Pomelo Entity Framework Core Provider.

### Authentication and Security

- JSON Web Tokens (JWT).
- ASP.NET Core Cookie Authentication.
- ASP.NET Core Identity Password Hashing.
- ASP.NET Core User Secrets.
- Protected API endpoints.
- Role-based authorisation.

### Development Tools

- Visual Studio 2022.
- Swagger / OpenAPI.
- MySQL Workbench.
- Git.
- GitHub.

---

## 🔒 Security

Sensitive information must not be committed to the public repository.

The following information must never be uploaded to GitHub:

- Database passwords.
- Database connection strings containing credentials.
- JWT signing keys.
- Email passwords.
- SMTP credentials.
- External API keys.
- Private service credentials.
- Other authentication secrets.

Development credentials are managed using ASP.NET Core User Secrets where applicable.

The public `appsettings.json` should contain only non-sensitive configuration.

### Authorisation

Protected API endpoints require valid authentication tokens.

Backend authorisation must ensure that users can only access or modify records associated with their own accounts.

Administrator accounts use separate authentication and role-based permissions.

---

## ⚙️ Local Development Setup

### Requirements

- Visual Studio 2022 or later.
- Appropriate .NET SDK for the solution.
- .NET MAUI workload.
- MySQL Server.
- MySQL Workbench (recommended).
- Git.

For Android builds, the appropriate Android SDK components must also be installed.

### Step 1: Clone the Repository

```bash
git clone https://github.com/martinhleketani/SpendWise.git
```

Open the repository in Visual Studio using the solution file.

### Step 2: Configure the Backend

Configure development credentials using ASP.NET Core User Secrets.

Typical configuration values include:

```text
ConnectionStrings:DefaultConnection
Jwt:Key
Email configuration
External API credentials (when applicable)
```

### Step 3: Configure MySQL

- Ensure MySQL Server is running.
- Create or configure the required database.
- Configure the database connection string.
- Apply Entity Framework Core migrations where required.

### Step 4: Run the API and Admin Portal

The Admin Portal is hosted within `SpendWise.API`.

1. Open the solution in Visual Studio.
2. Set `SpendWise.API` as the startup project.
3. Start the API using its HTTPS launch profile.
4. Open Swagger to test available API endpoints.
5. Open the Admin Portal through its configured MVC route.

Local development addresses:

```text
API:
https://localhost:7284/

Swagger:
https://localhost:7284/swagger

Admin Portal:
https://localhost:7284/Admin

Direct Admin Login:
https://localhost:7284/Admin/AdminAccount/Login
```

The actual port may differ depending on the launch profile.

### Step 5: Run the .NET MAUI Application

- Start the API first.
- Set the .NET MAUI application as the startup project.
- Run the Windows application.
- Ensure the client API base URL matches the running backend address.

For Android emulators and physical devices, the API address and HTTPS configuration may need to be adjusted. `localhost` on a device does not automatically refer to the development computer.

---

## 📁 Current Project Structure

```text
SpendWise/
│
├── SpendWise.App/
│   ├── Pages/
│   ├── Models/
│   ├── Services/
│   ├── Resources/
│   └── App.xaml
│
├── SpendWise.API/
│   ├── Areas/
│   │   └── Admin/
│   │       ├── Controllers/
│   │       │   ├── AdminAccountController.cs
│   │       │   ├── AdminDashboardController.cs
│   │       │   ├── UserManagementController.cs
│   │       │   ├── LedgerManagementController.cs
│   │       │   └── AuditLogsController.cs
│   │       │
│   │       ├── Models/
│   │       │   └── AdminLoginViewModel.cs
│   │       │
│   │       └── Views/
│   │           ├── AdminAccount/
│   │           ├── AdminDashboard/
│   │           ├── UserManagement/
│   │           ├── LedgerManagement/
│   │           ├── AuditLogs/
│   │           ├── Settings/
│   │           └── Shared/
│   │
│   ├── Controllers/
│   ├── Models/
│   ├── Data/
│   ├── Services/
│   ├── Migrations/
│   ├── wwwroot/
│   └── Program.cs
│
├── README.md
└── .gitignore
```

This structure reflects the current two-project solution, with the Admin Portal hosted inside the API.

---

## 🎯 Project Purpose

Martin SpendWise demonstrates the integration of:

- Cross-platform application development.
- Backend API development.
- Relational database design.
- Object-oriented programming.
- Authentication and authorisation.
- Personal finance management.
- Budget monitoring.
- Transaction processing.
- Grocery management.
- RESTful communication.
- Secure application configuration.
- Data analysis and reporting.
- Software testing and debugging.
- Version control and project documentation.

The project demonstrates the development of a practical software system, from database design and backend implementation to the user-facing application.

It also serves as a software development portfolio project.

---

## 🧭 Development Roadmap

| Development Phase | Description | Status |
|---|---|---|
| Phase 1 | Customer authentication and account management | Implemented |
| Phase 2 | Monthly budgeting and spending categories | Implemented |
| Phase 3 | Purchase recording and spending history | Implemented |
| Phase 4 | Grocery list management | Implemented |
| Phase 5 | Budget threshold notifications | Implemented |
| Phase 6 | Purchase deletion and budget restoration | In progress |
| Phase 7 | Frontend redundancy reduction and UI improvements | In progress |
| Phase 8 | Transaction management and integration testing | In progress |
| Phase 9 | Admin Portal authentication and access control | Implemented |
| Phase 10 | Admin dashboard and management functionality | Partially implemented |
| Phase 11 | Administrator audit logging | Implemented; testing ongoing |
| Phase 12 | Customer audit logging | Implemented in code; testing ongoing |
| Phase 13 | Security Settings and session management | In progress |
| Phase 14 | Notification Settings and delivery | In progress |
| Phase 15 | Customer account deletion and deletion auditing | Planned |
| Phase 16 | Administrator password recovery | Planned |
| Phase 17 | SpendWise Pay architecture and provider integration | Planned |
| Phase 18 | Retailer price comparison | Planned |
| Phase 19 | Grocery basket optimisation | Planned |
| Phase 20 | Advanced analytics and reporting | Planned |
| Phase 21 | Mobile and cloud deployment | Planned |

*Implemented indicates functionality developed within the project; it does not necessarily mean production-ready or fully tested.*

---

## 👨‍💻 Developer

**Martin Chabalala**

BSc Information Technology  
North-West University

Aspiring Software Developer

GitHub: [martinhleketani](https://github.com/martinhleketani)

---

## 📄 Project Status

**Status: Active Development**

### What We Have

- Customer authentication and account management.
- Monthly budget creation and monitoring.
- Spending category management.
- Purchase recording.
- Transaction history.
- Budget threshold notifications.
- Grocery list management.
- Financial summaries.
- Initial Admin Portal.
- Separate administrator authentication architecture.
- Administrator dashboard and profile.
- User Management.
- Ledger Management.
- Audit Logs administration.
- Expanded audit logging model and database migration.
- Customer registration, login and password-reset audit integration in code.
- Redesigned customer login interface.
- Personalised login welcome modal.
- Updated dashboard interface and API integration work.

### What We Are Currently Working On

- Purchase deletion and budget restoration.
- Transaction management improvements.
- Frontend redundancy reduction and reusable UI components.
- Improving navigation and interface consistency.
- Customer and administrator audit-log testing.
- Security Settings implementation.
- Administrator password management.
- Administrator session timeout configuration.
- Administrator login security controls.
- Notification Settings implementation.
- Notification preferences and actual alert delivery.
- Error handling and validation.
- Frontend and backend integration testing.
- Completing and testing the updated login and dashboard interfaces.

### What We Plan to Build Next

- Customer account deletion and permanent deletion auditing.
- Complete administrator security controls.
- Complete administrator notification functionality.
- Additional dashboard analytics and reporting.
- Administrator password recovery.
- SpendWise Pay.
- Grocery price comparison across supported South African retailers.
- Retailer specials and promotions.
- Smart grocery basket comparison.
- Cheapest-basket recommendations.
- Advanced financial analytics.
- Mobile deployment.
- Cloud hosting and production deployment.

### Long-Term Vision

The long-term vision is to develop Martin SpendWise into a comprehensive personal finance and grocery management platform that helps South African consumers manage their budgets, understand their spending, make more cost-effective shopping decisions and eventually access integrated payment services.

---

## 📜 License

No open-source license has been specified for this project.

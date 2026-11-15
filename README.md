# 💰 Martin SpendWise

**Martin SpendWise** is a full-stack personal finance, budgeting and grocery management application developed using .NET MAUI, ASP.NET Core Web API and MySQL.

The application helps users manage monthly budgets, record purchases, monitor spending, organise grocery lists and make informed financial decisions.

The current system focuses on personal budgeting, purchase tracking, spending monitoring and grocery management, with future plans to introduce grocery price comparison across major South African retailers.

---

## 🚀 Project Overview

Martin SpendWise combines financial tracking and grocery planning in one application.

The project currently consists of:

- A **.NET MAUI application** for the customer interface.
- An **ASP.NET Core Web API** for backend services and business logic.
- A **MySQL database** for persistent data storage.
- **Entity Framework Core** for database operations.
- **JWT authentication** for securing user accounts and API requests.
- An administration portal currently under development.

The application follows a client-server architecture, with the frontend communicating with the backend through REST API endpoints.

The Windows desktop application is the current development and testing target.

---

## ✨ Current Features

### 🔐 User Authentication and Account Management

- User registration
- Secure login
- JWT-based authentication
- Password hashing
- Password recovery
- Email-based password reset
- Authenticated access to protected API endpoints

### 💵 Budget Management

- Create monthly budgets
- Manage budget categories
- Allocate funds to spending categories
- Monitor available funds
- Track spending against budgets
- View remaining budget balances
- Monitor budget utilisation
- Receive notifications when spending reaches configured thresholds

### 💳 Transaction Management

- Record financial transactions
- Record purchases and expenses
- Categorise spending
- View transaction history
- Monitor financial activity
- View spending summaries
- Filter spending history
- Review purchase descriptions, amounts and dates

### 🧾 Purchase Recording

- Record purchases against monthly budgets
- Select the relevant spending category
- Enter purchase amounts and descriptions
- Monitor how purchases affect category spending
- View recorded purchases in Spending History
- Receive budget usage notifications

### 🛒 Grocery Management

- Create grocery lists
- Add grocery products
- Manage grocery list items
- Specify grocery quantities
- Estimate grocery expenses
- Organise grocery purchases
- Monitor grocery-related spending

### 📊 Financial Summaries and Insights

- Spending summaries
- Budget utilisation information
- Category-based spending
- Transaction counts
- Average purchase amounts
- Financial activity overviews

---

## 🚧 Current Development Progress

### 1. SpendWise Admin Portal

An administrator portal is being developed to support future administration and management of the SpendWise platform.

The initial development work includes:

- Creating a separate ASP.NET Core MVC Admin project.
- Creating an administrator login page.
- Designing the login interface using HTML and CSS.
- Implementing an administrator login view model.
- Creating an `AdminAccountController`.
- Configuring the Admin Portal to open the login page by default.
- Configuring static file support for CSS and other frontend resources.
- Resolving the initial CSS file path issue.
- Successfully opening the Admin Login page in the browser.

**Current status:** The Admin Login interface is working. Administrator authentication has not yet been connected to the API.

### Planned Admin Portal Functionality

The administration portal is intended to provide:

- Administrator authentication
- Admin dashboard
- User management
- Wallet management
- Transaction monitoring
- Payment management
- Transfer monitoring
- Reconciliation
- Financial reports
- Security and audit logs
- System settings

The long-term architecture will use the existing SpendWise API as the shared backend for the customer application and administrator portal, with separate authentication and permissions.

The proposed consolidation of the Admin Portal into the existing API project has not yet been completed.

### 2. SpendWise Pay – Future Expansion

SpendWise is being considered for expansion into a financial platform supporting:

- Person-to-person payments
- Merchant payments
- Direct bank payments
- A SpendWise digital wallet
- Wallet deposits and withdrawals
- Payment history
- Automatic transaction recording

The payment functionality requires further technical, regulatory and provider research before implementation.

Initial development and testing will use a controlled test environment. Real customer funds will not be handled until the necessary approvals, provider arrangements and safeguards are in place.

### 3. Purchase Deletion

A Delete Purchase feature is being implemented.

The intended functionality includes:

- Displaying a Delete Purchase button in Spending History
- Requesting confirmation before deletion
- Deleting purchases through authenticated API endpoints
- Refreshing transaction history after successful deletion
- Updating spending summaries
- Restoring the appropriate budget balance
- Preventing users from deleting purchases belonging to other accounts

**Current status:** The deletion functionality is still being debugged and tested.

### 4. Transaction Management Improvements

Current improvements include:

- Improving transaction handling
- Strengthening input validation
- Improving error messages
- Ensuring accurate spending calculations
- Testing API communication
- Improving the consistency of displayed financial information

### 5. User Interface Improvements

Ongoing improvements include:

- Improving page layouts
- Enhancing navigation
- Improving transaction displays
- Refining buttons and user interactions
- Improving loading indicators and error feedback
- Ensuring consistent styling throughout the application

### 6. Testing and Debugging

Development testing focuses on:

- Authentication and authorisation
- Budget calculations
- Purchase recording
- Transaction retrieval
- Purchase deletion
- Database consistency
- Error handling
- Frontend and backend integration

### 7. Development Environment – 15 November 2026

The following development environment issues were identified:

**Visual Studio startup configuration**

- The Admin Website was successfully launched using `dotnet run`.
- Visual Studio was attempting to start the Android emulator when the intended target was the Admin Website and API.
- Multiple startup project configuration was investigated.
- Running the API and Admin Website through separate terminal sessions was identified as an alternative development approach.

**Android SDK installation**

Visual Studio reported missing Android SDK components required for building `SpendWise.App` for Android:

- Android SDK Build-Tools 34 (`build-tools;34.0.0`)
- Android SDK Command-line Tools (`cmdline-tools;11.0`)

The installation attempts failed because the development environment reported that a network connection was unavailable.

This issue affects Android-targeted builds. It does not mean that the existing Windows desktop application or backend is broken.

**Visual Studio spelling messages**

Spelling warnings were reported for words such as:

- Organise
- Utilisation
- Categorise
- Optimisation

These are valid British English spellings. The messages are spelling-checker warnings rather than C# compilation errors.

---

## 🔮 Future Development

### 🏪 Grocery Retailer Price Comparison

Potential retailer integrations include:

- Shoprite
- Checkers
- Pick n Pay

Planned features:

- Search for grocery products across supported retailers
- Compare prices for similar products
- View grocery specials and promotions
- Compare shopping basket totals
- Identify cheaper product alternatives
- View product price history
- Recommend cost-effective shopping options
- Estimate how a grocery basket fits within a user's budget

Retailer integration will depend on access to reliable, permitted pricing data.

**Status: Planned. Live retailer price comparison is not yet available.**

### 🛍️ Smart Grocery Basket

- Build grocery baskets from shopping lists
- Estimate total basket costs
- Compare basket costs across supported retailers
- Identify potential savings
- Suggest cheaper product alternatives
- Help users stay within grocery budgets

### 📈 Advanced Financial Analytics

- Interactive financial dashboards
- Bar charts, pie charts and line charts
- Monthly spending trends
- Category spending comparisons
- Budget performance analysis
- Spending forecasts
- Financial recommendations
- Exportable financial reports

### 📱 Cross-Platform Expansion

- Android deployment
- iOS deployment
- Improved mobile layouts
- Enhanced cross-platform compatibility

### ☁️ Cloud and Production Deployment

- Cloud-hosted backend services
- Production database hosting
- Secure production configuration
- Improved application performance
- Automated testing
- Deployment pipelines
- Production monitoring

---

## 🏗️ System Architecture

Martin SpendWise follows a client-server architecture.

### Current Architecture

```text
              MARTIN SPENDWISE
                     |
          +----------+----------+
          |                     |
          v                     v
   .NET MAUI App          SpendWise Admin
   Customer Interface    (MVC - in development)
          |                     |
          |                     |
          +----------+----------+
                     |
                     v
             ASP.NET Core API
                  (.NET 8)
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

The intended architecture will use the existing API as the shared backend, with separate customer and administrator access.

### Frontend Layer

The .NET MAUI application is responsible for:

- Displaying the customer interface
- Handling user interaction
- Managing navigation
- Presenting budgets and transactions
- Displaying grocery lists
- Communicating with the backend API

The Admin Portal will provide a separate interface for authorised administrators.

### Backend Layer

The ASP.NET Core Web API handles:

- User authentication
- Authorisation
- Business logic
- Budget calculations
- Transaction processing
- Purchase management
- Grocery management
- Database operations
- REST API endpoints

### Database Layer

MySQL stores:

- User accounts
- Budgets
- Budget categories
- Transactions
- Purchases recorded as transactions
- Grocery lists and items
- Budget notifications
- Other application records

Entity Framework Core is used to communicate with the database.

---

## 🛠️ Technology Stack

### Frontend

- C#
- .NET MAUI
- XAML
- HTML
- CSS

### Backend

- C#
- ASP.NET Core Web API
- ASP.NET Core MVC
- .NET 8
- REST APIs

### Database

- MySQL
- Entity Framework Core
- Pomelo Entity Framework Core Provider

### Authentication and Security

- JSON Web Tokens (JWT)
- ASP.NET Core Password Hashing
- ASP.NET Core User Secrets
- Protected API endpoints

### Development Tools

- Visual Studio 2022
- Swagger / OpenAPI
- MySQL Workbench
- Git
- GitHub

---

## 🔒 Security

Sensitive information must not be committed to the public repository.

The following information must never be uploaded to GitHub:

- Database passwords
- Database connection strings containing credentials
- JWT signing keys
- Email passwords
- SMTP credentials
- External API keys
- Private service credentials
- Other authentication secrets

Development credentials are intended to be managed using ASP.NET Core User Secrets.

The public `appsettings.json` should contain only non-sensitive configuration.

### Example Configuration

```json
{
  "Jwt": {
    "Issuer": "SpendWise.API",
    "Audience": "SpendWise.App",
    "ExpiryMinutes": 60
  }
}
```

Actual credentials are intentionally excluded from the repository.

### Authorisation

Protected API endpoints require valid authentication tokens.

Backend authorisation must ensure that users can only access or modify records associated with their own accounts.

Administrator accounts will require separate role-based permissions and secure authentication.

---

## ⚙️ Local Development Setup

### Requirements

- Visual Studio 2022 or later
- .NET 8 SDK
- .NET MAUI workload
- MySQL Server
- MySQL Workbench (recommended)
- Git

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

### Step 4: Run the API

- Open the API project.
- Select the API startup profile.
- Start the ASP.NET Core Web API.
- Use Swagger to test available endpoints.

### Step 5: Run the .NET MAUI Application

- Start the API first.
- Set the .NET MAUI application as the startup project.
- Run the Windows application.
- Ensure the client API base URL matches the running backend address.

### Step 6: Run the Admin Portal

The current Admin MVC project can be started separately during development.

```bash
cd SpendWise.Admin
dotnet run
```

The API can be started in another terminal:

```bash
cd SpendWise.API
dotnet run
```

The Admin Portal's authentication and API integration are still under development.

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
│   ├── Controllers/
│   ├── Data/
│   ├── DTOs/
│   ├── Models/
│   ├── Services/
│   ├── Migrations/
│   └── Program.cs
│
├── SpendWise.Admin/
│   ├── Controllers/
│   ├── Models/
│   ├── Views/
│   ├── wwwroot/
│   └── Program.cs
│
├── README.md
└── .gitignore
```

The Admin project is currently separate. Its planned consolidation into the API project has not yet been performed.

---

## 🎯 Project Purpose

Martin SpendWise demonstrates the integration of:

- Cross-platform application development
- Backend API development
- Relational database design
- Object-oriented programming
- Authentication and authorisation
- Personal finance management
- Budget monitoring
- Transaction processing
- Grocery management
- RESTful communication
- Secure application configuration
- Data analysis and reporting
- Software testing and debugging
- Version control and project documentation

The project demonstrates the development of a practical software system, from database design and backend implementation to the user-facing application.

It also serves as a software development portfolio project.

---

## 🧭 Development Roadmap

| Development Phase | Description | Status |
|---|---|---|
| Phase 1 | User authentication and account management | Implemented |
| Phase 2 | Monthly budgeting and spending categories | Implemented |
| Phase 3 | Purchase recording and spending history | Implemented |
| Phase 4 | Grocery list management | Implemented |
| Phase 5 | Budget threshold notifications | Implemented |
| Phase 6 | Purchase deletion and budget restoration | In progress |
| Phase 7 | UI improvements and integration testing | In progress |
| Phase 8 | Admin Portal and authentication | In progress |
| Phase 9 | Admin/API architecture consolidation | Planned |
| Phase 10 | SpendWise Pay architecture and provider integration | Planned |
| Phase 11 | Retailer price comparison | Planned |
| Phase 12 | Grocery basket optimisation | Planned |
| Phase 13 | Advanced analytics and reporting | Planned |
| Phase 14 | Mobile and cloud deployment | Planned |

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

- User authentication and account management
- Monthly budget creation and monitoring
- Spending category management
- Purchase recording
- Transaction history
- Budget threshold notifications
- Grocery list management
- Financial summaries
- Initial Admin Portal login interface

### What We Are Currently Working On

- Purchase deletion
- Accurate budget restoration after deleting purchases
- Transaction management improvements
- Admin Portal development
- Admin authentication architecture
- User interface enhancements
- Error handling and validation
- Frontend and backend integration testing

### What We Plan to Build Next

- Admin dashboard and administration functionality
- Admin/API architecture consolidation
- SpendWise Pay
- Grocery price comparison across supported South African retailers
- Retailer specials and promotions
- Smart grocery basket comparison
- Cheapest-basket recommendations
- Advanced financial analytics
- Mobile deployment
- Cloud hosting and production deployment

### Long-Term Vision

The long-term vision is to develop Martin SpendWise into a comprehensive personal finance and grocery management platform that helps South African consumers manage their budgets, understand their spending, make more cost-effective shopping decisions and eventually access integrated payment services.

---

## 📜 License

No open-source license has been specified for this project.
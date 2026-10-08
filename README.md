# 💰 Martin SpendWise

**Martin SpendWise** is a full-stack personal finance, budgeting and grocery management application developed using .NET MAUI, ASP.NET Core Web API and MySQL.

The application is designed to help users manage monthly budgets, record purchases, monitor spending, organise grocery lists and make informed financial decisions.

The current system focuses on **personal budgeting, purchase tracking, spending monitoring and grocery management**, with future plans to introduce grocery price comparison across major South African retailers.

---

## 🚀 Project Overview

Martin SpendWise is a personal finance management system that combines financial tracking and grocery planning in one application.

The project consists of:

- A **.NET MAUI application** for the user interface
- An **ASP.NET Core Web API** for backend services and business logic
- A **MySQL database** for persistent data storage
- **Entity Framework Core** for database operations
- **JWT authentication** for securing user accounts and API requests

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

SpendWise allows users to:

- Create monthly budgets
- Manage budget categories
- Allocate funds to spending categories
- Monitor available funds
- Track spending against budgets
- View remaining budget balances
- Monitor budget utilisation
- Receive notifications when spending reaches configured thresholds

Budget notifications help users identify when they are approaching or exceeding their allocated spending limits.

### 💳 Transaction Management

Users can:

- Record financial transactions
- Record purchases and expenses
- Categorise spending
- View transaction history
- Monitor financial activity
- View spending summaries
- Filter spending history
- Review purchase descriptions, amounts and dates

### 🧾 Purchase Recording

The purchase recording functionality allows users to:

- Record purchases against monthly budgets
- Select the relevant spending category
- Enter purchase amounts and descriptions
- Monitor how purchases affect category spending
- View recorded purchases in Spending History
- Receive budget usage notifications

### 🛒 Grocery Management

SpendWise includes grocery planning functionality that allows users to:

- Create grocery lists
- Add grocery products
- Manage grocery list items
- Specify grocery quantities
- Estimate grocery expenses
- Organise grocery purchases
- Monitor grocery-related spending

The grocery management module helps users plan purchases and understand their estimated grocery costs.

### 📊 Financial Summaries and Insights

SpendWise provides financial information through:

- Spending summaries
- Budget utilisation information
- Category-based spending
- Transaction counts
- Average purchase amounts
- Financial activity overviews

These features help users understand how their money is being spent.

---

## 🚧 Currently Under Development

The current development phase focuses on improving the reliability, functionality and user experience of the existing application.

### 🗑️ Purchase Deletion

A Delete Purchase feature is currently being implemented.

The intended functionality includes:

- Displaying a Delete Purchase button in Spending History
- Allowing users to remove incorrectly recorded purchases
- Requesting confirmation before deletion
- Deleting purchases through authenticated API endpoints
- Refreshing transaction history after successful deletion
- Updating spending summaries
- Restoring the appropriate budget balance
- Preventing users from deleting purchases belonging to other accounts

**Current status:** The deletion functionality is still being debugged and tested. It is not yet considered fully operational.

### 🔄 Transaction Management Improvements

Current improvements include:

- Improving transaction handling
- Strengthening input validation
- Improving error messages
- Ensuring accurate spending calculations
- Testing API communication
- Improving the consistency of displayed financial information

### 🎨 User Interface Improvements

Ongoing improvements include:

- Improving page layouts
- Enhancing navigation
- Improving transaction displays
- Refining buttons and user interactions
- Improving loading indicators and error feedback
- Ensuring consistent styling throughout the application

### 🧪 Testing and Debugging

Development testing focuses on:

- Authentication and authorization
- Budget calculations
- Purchase recording
- Transaction retrieval
- Purchase deletion
- Database consistency
- Error handling
- Frontend and backend integration

---

## 🔮 Future Development

The following features are planned for future versions of Martin SpendWise and are not currently presented as completed functionality.

### 🏪 Grocery Retailer Price Comparison

A major future objective is to integrate grocery pricing information from South African retailers, potentially including:

- Shoprite
- Checkers
- Pick n Pay

The goal is to help users compare grocery prices from multiple retailers through one application.

#### Planned Features

- Search for grocery products across supported retailers
- Compare prices for similar products
- View available grocery specials and promotions
- Compare shopping basket totals
- Identify cheaper product alternatives
- View product price history
- Recommend cost-effective shopping options
- Estimate how a grocery basket fits within a user's budget

Retailer integration will depend on access to reliable, permitted pricing data and appropriate methods for keeping product information up to date.

**Status: Planned for future development. Live retailer price comparison is not yet available.**

### 🛍️ Smart Grocery Basket

Planned functionality includes:

- Building a grocery basket from a shopping list
- Estimating the total cost of the basket
- Comparing basket costs across supported retailers
- Identifying potential savings
- Suggesting cheaper product alternatives
- Helping users stay within their grocery budgets

### 📈 Advanced Financial Analytics

Future enhancements may include:

- Interactive financial dashboards
- Bar charts, pie charts and line charts
- Monthly spending trends
- Category spending comparisons
- Budget performance analysis
- Spending forecasts
- Financial recommendations
- Exportable financial reports

### 📱 Cross-Platform Expansion

Planned improvements include:

- Android deployment
- iOS deployment
- Improved mobile layouts
- Enhanced cross-platform compatibility

### ☁️ Cloud and Production Deployment

Long-term technical improvements include:

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

```text
             MARTIN SPENDWISE
                    |
                    v
           .NET MAUI Application
              (C# and XAML)
                    |
                    | HTTPS / REST / JSON
                    v
          ASP.NET Core Web API
                (.NET 8)
                    |
                    | Entity Framework Core
                    v
                  MySQL
                    |
                    v
            Persistent Storage
```

### Frontend Layer

The .NET MAUI application is responsible for:

- Displaying the user interface
- Handling user interaction
- Managing navigation
- Presenting budgets and transactions
- Displaying grocery lists
- Communicating with the backend API

### Backend Layer

The ASP.NET Core Web API handles:

- User authentication
- Authorization
- Business logic
- Budget calculations
- Transaction processing
- Purchase management
- Grocery management
- Database operations
- REST API endpoints

### Database Layer

MySQL is used to store application information, including:

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

### Backend

- C#
- ASP.NET Core Web API
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
- Secure backend configuration

### Development Tools

- Visual Studio 2022
- Swagger / OpenAPI
- MySQL Workbench
- Git
- GitHub

---

## 🔒 Security

Security is an important part of the SpendWise architecture.

Sensitive information must not be committed to the public repository.

The following information must **never be uploaded to GitHub**:

- Database passwords
- Database connection strings containing credentials
- JWT signing keys
- Email passwords
- SMTP credentials
- External API keys
- Private service credentials
- Other authentication secrets

Development credentials are intended to be managed using **ASP.NET Core User Secrets**.

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

### Authorization

Protected API endpoints require valid authentication tokens.

Backend authorization must ensure that users can only access or modify records associated with their own accounts.

---

## ⚙️ Local Development Setup

### Requirements

Install:

- Visual Studio 2022 or later
- .NET 8 SDK
- .NET MAUI workload
- MySQL Server
- MySQL Workbench (recommended)
- Git

### Step 1: Clone the Repository

```bash
git clone https://github.com/martinhleketani/SpendWise.git
```

Open the repository in Visual Studio using the solution file.

### Step 2: Configure the Backend

Configure the required development credentials using ASP.NET Core User Secrets.

Typical configuration values include:

```text
ConnectionStrings:DefaultConnection
Jwt:Key
Email configuration
External API credentials (when applicable)
```

The actual values depend on the local development environment.

### Step 3: Configure MySQL

- Ensure MySQL Server is running
- Create or configure the required database
- Configure the database connection string
- Apply Entity Framework Core migrations where required

### Step 4: Run the API

- Open the API project in Visual Studio
- Select the API startup profile
- Start the ASP.NET Core Web API
- Use Swagger to test available endpoints

### Step 5: Run the .NET MAUI Application

- Start the API first
- Set the .NET MAUI application as the startup project
- Run the Windows application
- Ensure the client API base URL matches the running backend address

The application requires the backend and database to be correctly configured for features that depend on persistent data.

---

## 📁 Project Structure

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
├── README.md
└── .gitignore
```

The structure above illustrates the main application components. The actual repository layout may differ slightly.

---

## 🎯 Project Purpose

Martin SpendWise was developed as a practical software engineering project demonstrating the integration of:

- Cross-platform application development
- Backend API development
- Relational database design
- Object-oriented programming
- Authentication and authorization
- Personal finance management
- Budget monitoring
- Transaction processing
- Grocery management
- RESTful communication
- Secure application configuration
- Data analysis and reporting
- Software testing and debugging
- Version control and project documentation

The project demonstrates the development of a real-world software system, from database design and backend implementation to the user-facing application.

It also serves as a practical software development portfolio project.

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
| Phase 8 | Retailer price comparison | Planned |
| Phase 9 | Grocery basket optimisation | Planned |
| Phase 10 | Advanced analytics and reporting | Planned |
| Phase 11 | Mobile and cloud deployment | Planned |

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

Martin SpendWise is an ongoing full-stack software development project.

### What We Have Now

The current application focuses on:

- User authentication and account management
- Monthly budget creation and monitoring
- Spending category management
- Purchase recording
- Transaction history
- Budget threshold notifications
- Grocery list management
- Financial summaries

### What We Are Currently Working On

- Purchase deletion
- Accurate budget restoration after deleting purchases
- Transaction management improvements
- User interface enhancements
- Error handling and validation
- Frontend and backend integration testing

### What We Plan to Build Next

- Grocery price comparison across supported South African retailers
- Retailer specials and promotions
- Smart grocery basket comparison
- Cheapest-basket recommendations
- Advanced financial analytics
- Mobile deployment
- Cloud hosting and production deployment

### Long-Term Vision

The long-term vision is to develop Martin SpendWise into a comprehensive personal finance and grocery management platform that helps South African consumers manage their budgets, understand their spending and make more cost-effective shopping decisions.

---

## 📜 License

No open-source license has been specified for this project.
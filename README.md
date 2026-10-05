# 💰 Martin SpendWise

**Martin SpendWise** is a full-stack personal finance, budgeting and grocery management application designed to help users manage their money, monitor spending and make better purchasing decisions.

The system combines personal financial management with grocery planning and retailer price comparison in a single application.

---

## 🚀 Overview

SpendWise allows users to manage their finances through budgeting, transaction tracking, spending insights and grocery planning.

The project consists of a **.NET MAUI client application** connected to an **ASP.NET Core Web API**, with **MySQL** used for persistent data storage.

The application is designed around a client-server architecture where the mobile/desktop client communicates with the backend through REST API endpoints.

---

## ✨ Main Features

### 🔐 User Authentication

- User registration
- Secure login
- JWT-based authentication
- Password hashing
- Password recovery
- Email-based password reset
- Authenticated API access

### 💵 Budget Management

Users can:

- Create monthly budgets
- Monitor available funds
- Track spending against their budget
- View financial summaries
- Monitor remaining balances

### 💳 Transaction Management

- Record income and expenses
- Categorise transactions
- View transaction history
- Monitor spending patterns
- Analyse financial activity

### 📊 Financial Reports & Insights

SpendWise provides users with financial information through:

- Spending summaries
- Budget analysis
- Category-based spending
- Financial reports
- Visual insights and charts

### 🛒 Grocery Management

Users can:

- Create grocery lists
- Add grocery products
- Track grocery quantities
- Estimate grocery costs
- Compare grocery spending against available budgets

### 🏪 Retailer Price Comparison

SpendWise includes infrastructure for retrieving and comparing grocery pricing information from supported retailer/data services.

The long-term objective is to allow users to compare products and prices from multiple retailers from one application instead of manually checking several retailer platforms.

External service credentials are handled through secure backend configuration and are not stored in the client application.

---

## 🏗️ Architecture

SpendWise follows a client-server architecture:

```text
.NET MAUI Application
        |
        | HTTPS / REST / JSON
        v
ASP.NET Core Web API
        |
        | Entity Framework Core
        v
      MySQL
```

External services used by the system are accessed through the backend API rather than exposing private credentials to the client.

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

### Authentication & Security

- JSON Web Tokens (JWT)
- ASP.NET Core Password Hashing
- Secure configuration
- User Secrets for development credentials

### Development Tools

- Visual Studio
- Swagger / OpenAPI
- Git
- GitHub

---

## 🔒 Security

Sensitive information is intentionally excluded from the repository.

The following information must **never be committed to source control**:

- Database passwords
- Database connection strings containing credentials
- JWT signing keys
- Email account passwords
- SMTP credentials
- External API keys
- Private service credentials

Development secrets are stored locally using **ASP.NET Core User Secrets**.

The public `appsettings.json` contains only non-sensitive configuration.

Example:

```json
{
  "Jwt": {
    "Issuer": "SpendWise.API",
    "Audience": "SpendWise.App",
    "ExpiryMinutes": 60
  }
}
```

Developers running the project locally must configure their own required credentials.

---

## ⚙️ Local Development

### Requirements

Install:

- Visual Studio 2022 or later
- .NET 8 SDK
- .NET MAUI workload
- MySQL Server
- MySQL Workbench (recommended)

### Backend Configuration

Configure the required development credentials using ASP.NET Core User Secrets.

Typical configuration includes:

```text
ConnectionStrings:DefaultConnection
Jwt:Key
Email configuration
External API credentials
```

Actual credentials are intentionally not included in this repository.

### Database

Ensure MySQL Server is running and configure a local database connection through User Secrets.

Entity Framework Core is used for database access and schema management.

### Run the API

Set the SpendWise API project as the startup project and run it through Visual Studio.

Swagger can be used during development to test available API endpoints.

### Run the Client

Start the SpendWise .NET MAUI project after the API is running.

The client communicates with the SpendWise backend through HTTP/REST requests.

---

## 📁 Project Structure

```text
SpendWise
│
├── SpendWise
│   ├── .NET MAUI client
│   ├── Views
│   ├── ViewModels
│   ├── Models
│   └── Services
│
├── SpendWise.API
│   ├── Controllers
│   ├── Data
│   ├── DTOs
│   ├── Models
│   ├── Services
│   ├── Migrations
│   └── Program.cs
│
├── README.md
└── .gitignore
```

---

## 🎯 Project Purpose

SpendWise was developed as a practical software engineering project demonstrating the integration of:

- Cross-platform application development
- Backend API development
- Relational databases
- Authentication and authorization
- Personal finance management
- External service integration
- RESTful communication
- Secure configuration
- Data analysis and reporting

The project is also intended to demonstrate the development of a real-world system from database and backend design through to the user-facing application.

---

## 🔮 Future Development

Planned improvements include:

- Expanded retailer integrations
- More comprehensive grocery price comparison
- Special and promotion tracking
- Cheapest-basket recommendations
- Improved financial analytics
- Additional reporting capabilities
- Cloud-hosted backend services
- Production deployment
- Mobile deployment improvements

---

## 👨‍💻 Developer

**Martin Chabalala**

BSc Information Technology  
North-West University

Graduate / Junior Software Developer

GitHub: [martinhleketani](https://github.com/martinhleketani)

---

## 📄 Project Status

SpendWise is currently under active development.

Core personal finance, budgeting, authentication, transaction management and grocery functionality have been implemented, while retailer integration and price-comparison capabilities continue to be expanded.
🛍️ Orders Management System

A .NET 8 Web API + Razor Pages project implementing a secure, role-based Orders Management System with Products, Customers, and Orders. The system demonstrates CRUD operations, caching, concurrency handling, JWT authentication, and integration with an external discount service.

🚀 Features
🧩 Core Functionality

CRUD Endpoints

Products (Admin only)
Customers (Admin only)
Orders
Validates product stock and pricing before order creation.
Applies discounts via HttpClient call to external discount service.
Supports filters (customerId, date), pagination, and sorting.
Implements response caching and ETag for performance and efficiency.

🔒 Security

JWT Authentication with login endpoint (/api/auth/login).
Role-based access control (Admin, User).
Rate Limiting to prevent abuse.
Security best practices:
HTTP Security Headers
No sensitive data exposure in responses.

⚙️ Concurrency & Validation

Optimistic Concurrency Control on Product updates.
Uses If-Match header with an ETag token.
Returns 409 Conflict on version mismatch.

Validation

Stock and price verified at order creation.
Graceful error handling and proper HTTP response codes.

🧠 Caching

In-memory caching (IMemoryCache) for order listings.
ETag validation to minimize unnecessary network load.

💻 UI (Razor Pages)

Simple frontend with Razor Pages:
Login page to get JWT token.
Products list (read-only for normal users).
Create Order page using fetch API calls with JWT token.
JWT token is stored temporarily and attached to API requests.

🧰 Tech Stack
Component	Technology
Backend	ASP.NET Core 8 Web API
Frontend	Razor Pages
Auth	JWT (JSON Web Token)
Database	In-Memory Repository
Integration	HttpClient (Discount Service)
Caching	IMemoryCache
Rate Limiting	ASP.NET Core Rate Limiting Middleware

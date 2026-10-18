# Hospital Management System

A hospital management system built with ASP.NET Core MVC and C#. It supports role-based access and covers doctor scheduling, appointments, billing, medical records, prescriptions and reports.

## Roles
- Admin
- Doctor
- Patient
- Receptionist

## Features
- Role-based login and access control with JWT authentication
- Doctor scheduling with calendar events
- Appointment booking and management
- Patient, doctor and department management
- Medical records and prescriptions
- Test results
- Billing
- Notifications
- Reports

## Tech Stack
- Language: C#
- Framework: ASP.NET Core MVC (.NET 8)
- Views: Razor (.cshtml)
- ORM: Entity Framework Core with Migrations
- Database: PostgreSQL (hosted on Neon)
- Authentication: JWT
- Version control: Git and GitHub

## Project Structure
- Controllers: request handling for each module
- Models: Doctor, Patient, Appointment, Billing, MedicalRecord, Prescription, TestResult, DoctorSchedule and more
- Views: Razor pages for each role and module
- Services: business logic
- Data and Migrations: database context and schema

## How to Run
1. Clone the repository
2. Open the solution in Visual Studio or VS Code
3. Add your own database connection string and JWT key in `appsettings.Development.json` or with .NET user secrets (never commit real secrets)
4. Apply the migrations: `dotnet ef database update`
5. Run the project: `dotnet run`

## My Role
[Write your part here, for example: Full stack development, or the modules you built. Remove this section if you built the whole project.]

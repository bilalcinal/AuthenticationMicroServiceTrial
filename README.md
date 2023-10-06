# .NET 6 Microservices-based Authentication System

This project demonstrates a comprehensive user authentication system built on .NET 6 as a Microservice-based architecture. Instead of a monolithic approach, this project decouples different aspects of authentication into separate services, which are then orchestrated using Ocelot as the gateway.

## Microservices:

1. **Authentication Service**: Handles user login, JWT token generation, and other core authentication mechanisms.
2. **Account Service**: Manages user registration, account details, and any associated account management functions.
3. **Notification Service**: Manages the sending of email notifications, such as account confirmation emails.

## Features:

1. **JWT Token Based Authentication**: Securely manage user sessions and protect API endpoints.
2. **Email Notifications**: Using SMTP for sending user account-related emails.

## Technologies Used:

- **.NET 6 WebAPI**: As the foundation for building the microservices.
- **Ocelot**: As the gateway to manage and forward API requests appropriately to the respective microservices.
- **SMTP**: For sending out email notifications.

## Getting Started:

### Prerequisites:

- [.NET SDK 6.0](https://dotnet.microsoft.com/download/dotnet/6.0)
- Configured SMTP server for email notifications

### Installation:

1. Clone this repository:
   \```bash
   git clone  https://github.com/bilalcinal/AuthenticationMicroServiceTrial
   \```
2. Navigate to the root project directory:
   \```bash
   cd AuthenticationMicroServiceTrial
   \```
3. Update the configuration files across the services, especially the SMTP settings in the Notification Service and JWT token settings in the Authentication Service.
4. Run each microservice separately or use a docker-compose (if provided) to boot them all:
   \```bash
   dotnet run --project [specific-service-path]
   \```

Visit the Ocelot gateway endpoint to start making requests to the services.

## Contributing:

Pull requests are welcome. For major changes, please open an issue first to discuss what you'd like to change.

## License:

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

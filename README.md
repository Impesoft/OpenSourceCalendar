# OpenSourceCalendar

OpenSourceCalendar is a Blazor WebAssembly application targeting .NET 9. This project provides functionalities for booking rooms, managing user roles, and handling user authentication.

## Project Structure

- **OpenSourceCalendar.Client**: The Blazor WebAssembly client project.
- **OpenSourceCalendar.Server**: The ASP.NET Core server project (if applicable).
- **OpenSourceCalendar.Shared**: Shared classes and services between the client and server.
- **OpenSourceCalendar.Data**: Data models and Entity Framework Core migrations.

## Key Components

### Booking.razor.cs
This component handles the booking functionalities, including displaying available rooms, managing booking states, and navigating between months.

### ApplicationUser.cs
This class extends `IdentityUser` to include additional properties for the application user.

### Error.razor
This component displays error messages and request IDs when an error occurs.

## Configuration

- **`appsettings.json`**: Contains configuration settings such as pricing for seasons.
- **Dependency Injection**: Services like `IBookingService`, `BookingStateService`, and `VisitorService` are injected into components for use.

## Running the Project

1. **Restore Dependencies**: Run `dotnet restore` to restore all dependencies.
2. **Build the Project**: Run `dotnet build` to build the project.
3. **Run the Project**: Use `dotnet run` to start the application.

## Additional Features

- **SignalR Integration**: The project uses SignalR for real-time updates.
- **Localization**: Dates are formatted using specific cultures (e.g., `nl-BE`).

## Future Enhancements

- **Unit Tests**: Add unit tests for critical components and services.
- **Error Handling**: Improve error handling and user feedback mechanisms.
- **Performance Optimization**: Optimize performance for large datasets and real-time updates.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

## Contributing

Contributions are welcome! Please read the [CONTRIBUTING](CONTRIBUTING.md) guidelines before submitting a pull request.

## Contact

For any questions or feedback, please open an issue on the [GitHub repository](https://github.com/your-repo/OpenSourceCalendar).

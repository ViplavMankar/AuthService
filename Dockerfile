# Use the official .NET 8 runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Expose the port that Render provides
EXPOSE ${PORT}

# Use the .NET 8 SDK to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["AuthService.csproj", "./"]
RUN dotnet restore "AuthService.csproj"

# Create /app/data directory and set permissions
# RUN mkdir -p /app/data && chmod -R 777 /app/data

# Copy all source files and publish the app
COPY . .
RUN dotnet publish "AuthService.csproj" -c Release -o /app/publish

# Build the runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Set the URL to listen on the port provided by Render
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

# Run the application
ENTRYPOINT ["dotnet", "AuthService.dll"]
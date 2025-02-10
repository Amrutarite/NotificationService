# Use official .NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Set the time zone to Asia/Kolkata
ENV TZ=Asia/Kolkata
RUN apt-get update && apt-get install -y tzdata \
    && ln -fs /usr/share/zoneinfo/Asia/Kolkata /etc/localtime \
    && dpkg-reconfigure --frontend noninteractive tzdata

# Expose internal container port 
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY NotificationService/NotificationService.csproj ./NotificationService/
WORKDIR /src/NotificationService
RUN dotnet restore

# Clean obj and bin to prevent duplicate attribute issues
RUN rm -rf obj bin

# Copy the entire project and build
COPY . . 
RUN dotnet publish -c Release -o /app/publish

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# ✅ FIXED: Ensure correct path for NLog.config
COPY --from=build /src/NotificationService/NLog.config /app/NLog.config

# Start the application
ENTRYPOINT ["dotnet", "NotificationService.dll"]

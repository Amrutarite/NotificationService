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

# Correct path for csproj
COPY ["NotificationService/NotificationService.csproj", "./"]
RUN dotnet restore "./NotificationService.csproj"

# Copy everything and build the app
COPY . . 
WORKDIR "/src"
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Ensure NLog.config is copied
COPY ./NotificationService/NLog.config /app/NLog.config
COPY ./NotificationService/NLog.config /app/nlog.config

# Start the application
ENTRYPOINT ["dotnet", "NotificationService.dll"]

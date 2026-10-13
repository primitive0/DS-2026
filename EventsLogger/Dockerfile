FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TextMessaging/TextMessaging.csproj TextMessaging/
COPY EventsLogger/EventsLogger.csproj EventsLogger/
RUN dotnet restore EventsLogger/EventsLogger.csproj
COPY TextMessaging/ TextMessaging/
COPY EventsLogger/ EventsLogger/
RUN dotnet publish EventsLogger/EventsLogger.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "EventsLogger.dll"]

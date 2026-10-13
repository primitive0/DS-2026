FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TextMessaging/TextMessaging.csproj TextMessaging/
COPY Valuator/Valuator.csproj Valuator/
RUN dotnet restore Valuator/Valuator.csproj
COPY TextMessaging/ TextMessaging/
COPY Valuator/ Valuator/
RUN dotnet publish Valuator/Valuator.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Valuator.dll"]

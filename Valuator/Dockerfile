# build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# restore
COPY *.csproj ./
RUN dotnet restore

# build/publish
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# run
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish ./

# слушаем на 8080 внутри контейнера
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Valuator.dll"]

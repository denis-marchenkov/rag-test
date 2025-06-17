FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet restore embedding-service.csproj
RUN dotnet build -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/build .
# Install debugging tools
RUN apt-get update && apt-get install -y --no-install-recommends unzip
RUN curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg

ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 7001
EXPOSE 5002
ENTRYPOINT ["dotnet", "embedding-service.dll"] 
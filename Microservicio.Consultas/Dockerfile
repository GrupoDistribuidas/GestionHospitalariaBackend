FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Microservicio.Consultas/Microservicio.Consultas.csproj", "Microservicio.Consultas/"]
RUN dotnet restore "Microservicio.Consultas/Microservicio.Consultas.csproj"

COPY . ./
WORKDIR /src/Microservicio.Consultas
RUN dotnet publish "Microservicio.Consultas.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 5105
ENTRYPOINT ["dotnet", "Microservicio.Consultas.dll"]

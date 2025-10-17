FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Microservicio.Autenticacion/Microservicio.Autenticacion.csproj", "Microservicio.Autenticacion/"]
RUN dotnet restore "Microservicio.Autenticacion/Microservicio.Autenticacion.csproj"

COPY . ./
WORKDIR /src/Microservicio.Autenticacion
RUN dotnet publish "Microservicio.Autenticacion.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 5101
ENTRYPOINT ["dotnet", "Microservicio.Autenticacion.dll"]

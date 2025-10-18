FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Microservicio.Administracion/Microservicio.Administracion.csproj", "Microservicio.Administracion/"]
RUN dotnet restore "Microservicio.Administracion/Microservicio.Administracion.csproj"

COPY . ./
WORKDIR /src/Microservicio.Administracion
RUN dotnet publish "Microservicio.Administracion.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 5100
ENTRYPOINT ["dotnet", "Microservicio.Administracion.dll"]

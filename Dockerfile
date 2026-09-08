FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY ["Manager.WebAPI/Manager.WebAPI.csproj", "Manager.WebAPI/"]
COPY ["Manager.BusinessLogic/Manager.BusinessLogic.csproj", "Manager.BusinessLogic/"]
COPY ["Manager.DataAccess/Manager.DataAccess.csproj", "Manager.DataAccess/"]
RUN dotnet restore "Manager.WebAPI/Manager.WebAPI.csproj"


COPY . .
WORKDIR "/src/Manager.WebAPI"
RUN dotnet build "Manager.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Manager.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN mkdir -p /app/uploads 
ENTRYPOINT ["dotnet", "Manager.WebAPI.dll"]
﻿FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Movie-Knight/Movie-Knight.csproj", "Movie-Knight/"]
RUN dotnet restore "Movie-Knight/Movie-Knight.csproj"
COPY . .
WORKDIR "/src/Movie-Knight"
RUN dotnet build "Movie-Knight.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Movie-Knight.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Movie-Knight.dll"]

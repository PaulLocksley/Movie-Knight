FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build-settup
WORKDIR /src
RUN curl -sLO https://github.com/tailwindlabs/tailwindcss/releases/latest/download/tailwindcss-linux-x64 && \
   chmod +x tailwindcss-linux-x64 && \
   mv tailwindcss-linux-x64 /usr/local/bin/tailwindcss

FROM build-settup AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Movie-Knight.csproj"
RUN dotnet build "Movie-Knight.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Movie-Knight.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
EXPOSE 4010
ENTRYPOINT ["dotnet", "Movie-Knight.dll"]

#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["FileStreamPOC/FileStreamPOC.csproj", "FileStreamPOC/"]
RUN dotnet restore "FileStreamPOC/FileStreamPOC.csproj"
COPY . .
WORKDIR "/src/FileStreamPOC"
RUN dotnet build "FileStreamPOC.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FileStreamPOC.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FileStreamPOC.dll"]
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy solution + project files first (better layer caching)
COPY Miestamy30.3.sln ./
COPY Miestamy30.3/Miestamy30.3.csproj ./Miestamy30.3/
RUN dotnet restore

# Copy everything else and publish
COPY . ./
RUN dotnet publish Miestamy30.3/Miestamy30.3.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Miestamy30.3.dll"]

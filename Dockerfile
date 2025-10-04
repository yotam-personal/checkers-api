# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project files and restore dependencies
COPY ["ChekersAPI/ChekersAPI.csproj", "ChekersAPI/"]
COPY ["CheckersEngine/CheckersEngine.csproj", "CheckersEngine/"]
RUN dotnet restore "ChekersAPI/ChekersAPI.csproj"

# Copy the rest of the application code
COPY . .
WORKDIR "/src/ChekersAPI"
RUN dotnet build "ChekersAPI.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "ChekersAPI.csproj" -c Release -o /app/publish

# Use the ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy the published app
COPY --from=publish /app/publish .

# Explicitly ensure Properties directory exists and copy key.json
RUN mkdir -p Properties
COPY --from=build /src/ChekersAPI/Properties/key.json ./Properties/

# Set the entry point
ENTRYPOINT ["dotnet", "ChekersAPI.dll"]
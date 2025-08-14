FROM microsoft/dotnet/:8.0-sdk AS build-env
RUN dotnet --info
# This line is now after the FROM instruction

WORKDIR /app
COPY *.sln ./
COPY ChekersAPI/ChekersAPI.csproj ChekersAPI/
COPY CheckersEngine/CheckersEngine.csproj CheckersEngine/
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o out

WORKDIR /app
EXPOSE 5000
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "ChekersAPI.dll"]
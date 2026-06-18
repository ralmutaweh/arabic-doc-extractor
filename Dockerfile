# Stage 1 — Build
# Uses the full SDK image which includes the compiler, NuGet, and build tools
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Stage 2 — Runtime
# Uses the lean ASP.NET runtime image — no compiler, smaller and more secure
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "ArabicPdfReader.dll"]
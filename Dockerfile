FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy everything and build
COPY . .
WORKDIR /app/src/ElBruno.QwenTTS.Web
RUN dotnet restore
RUN dotnet publish -c Release -o /out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .

# Hugging Face Spaces requires the app to run on port 7860
ENV ASPNETCORE_URLS=http://0.0.0.0:7860
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 7860

ENTRYPOINT ["dotnet", "ElBruno.QwenTTS.Web.dll"]

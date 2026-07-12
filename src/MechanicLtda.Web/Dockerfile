# Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner de depuração e como o Visual Studio usa este Dockerfile para criar suas imagens para uma depuração mais rápida.

# Esta fase é usada durante a execução no VS no modo rápido (Padrão para a configuração de Depuração)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root

# ✅ Dependências nativas exigidas pelo QuestPDF/SkiaSharp no Linux (exportação de PDF de orçamentos)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    libfreetype6 \
    fontconfig \
    fonts-dejavu-core \
    curl \
    && fc-cache -f \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Esta fase é usada para compilar o projeto de serviço
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copia os arquivos de projeto para aproveitar o cache de restore
COPY ["src/MechanicLtda.Web/MechanicLtda.Web.csproj", "src/MechanicLtda.Web/"]
COPY ["src/MechanicLtda.Application/MechanicLtda.Application.csproj", "src/MechanicLtda.Application/"]
COPY ["src/MechanicLtda.Domain/MechanicLtda.Domain.csproj", "src/MechanicLtda.Domain/"]
COPY ["src/MechanicLtda.Infrastructure/MechanicLtda.Infrastructure.csproj", "src/MechanicLtda.Infrastructure/"]
COPY ["src/MechanicLtda.Bootstrap/MechanicLtda.Bootstrap.csproj", "src/MechanicLtda.Bootstrap/"]

RUN dotnet restore "src/MechanicLtda.Web/MechanicLtda.Web.csproj"

# Copia todo o código-fonte
COPY . .

WORKDIR "/src/src/MechanicLtda.Web"
RUN dotnet build "MechanicLtda.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase é usada para publicar o projeto de serviço a ser copiado para a fase final
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MechanicLtda.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase é usada na produção ou quando executada no VS no modo normal
FROM base AS final
WORKDIR /app
COPY --from=publish --chown=$APP_UID:$APP_UID /app/publish .
# Declarado explicitamente (mesmo já herdado de "base") para que scanners de
# segurança (ex.: SonarQube S6471) confirmem que a imagem final não roda como root.
USER $APP_UID
ENTRYPOINT ["dotnet", "MechanicLtda.Web.dll"]

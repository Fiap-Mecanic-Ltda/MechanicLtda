# Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner de depuração e como o Visual Studio usa este Dockerfile para criar suas imagens para uma depuração mais rápida.

# Esta fase é usada durante a execução no VS no modo rápido (Padrão para a configuração de Depuração)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root

# ✅ Dependências nativas exigidas pelo QuestPDF/SkiaSharp no Linux
RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    libfreetype6 \
    fontconfig \
    fonts-dejavu-core \
    curl \
    && fc-cache -f \
    && rm -rf /var/lib/apt/lists/*

# Agente APM do New Relic para .NET (instalação oficial via apt, imagem Debian).
# Fica desligado por padrão (CORECLR_ENABLE_PROFILING=0): quem liga é o deployment do
# cluster, junto com NEW_RELIC_LICENSE_KEY e NEW_RELIC_APP_NAME. Assim o docker-compose e
# a execução local seguem sem agente e sem tentar falar com o New Relic.
RUN apt-get update && apt-get install -y --no-install-recommends wget ca-certificates gnupg \
    && echo 'deb [signed-by=/usr/share/keyrings/newrelic-apt.gpg] http://apt.newrelic.com/debian/ newrelic non-free' \
       > /etc/apt/sources.list.d/newrelic.list \
    && wget -qO- https://download.newrelic.com/NEWRELIC_APT_2DAD550E.public \
       | gpg --import --batch --no-default-keyring --keyring /usr/share/keyrings/newrelic-apt.gpg \
    && apt-get update && apt-get install -y --no-install-recommends newrelic-dotnet-agent \
    && apt-get purge -y --auto-remove wget gnupg \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /usr/local/newrelic-dotnet-agent/logs \
    && chown -R $APP_UID /usr/local/newrelic-dotnet-agent/logs

ENV CORECLR_ENABLE_PROFILING=0 \
    CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
    CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
    CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so

USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Esta fase é usada para compilar o projeto de serviço
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copia os arquivos de projeto para aproveitar o cache de restore
COPY ["src/MechanicLtda.API/MechanicLtda.API.csproj", "src/MechanicLtda.API/"]
COPY ["src/MechanicLtda.Application/MechanicLtda.Application.csproj", "src/MechanicLtda.Application/"]
COPY ["src/MechanicLtda.Domain/MechanicLtda.Domain.csproj", "src/MechanicLtda.Domain/"]
COPY ["src/MechanicLtda.Infrastructure/MechanicLtda.Infrastructure.csproj", "src/MechanicLtda.Infrastructure/"]
COPY ["src/MechanicLtda.Bootstrap/MechanicLtda.Bootstrap.csproj", "src/MechanicLtda.Bootstrap/"]

RUN dotnet restore "src/MechanicLtda.API/MechanicLtda.API.csproj"

# Copia todo o código-fonte
COPY . .

WORKDIR "/src/src/MechanicLtda.API"
RUN dotnet build "MechanicLtda.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase é usada para publicar o projeto de serviço a ser copiado para a fase final
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MechanicLtda.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase é usada na produção ou quando executada no VS no modo normal
FROM base AS final
WORKDIR /app
COPY --from=publish --chown=$APP_UID:$APP_UID /app/publish .
# Declarado explicitamente (mesmo já herdado de "base") para que scanners de
# segurança (ex.: SonarQube S6471) confirmem que a imagem final não roda como root.
USER $APP_UID
ENTRYPOINT ["dotnet", "MechanicLtda.API.dll"]
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
WORKDIR /app

RUN apt-get update \
    && apt-get install -y curl 

RUN apt-get update && apt-get install -y tmux

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY src/*.sln .

COPY src/. .
RUN dotnet restore

WORKDIR /source/Dustech.IpMailer.App
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
FROM base AS final
WORKDIR /app
COPY --from=build /app ./
CMD ["bash"]
#ENTRYPOINT ["dotnet", "Dustech.IpMailer.App.dll"]
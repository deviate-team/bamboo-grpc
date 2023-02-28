FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

RUN apt-get update \
  && DEBIAN_FRONTEND=noninteractive \
  apt-get install --no-install-recommends --assume-yes \
  protobuf-compiler-grpc

ENV PROTOBUF_PROTOC=/usr/bin/protoc

WORKDIR /src

COPY bamboo-grpc.csproj .

RUN dotnet restore

COPY . .
RUN dotnet build --verbosity normal
RUN dotnet publish --no-restore -c Release -o /published bamboo-grpc.csproj 

FROM mcr.microsoft.com/dotnet/aspnet:7.0 as runtime
# ENV ASPNETCORE_URLS=https://+:443

WORKDIR /app

COPY --from=build /published .

ENTRYPOINT ["dotnet","bamboo-grpc.dll"]

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

RUN apt-get update \
  && DEBIAN_FRONTEND=noninteractive \
  apt-get install --no-install-recommends --assume-yes \
  protobuf-compiler-grpc

ENV PROTOBUF_PROTOC=/usr/bin/protoc

WORKDIR /app

# copy csproj and restore as distinct layers
COPY bamboo-grpc.csproj .
RUN dotnet restore

# copy everything else and build app
COPY . .
RUN dotnet build --verbosity normal
RUN dotnet publish -o /out/publish

# final stage/image
WORKDIR /out/publish
EXPOSE 5000 

ENTRYPOINT ["dotnet","bamboo-grpc.dll"]

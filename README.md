# BAMBOO gRPC APIs

## Introduction
BAMBOO is a TODO list project built using .NET 7 gRPC, MongoDB, Redis, and Docker. 

## Requirements
To run this project, you need to have the following software installed on your system:
- Docker
- Docker Compose

Getting Started
To get started with BAMBOO, clone the project from GitHub using the following command:
```bash
git clone https://github.com/deviate-team/bamboo-grpc.git
```

Next, navigate to the cloned directory:
```bash
cd bamboo-grpc
```

Then, run the following command to build the Docker images:
```bash
docker-compose build
```

After the images are built, you can start the project using the following command:
```
docker-compose up
```
This will start the MongoDB, Redis, and .NET gRPC server containers.

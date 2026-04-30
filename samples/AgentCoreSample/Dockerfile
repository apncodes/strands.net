# Dockerfile for deploying AgentCoreSample to Amazon Bedrock AgentCore Runtime
#
# AgentCore Runtime requirements:
#   - ARM64 image (linux/arm64)
#   - Port 8080
#   - GET /health returns 200
#   - POST /invocations receives prompts
#
# Build: docker buildx build --platform linux/arm64 -t agentcore-sample:latest .
# Push:  docker push <account>.dkr.ecr.<region>.amazonaws.com/agentcore-sample:latest

FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore from the solution root so project references resolve
COPY ../../ ./
RUN dotnet publish samples/AgentCoreSample/AgentCoreSample.csproj \
    --configuration Release \
    --runtime linux-arm64 \
    --self-contained false \
    --output /app/publish

FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# AgentCore Runtime routes traffic to port 8080
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish ./

USER app
ENTRYPOINT ["dotnet", "AgentCoreSample.dll"]

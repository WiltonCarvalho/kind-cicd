# Build
# docker build -t demo-app . --progress=plain

# Run Demo App
# docker run -it --rm --name demo-app  -p 8080:8080 -p 8081:8081 -e "cors.allowedOrigins=https://cors.allowed1.com,https://cors.allowed2.com" demo-app
# curl -s localhost:8080
# curl -s localhost:8081/actuator/health | jq .
# curl -s localhost:8081/actuator/prometheus

# CORS
# curl -H 'Origin: https://cors.allowed1.com' -i -f http://localhost:8080
# curl -H 'Origin: https://cors.allowed2.com' -i -f http://localhost:8080
# curl -H 'Origin: https://cors.denied.com' -i -f http://localhost:8080 -v
FROM eclipse-temurin:17-jre
USER 999:0
WORKDIR /app
COPY build/extract/dependencies/ ./
COPY build/extract/spring-boot-loader/ ./
COPY build/extract/snapshot-dependencies/ ./
COPY build/extract/application/ ./
ENTRYPOINT ["java", "org.springframework.boot.loader.launch.JarLauncher"]


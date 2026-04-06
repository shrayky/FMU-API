@echo off

docker build -f /mnt/d/DesignSharpC/FmuApi/builds/Dockerfile \
  -t fmu-api:11.4.0 \
  "/mnt/d/DesignSharpC/FmuApi/builds/x64 linux"

pause
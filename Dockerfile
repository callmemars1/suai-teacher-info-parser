FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /teacher-info-parser
COPY . ./
RUN dotnet restore
RUN dotnet publish -c release -o /teacher-info-parser --no-restore

FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /teacher-info-parser
COPY --from=build-env ./teacher-info-parser .
ENV TIME_SPAN 7200
ENV COOKIE 6tp1o0aeh60q1f7n42khc8pphg
ENV DB_NAME=teacher_info
ENV DB_USER=teacher_info_service
ENV DB_PASS=JFDSdkfkd6375sojkfodsjkDF5434246
ENV DB_HOST=178.252.96.108:5431
ENTRYPOINT ["dotnet", "NewParser.dll"]
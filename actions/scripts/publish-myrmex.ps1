param(
    [string]$Registry = "192.168.1.55:5000",
    [string]$Tag = (Get-Date -Format "yyyyMMdd-HHmm")
)

$ErrorActionPreference = "Stop"

dotnet publish .\Myrmex.ApiService\Myrmex.ApiService.csproj `
  -c Release `
  --os linux `
  --arch x64 `
  /t:PublishContainer `
  -p:ContainerRepository=myrmex-api `
  -p:ContainerImageTag=$Tag

docker tag myrmex-api:$Tag $Registry/myrmex-api:$Tag
docker push $Registry/myrmex-api:$Tag

dotnet publish .\Myrmex.WebApp\Myrmex.WebApp.csproj `
  -c Release `
  --os linux `
  --arch x64 `
  /t:PublishContainer `
  -p:ContainerRepository=myrmex-webapp `
  -p:ContainerImageTag=$Tag

docker tag myrmex-webapp:$Tag $Registry/myrmex-webapp:$Tag
docker push $Registry/myrmex-webapp:$Tag

Write-Host ""
Write-Host "Published:"
Write-Host "$Registry/myrmex-api:$Tag"
Write-Host "$Registry/myrmex-webapp:$Tag"
Write-Host ""
Write-Host "Set MYRMEX_TAG=$Tag in staging .env"
param(
    [string]$Registry = "192.168.1.55:5000",
    [string]$Tag = (Get-Date -Format "yyyyMMdd-HHmm")
)

dotnet publish .\Myrmex.ApiService\Myrmex.ApiService.csproj `
  -c Release `
  --os linux `
  --arch x64 `
  /t:PublishContainer `
  -p:ContainerRepository=myrmex-api `
  -p:ContainerImageTag=$Tag

docker tag myrmex-api:$Tag $Registry/myrmex-api:$Tag
docker push $Registry/myrmex-api:$Tag

Write-Host "Published image: $Registry/myrmex-api:$Tag"
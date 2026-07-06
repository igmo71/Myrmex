$registry = "192.168.1.55:5000"
$tag = Get-Date -Format "yyyyMMdd-HHmm"

dotnet publish .\Myrmex.ApiService\Myrmex.ApiService.csproj `
  -c Release `
  --os linux `
  --arch x64 `
  /t:PublishContainer `
  -p:ContainerRepository=myrmex-api `
  -p:ContainerImageTag=$tag

docker tag myrmex-api:$tag $registry/myrmex-api:$tag
docker push $registry/myrmex-api:$tag

dotnet publish .\Myrmex.WebApp\Myrmex.WebApp.csproj `
  -c Release `
  --os linux `
  --arch x64 `
  /t:PublishContainer `
  -p:ContainerRepository=myrmex-webapp `
  -p:ContainerImageTag=$tag

docker tag myrmex-webapp:$tag $registry/myrmex-webapp:$tag
docker push $registry/myrmex-webapp:$tag

Write-Host "Published:"
Write-Host "$registry/myrmex-api:$tag"
Write-Host "$registry/myrmex-webapp:$tag"
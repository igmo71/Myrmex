docker compose pull
docker compose up -d --force-recreate
docker compose ps

docker logs myrmex-api --tail 100
docker logs myrmex-webapp --tail 100
docker logs myrmex-webapp-dataprotection-init --tail 100
docker logs myrmex-aspire-dashboard --tail 100

docker run --rm `
  -v myrmex_webapp_dataprotection:/keys `
  alpine `
  sh -c "ls -la /keys"
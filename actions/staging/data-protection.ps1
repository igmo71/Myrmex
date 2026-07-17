docker compose down

docker run --rm `
  -v myrmex_webapp_dataprotection:/keys `
  alpine `
  sh -c "chown -R 1654:1654 /keys"

docker compose up -d
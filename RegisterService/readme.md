docker run --name my-postgres -e POSTGRES_PASSWORD=mysecretpassword -p 5432:5432 postgres
docker run -d -p 9092:9092 --network host --name broker apache/kafka:latest
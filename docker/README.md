# FreeSO Server Docker Setup

Runs the FreeSO server and MariaDB database via Docker Compose.

## Usage

Run from the repository root `FreeSO/`:

```bash
docker compose -f docker/docker-compose.yml up --build -d
```

Stop the server:

```bash
docker compose -f docker/docker-compose.yml down
```

## Configuration

Edit `docker/config.json` before starting:

- **`secret`** - Leave as `GENERATE` for auto-generation by the container, or set your own hex string
- **`public_host`** - Change if hosting remotely (defaults to `localhost`)
- **`database.connectionString`** - Update if you changed MariaDB credentials in `docker-compose.yml`

Update `docker-compose.yml` to point to your local TSO client installation:

Windows:
```yaml
- C:/Path/To/Your/TSOClient:/game:ro
```
MacOS:
```yaml
- ~/Documents/The Sims Online/TSOClient:/game:ro
```


## What's Running

- **FreeSO server** - Game server on ports 9000 (API), 33100-33101 (city), 34100-34101 (lots), 35100-35101 (tasks)
- **MariaDB 11** - Database with persistent storage in a Docker volume

The database is automatically initialized on first run.

Connect to the server in game via default ip: `http://localhost:9000` (or your public IP if hosting remotely).

## Requirements

- The Sims Online client files
- Ports 9000, 33100-33101, 34100-34101, 35100-35101 available

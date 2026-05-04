## Build

### macOS

1. Navigate to the `FSO.Unix` folder.
2. Run the following command in Terminal to build a release publish:

```bash
dotnet publish -c Release -r osx-arm64 --self-contained true
```

This will generate a .app bundle in: bin/Release/net9.0/osx-arm64/publish folder.
You can then copy the app to your Applications folder.

### Linux

1. Navigate to the `FSO.Unix` folder.
2. Run the following command to build:

```bash
dotnet build -r linux-x64
```

Or to publish a self-contained release:

```bash
dotnet publish -c Release -r linux-x64 --self-contained true
```

## Deploy

To build, install, and launch in one step:

```bash
./deploy.sh
```

This script works on both macOS and Linux:
- **macOS**: Installs to `/Applications/FreeSO.app` and opens the app
- **Linux**: Installs to `~/.local/share/FreeSO` and creates a desktop entry

## Launch 3D

### macOS

To open the app in 3D mode, run the open command with `--args -3d`:

E.g when the application is placed in the Applications folder, you can run:

```bash
open /Applications/FreeSO.app --args -3d
```

### Linux

```bash
./FreeSO -3d
```

## Troubleshooting

### Deploy Script Permissions

If you get a permission denied error when running `deploy.sh`, make it executable:

```bash
chmod +x deploy.sh
```

# Raspberry Pi 5 deployment

The face (raylib, rendering straight to the display over DRM/KMS) runs as the
`face` service in [docker-compose.yml](docker-compose.yml), alongside `brain`
and `voice`. It needs 64-bit Raspberry Pi OS, a connected display and no
desktop session using it (no display manager running).

Raylib 6.0 is compiled inside [face/Dockerfile](face/Dockerfile) with
`PLATFORM_DRM`, so nothing needs to be built by hand on the Pi.

The image is built for arm64 by the
[face-image workflow](.github/workflows/face-image.yml) on every push to `main`
that touches `face/`, and published to Docker Hub as `<user>/blinky-face`. Deploy it
with:

```sh
scp docker-compose.yml pi@<pi-host>:blinky/
ssh pi@<pi-host> 'cd ~/blinky && docker compose pull face && docker compose up -d face'
```

To build on the Pi instead (slow, 10-20 minutes and a few GB of disk), copy
`face/Dockerfile`, `face/.dockerignore` and `face/src` (without `bin/` and
`obj/`) to `~/blinky/face/` and run `docker compose build face`.

The container gets `/dev/dri` and `/dev/i2c-1` (PCA9685 servo board) and the
`video`, `render` and `i2c` groups by numeric gid. If your Pi's gids differ,
update `group_add` (`getent group video render i2c`). Servo angles persist in
the `face-data` volume. The container restarts automatically, including at
boot.

The API listens on port 5000 and `brain` reaches it at `http://face:5000`.
Check it with `curl http://<pi-host>:5000/health`. The emotion endpoints have
no authentication, so restrict access to a trusted network.

The `voice` service (Piper text-to-speech, source in [mouth/](mouth)) is
published the same way by the
[mouth-image workflow](.github/workflows/mouth-image.yml) as
`<user>/blinky-voice`. The image contains only `app.py`; the voice model is
mounted from `~/blinky/mouth/voices/` on the Pi (it is not in git). Deploy with
`docker compose pull voice && docker compose up -d voice`.

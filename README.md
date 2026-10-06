# Raspberry Pi 5 deployment

This app needs 64-bit Raspberry Pi OS and a connected display. The DRM build
below can be launched over SSH without a graphical login, provided no desktop
session is using the display. The .NET runtime is included in the publish output.

On the development machine, publish and copy the output directory to the Pi:

```sh
dotnet publish face.csproj -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true -o bin/Release/net8.0/linux-arm64/publish
ssh pi@<pi-host> 'mkdir -p ~/face'
scp -r bin/Release/net8.0/linux-arm64/publish/. pi@<pi-host>:~/face/
```

`raylib-cs` 8.0.0 does not ship a Linux ARM64 native library. On the Pi, build
the matching raylib **6.0** shared library (once) and place it where the
`raylib-cs` resolver looks for Linux ARM64 libraries:

```sh
sudo apt-get update
sudo apt-get install build-essential git libdrm-dev libgbm-dev libegl1-mesa-dev libgles2-mesa-dev libasound2-dev
git clone --depth 1 --branch 6.0 https://github.com/raysan5/raylib.git ~/raylib-6
make -C ~/raylib-6/src clean
make -C ~/raylib-6/src -j2 PLATFORM=PLATFORM_DRM RAYLIB_LIBTYPE=SHARED
mkdir -p ~/face/runtimes/linux-arm64/native
cp -L ~/raylib-6/src/libraylib.so ~/face/libraylib.so
cp -L ~/raylib-6/src/libraylib.so ~/face/runtimes/linux-arm64/native/libraylib.so
```

Before launching, check the native library on the Pi:

```sh
file ~/face/face ~/face/libraylib.so
ldd ~/face/libraylib.so
```

Both files must report `aarch64`/`ARM aarch64`. If `libraylib.so` is missing,
repeat the `make` and `cp` commands above. If `ldd` reports `not found` for a
dependency, install that library on the Pi before launching.

From an SSH session on the Pi, stop the desktop for this boot and launch:

```sh
sudo systemctl stop display-manager
cd ~/face
chmod +x face
LD_LIBRARY_PATH="$PWD${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}" ./face --urls http://127.0.0.1:5000
```

To return to the desktop after stopping `face`, run
`sudo systemctl start display-manager`. SSH should remain available while the
desktop is stopped. Neither command changes the next boot's desktop setting.

Check `http://127.0.0.1:5000/health` on the Pi. To control it from another
machine, bind to a reachable address with `--urls http://0.0.0.0:5000` and
restrict access to a trusted network; the emotion endpoint has no authentication.
The DRM renderer needs a connected display and permission to access `/dev/dri`.
It will not draw in your SSH terminal. If you instead want to keep the Pi's
desktop running, rebuild raylib with `PLATFORM=PLATFORM_DESKTOP` after
`make -C ~/raylib-6/src clean`, install the X11 development packages, and
launch against the desktop's display rather than using the DRM library.

## Start at boot

Copy [face.service](face.service) to the Pi (alongside the published app):

```sh
scp face.service pete@192.168.1.89:~/face/face.service
```

The unit assumes the Pi user is `pete`, the executable and native library are
in `/home/pete/face`, and the DRM build is installed. Adjust the paths and
user in the unit if yours differ. On the Pi, stop any manually launched `face`
process first, then disable the desktop and enable the service:

```sh
sudo systemctl set-default multi-user.target
sudo systemctl disable --now display-manager
sudo install -m 644 ~/face/face.service /etc/systemd/system/face.service
sudo systemctl daemon-reload
sudo systemctl enable --now face.service
sudo systemctl status face.service
```

After reboot, check `sudo journalctl -u face.service -b --no-pager` for startup
logs. Use `sudo systemctl stop face.service` to stop the face and
`sudo systemctl start face.service` to run it again; disable autostart with
`sudo systemctl disable --now face.service`. The service listens on localhost
only, as in the manual launch command.
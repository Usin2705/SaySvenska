#!/bin/bash
# assume that your user is already linger with
# sudo loginctl enable-linger USERNAME

echo "Stop current container and pod..."
systemctl --user stop pod-saysvenska.service
systemctl --user disable pod-saysvenska.service
systemctl --user disable container-saysvenska-server.service

podman pod stop saysvenska
podman pod rm saysvenska
podman container rm saysvenska-server

echo "Rebuild new container..."
podman build --pull --rm -f "Dockerfile" -t saysvenska:latest "."

echo "Rebuild container and pod..."
podman pod create --name saysvenska -p 52705:52705
podman run -d --pod=saysvenska --name saysvenska-server saysvenska

echo "Recreate systemctl service..."
cd $HOME
podman generate systemd --new --name saysvenska -f
mkdir -p ~/.config/systemd/user/
mv -v pod-saysvenska.service ~/.config/systemd/user/
mv -v container-saysvenska-server.service ~/.config/systemd/user/

echo "Enable systemd service"
systemctl --user daemon-reload
systemctl --user enable pod-saysvenska.service
systemctl --user enable container-saysvenska-server.service

echo "Check the status of the systemd service"
systemctl --user status pod-saysvenska.service
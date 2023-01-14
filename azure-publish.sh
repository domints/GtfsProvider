#!/bin/bash
cd ~/deploy_temp/gtfs && \
echo "Stopping GTFS service..." && \
sudo /usr/bin/systemctl_stop gtfs && \
echo "GTFS service stopped. Removing old app" && \
rm -rf /var/dotnet/gtfs/* && \
echo "Old version removed. Unzipping artifacts" && \
tar -xzf artifact.tar.gz -C /var/dotnet/gtfs && \
echo "Artifacts unzipped. Starting GTFS service..." && \
sudo /usr/bin/systemctl_start gtfs && \
echo "Deployment script finished!"
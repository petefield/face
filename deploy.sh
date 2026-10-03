dotnet publish face.csproj -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o  bin/Release/net8.0/linux-arm64/publish
ssh pete@blinky.local 'sudo systemctl stop face.service'
scp -r ./bin/Release/net8.0/linux-arm64/publish/. pete@blinky.local:~/face/
scp ./face.service pete@blinky.local:/tmp/face.service
ssh pete@blinky.local 'sudo mv /tmp/face.service /etc/systemd/system/face.service && sudo systemctl daemon-reload'
ssh pete@blinky.local 'sudo systemctl start face.service'

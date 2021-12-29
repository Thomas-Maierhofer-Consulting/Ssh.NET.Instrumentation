FROM mcr.microsoft.com/dotnet/sdk:3.1

LABEL description="SSH Server for SSH Integration Tests"
LABEL maintainer="Thomas Maierhofer <thomas@maierhofer.de>"

RUN apt update && apt install  openssh-server sudo -y

RUN useradd -rm -d /home/ubuntu -s /bin/bash -g root -G sudo -u 1000 test 

RUN  echo 'test:test' | chpasswd

RUN service ssh start

EXPOSE 22

CMD ["/usr/sbin/sshd","-D"]
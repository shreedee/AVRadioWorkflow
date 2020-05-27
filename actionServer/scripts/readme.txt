To debuug a script 
Docker-compose up with command:startandwait

docker exec -it THECONTAINER_scriptsholder_1 bash

npm run debug -- ./src/test.ts

visual studio debugging still sucks,, better to use chrome
so -> chrome://inspect/#devices
and open dedicated DevTools

To typecheck while coding
just type "tsc --watch"

to create ssh key 
ssh-keygen -C "newadmintest@dev.local"

also restric it ssh to IPs actually that needs it
https://blog.tinned-software.net/restrict-ssh-logins-using-ssh-keys-to-a-particular-ip-address/

in .ssh/authrorizedkeys

from="117.196.156.96" ssh-rsa XXXXXXXXXX


To create tunel

in case of appliance create in appliance docker-compose

  ssh-connector:
    image: montefuscolo/ssh
    environment:
      - AUTHORIZED_KEYS=ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDTvSQX2gvn56XQWrdW1hwxWBp3a+oRVnDxpRHgmAGncKNkgUs2+C28sqk/m02dv1VY/yL8ArI7dPpTTeCyt6IJr8UtRNrgsMTIlSgUdW3mrqpog//kptXR1A+0pky0nyFlBQSzq/qktNS3cP8/dZi4C2NPo08R5iqRr5/8Q30RhXorGhntWwk4N9LCoImEx1UjkClm20BLny2IDonePiCIW+EKR06pHIMfXLQm4pIQaFyE2t9Bcd/N9Jn8INwYL7CzwwMv+erFvJT6ONou3lw8kq5GGGOOXdhnZw7av1pO+jxGcvYsf/EtA73D8V9rjS5/JYQpFSHlqaRR0r7INNGl newadmintest@dev.local
    ports:
      - 8023:22/tcp
    networks:
      - backend   


cp /root/.ssh/id_rsa_toCopy /root/.ssh/id_rsa
chmod 400 /root/.ssh/id_rsa
ssh -4  -L 9201:elasticsearch:9200 -N -f -p 8023  159.89.165.236
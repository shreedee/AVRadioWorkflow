FROM node:11.4.0-stretch

EXPOSE 9222

RUN npm install -g typescript ts-node-dev \
    && apt-get update \
    && apt-get install -y --no-install-recommends \
        openssh-client ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY ./scripts/ /scripts/
WORKDIR /scripts

RUN npm install && tsc

ENTRYPOINT ["npm","run"]

CMD ["check"]
#CMD tail -f /dev/null
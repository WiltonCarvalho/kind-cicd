FROM node:18-slim

ENV ORACLE_HOME=/opt/oracle/instantclient_19_10
ENV PATH="$PATH:$ORACLE_HOME"
ENV LD_LIBRARY_PATH="$ORACLE_HOME"
ENV TNS_ADMIN="$ORACLE_HOME/network/admin/"

WORKDIR /usr/src/app

COPY package*.json ./

RUN set -ex \
    && apt-get -q update \
    && apt-get -q -y install --no-install-recommends tzdata ca-certificates curl libaio1 unzip \
    && rm -rf /var/lib/apt/lists/*

RUN set -ex \
    && mkdir -p /opt/oracle \
    && cd /opt/oracle \
    && curl -fsSL https://download.oracle.com/otn_software/linux/instantclient/191000/instantclient-basiclite-linux.x64-19.10.0.0.0dbru.zip -o instantclient-basiclite-linux.zip \
    && curl -fsSL https://download.oracle.com/otn_software/linux/instantclient/191000/instantclient-sqlplus-linux.x64-19.10.0.0.0dbru.zip -o instantclient-sqlplus-linux.zip \
    && unzip -o instantclient-basiclite-linux.zip \
    && unzip -o instantclient-sqlplus-linux.zip \
    && rm -f instantclient-basiclite-linux.zip instantclient-sqlplus-linux.zip \
    && echo /opt/oracle/instantclient_19_10 > /etc/ld.so.conf.d/oracle-instantclient.conf \
    && ln -s /usr/lib/x86_64-linux-gnu/libaio.so.1 $ORACLE_HOME/libaio.so.1 \
    && ldconfig \
    && echo quit | sqlplus /nolog

RUN npm install

COPY . .

EXPOSE 3000
CMD [ "node", "server.js" ]

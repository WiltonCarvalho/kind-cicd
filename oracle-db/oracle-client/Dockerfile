FROM container-registry.oracle.com/database/free:23.26.4.0

ENV TNS_ADMIN="$ORACLE_HOME/network/admin/"

RUN set -ex \
    && echo quit | sqlplus /nolog \
    && orapki help

COPY tnsnames.ora sqlnet.ora $TNS_ADMIN

USER root
HEALTHCHECK NONE
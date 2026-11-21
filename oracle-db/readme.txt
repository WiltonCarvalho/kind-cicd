docker run -it --rm --name oracle-db \
-e ORACLE_PDB=FREEPDB1 \
-e ORACLE_PWD=passw0rd \
-p 1521:1521 \
-v $PWD/human_resources:/tmp/human_resources \
container-registry.oracle.com/database/free:23.26.4.0-lite

docker exec -it oracle-db bash

sqlplus sys/passw0rd@//localhost:1521/FREEPDB1 as sysdba

sqlplus sys/passw0rd@//localhost:1521/FREEPDB1 as sysdba @/tmp/human_resources/hr_install.sql

sqlplus hr/hr_passw0rd@//localhost:1521/FREEPDB1

SELECT table_name FROM user_tables;

SELECT COUNT(*) FROM employees;
SELECT manager_id, department_id, department_name FROM departments WHERE manager_id = 103;

docker build -t app nodejs-app --progress=plain

docker run -it --rm --name app --net=host \
-e DB_USER="hr" \
-e DB_PASSWORD="hr_passw0rd" \
-e DB_CONNECT_STRING="localhost:1521/FREEPDB1" \
app:latest
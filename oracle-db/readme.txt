docker run -it --rm --name oracle-db \
-e ORACLE_PDB=TEST \
-e ORACLE_PWD=passw0rd \
-p 1521:1521 \
-v $PWD/human_resources:/tmp/human_resources \
container-registry.oracle.com/database/free:23.5.0.0-lite

docker exec -it oracle-db bash

sqlplus sys/passw0rd@//localhost:1521/TEST as sysdba

@/tmp/human_resources/hr_install.sql

test123456

sqlplus hr/test123456@//localhost:1521/TEST

SELECT table_name FROM user_tables;

SELECT COUNT(*) FROM employees;
SELECT manager_id, department_id, department_name FROM departments WHERE manager_id = 103;
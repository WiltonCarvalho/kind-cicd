const oracledb = require('oracledb');

oracledb.outFormat = oracledb.OUT_FORMAT_OBJECT;

async function run() {

    const connection = await oracledb.getConnection({
        user          : process.env.DB_USER,
        password      : process.env.DB_PASSWORD,
        connectString : process.env.DB_CONNECT_STRING
    });

    const result = await connection.execute(
        `SELECT manager_id, department_id, department_name
         FROM departments
         WHERE manager_id = :id`,
        [103],  // bind value for :id
    );

    console.log(result.rows);
    await connection.close();
}

run();
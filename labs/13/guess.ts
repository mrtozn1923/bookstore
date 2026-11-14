import sql from "mssql";

const pool = await new sql.ConnectionPool(process.env.DB_CONNECTION_STRING!).connect();

// A EUR edition priced 19.99 in a decimal(10,2) column; how many cents does the API compute?
const { recordset } = await pool.request().query("SELECT CAST(19.99 AS decimal(10, 2)) AS Price");
const price = recordset[0].Price;
console.log(typeof price, price);
console.log(Math.trunc(price * 100));

await pool.close();

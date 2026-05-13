import { closeMysqlPool, initializeMysqlSchema } from "../src/db/mysql.js";

async function main() {
  await initializeMysqlSchema();
  console.log("✅ MySQL schema initialized successfully");
}

main()
  .catch((error) => {
    console.error("❌ Failed to initialize MySQL schema:", error.message);
    process.exitCode = 1;
  })
  .finally(async () => {
    await closeMysqlPool();
  });

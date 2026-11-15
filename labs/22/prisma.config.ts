// Prisma 7 reads its settings from here; run the CLI with the variables loaded, e.g. node --env-file=.env
import { defineConfig } from "prisma/config";

export default defineConfig({
    schema: "prisma/schema.prisma",
    datasource: { url: process.env.DATABASE_URL ?? "" },
});

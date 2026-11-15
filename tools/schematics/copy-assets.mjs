import { cpSync } from 'node:fs';

// tsc copies only .ts output; templates, schema and collection are copied as-is.
cpSync('src/collection.json', 'dist/collection.json');
cpSync('src/api-service/schema.json', 'dist/api-service/schema.json');
cpSync('src/api-service/files', 'dist/api-service/files', { recursive: true });

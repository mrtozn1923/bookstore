// Forwards /api calls from the Angular dev server to the catalog API.
// Under Aspire, WithReference(catalog) sets CATALOG_HTTPS / CATALOG_HTTP.
const target = process.env.CATALOG_HTTPS ?? process.env.CATALOG_HTTP ?? 'http://localhost:5080';

module.exports = {
  '/api': { target, secure: false, changeOrigin: true },
};

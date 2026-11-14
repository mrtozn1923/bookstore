// Forwards /api calls from the Angular dev server to the gateway.
// Under Aspire, WithReference(gateway) sets GATEWAY_HTTPS / GATEWAY_HTTP.
const target = process.env.GATEWAY_HTTPS ?? process.env.GATEWAY_HTTP ?? 'http://localhost:5070';

module.exports = {
  '/api': { target, secure: false, changeOrigin: true },
};

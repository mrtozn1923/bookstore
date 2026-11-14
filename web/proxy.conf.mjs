// ng serve proxy: /api/books -> <target>/books, so the browser stays on one origin (no CORS in development).
// API_PROXY_TARGET is set by Compose (http://bookstore-api:8080); dotnet run on the host listens on 5080
export default {
  '/api': {
    target: process.env.API_PROXY_TARGET ?? 'http://localhost:5080',
    pathRewrite: { '^/api': '' },
  },
};

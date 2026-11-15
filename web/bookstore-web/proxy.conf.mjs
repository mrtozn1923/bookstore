// Dev server forwards /api calls to the local Bookstore.Api.
export default {
  '/api': {
    target: `http://127.0.0.1:${process.env.BOOKSTORE_API_PORT ?? '5080'}`,
    secure: false,
  },
};

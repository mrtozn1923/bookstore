import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  // The create-next-app defaults since 16.4: caching is opt-in with 'use cache', everything else renders per request
  cacheComponents: true,
  partialPrefetching: true,
  // This app lives inside the Vite project, which has its own package-lock.json
  turbopack: { root: __dirname },
};

export default nextConfig;

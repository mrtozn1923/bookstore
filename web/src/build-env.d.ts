// Values replaced at build time by the "define" option in angular.json
declare const BOOKSTORE_VERSION: string;

// "loader": { ".svg": "file" } turns an SVG import into the URL of the emitted file
declare module '*.svg' {
  const url: string;
  export default url;
}

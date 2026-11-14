// Simulates two third-party origins (a web font service and an analytics vendor) on one local port.
// Chromium maps *.thirdparty.test to 127.0.0.1 via --host-resolver-rules (see browser.mjs).
// Usage: node third-party.mjs   (PORT=5302, DELAY_MS=250 = response time of a distant server)
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';

const port = Number(process.env.PORT ?? 5302);
const delayMs = Number(process.env.DELAY_MS ?? 250);
const fontDir = new URL('./node_modules/@fontsource/inter/files/', import.meta.url);
const ranges = {
  'latin-ext': 'U+0100-02BA, U+02BD-02C5, U+02C7-02CC, U+02CE-02D7, U+02DD-02FF, U+0304, U+0308, U+0329, U+1D00-1DBF, U+1E00-1E9F, U+1EF2-1EFF, U+2020, U+20A0-20AB, U+20AD-20C0, U+2113, U+2C60-2C7F, U+A720-A7FF',
  latin: 'U+0000-00FF, U+0131, U+0152-0153, U+02BB-02BC, U+02C6, U+02DA, U+02DC, U+0304, U+0308, U+0329, U+2000-206F, U+20AC, U+2122, U+2191, U+2193, U+2212, U+2215, U+FEFF, U+FFFD',
};

// Like a real analytics tag: a sizeable script that runs synchronously, then sends a hit
const analyticsJs = `(function () {
  var config = ${JSON.stringify(Array.from({ length: 1500 }, (_, i) => ({ id: i, rule: 'event_' + i + '_'.repeat(20) })))};
  var start = performance.now();
  while (performance.now() - start < 60) { config.sort(function () { return 0; }); }
  navigator.sendBeacon('http://analytics.thirdparty.test:${port}/collect', JSON.stringify({ page: location.pathname }));
})();`;

function fontCss(url) {
  const weights = (url.searchParams.get('family') ?? 'Inter:wght@400').split('@')[1].split(';');
  const display = url.searchParams.get('display');
  return weights.flatMap((w) => Object.entries(ranges).map(([subset, range]) => `/* ${subset} */
@font-face {
  font-family: 'Inter';
  font-style: normal;
  font-weight: ${w};${display ? `\n  font-display: ${display};` : ''}
  src: url(http://fonts.thirdparty.test:${port}/files/inter-${subset}-${w}-normal.woff2) format('woff2');
  unicode-range: ${range};
}`)).join('\n');
}

createServer(async (req, res) => {
  const url = new URL(req.url, `http://${req.headers.host}`);
  await new Promise((r) => setTimeout(r, delayMs));
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Timing-Allow-Origin', '*');
  if (url.pathname === '/css2') {
    res.writeHead(200, { 'Content-Type': 'text/css' }).end(fontCss(url));
  } else if (url.pathname.startsWith('/files/')) {
    const file = await readFile(new URL(url.pathname.slice(7), fontDir)).catch(() => null);
    res.writeHead(file ? 200 : 404, { 'Content-Type': 'font/woff2' }).end(file);
  } else if (url.pathname === '/analytics.js') {
    res.writeHead(200, { 'Content-Type': 'text/javascript' }).end(analyticsJs);
  } else {
    res.writeHead(204).end();
  }
}).listen(port, '127.0.0.1', () => console.log(`third-party simulator on 127.0.0.1:${port}, delay ${delayMs} ms`));

// Usage: node scripts/symbolicate.mjs report.json
// Maps the minified stack of an error report back to the TypeScript sources, the way an
// error tracking service does: JS file -> debugId -> source map -> original position.
import { readdirSync, readFileSync } from 'node:fs';
import { SourceMap } from 'node:module';
import { join } from 'node:path';

const browserDir = 'dist/bookstore-web/browser';
const mapsDir = 'dist/bookstore-web/source-maps';

const mapsByDebugId = new Map();
for (const file of readdirSync(mapsDir).filter((name) => name.endsWith('.js.map'))) {
  const payload = JSON.parse(readFileSync(join(mapsDir, file), 'utf8'));
  mapsByDebugId.set(payload.debugId, new SourceMap(payload));
}

function debugIdOf(jsFile) {
  const match = readFileSync(join(browserDir, jsFile), 'utf8').match(/\/\/# debugId=([0-9a-f-]+)/);
  return match?.[1];
}

const report = JSON.parse(readFileSync(process.argv[2], 'utf8'));
const frames = report.stack
  .split('\n')
  .map((line) => line.match(/at (?:(.+?) \()?.*\/([^/]+\.js):(\d+):(\d+)\)?$/))
  .filter((frame) => frame !== null)
  .map(([, fn = '<anonymous>', jsFile, line, column]) => ({
    fn,
    generated: `${jsFile}:${line}:${column}`,
    origin: mapsByDebugId.get(debugIdOf(jsFile))?.findOrigin(Number(line), Number(column)),
  }));

console.log(report.message);
frames.forEach((frame, index) => {
  // The name in the map belongs to the identifier being called at this position, so the
  // function a frame runs in is named by the call site one frame further down
  const name = frames[index + 1]?.origin?.name ?? frame.fn;
  const where = frame.origin?.fileName
    ? `${frame.origin.fileName.replace(/^(\.\.\/)+/, '')}:${frame.origin.lineNumber}:${frame.origin.columnNumber}`
    : `${frame.generated} [no source map]`;
  console.log(`  at ${name} (${where})`);
});

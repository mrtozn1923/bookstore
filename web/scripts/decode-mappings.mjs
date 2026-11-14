// Usage: node scripts/decode-mappings.mjs <file.js.map> [count]
// Decodes the Base64 VLQ "mappings" field by hand and prints the first segments.
import { readFileSync } from 'node:fs';

const BASE64 = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';

function decodeVlq(segment) {
  const values = [];
  let value = 0;
  let shift = 0;
  for (const char of segment) {
    const digit = BASE64.indexOf(char);
    value += (digit & 31) << shift;
    if (digit & 32) {
      shift += 5;
      continue;
    }
    values.push(value & 1 ? -(value >> 1) : value >> 1);
    value = 0;
    shift = 0;
  }
  return values;
}

const map = JSON.parse(readFileSync(process.argv[2], 'utf8'));
let count = Number(process.argv[3] ?? 8);
const state = [0, 0, 0, 0, 0];
map.mappings.split(';').forEach((line, lineIndex) => {
  state[0] = 0;
  for (const segment of line.split(',').filter(Boolean)) {
    const fields = decodeVlq(segment);
    fields.forEach((delta, index) => (state[index] += delta));
    if (lineIndex === 0 && fields.length >= 4 && count-- > 0) {
      const name = fields.length === 5 ? ` name=${map.names[state[4]]}` : '';
      console.log(
        `${segment.padEnd(8)} [${fields.join(',')}]`.padEnd(30) +
          `-> js 1:${state[0] + 1}  ${map.sources[state[1]].replace(/^(\.\.\/)+/, '')}:${state[2] + 1}:${state[3] + 1}${name}`,
      );
    }
  }
});

// Generates large sample cover images (1200x1800 JPEG) for the first 12 classics.
// Usage: node make-covers.mjs  -> ../api/Bookstore.Api/Covers/*.jpg
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';

const books = [
  ['Dune', 'Frank Herbert', '#c2410c', '#fbbf24'],
  ['Emma', 'Jane Austen', '#be185d', '#f9a8d4'],
  ['The Hobbit', 'J. R. R. Tolkien', '#15803d', '#bef264'],
  ['Middlemarch', 'George Eliot', '#4338ca', '#a5b4fc'],
  ['Frankenstein', 'Mary Shelley', '#334155', '#94a3b8'],
  ['Pride and Prejudice', 'Jane Austen', '#9d174d', '#fda4af'],
  ['Moby-Dick', 'Herman Melville', '#0369a1', '#7dd3fc'],
  ['Jane Eyre', 'Charlotte Bronte', '#6d28d9', '#c4b5fd'],
  ['War and Peace', 'Leo Tolstoy', '#991b1b', '#fca5a5'],
  ['Dracula', 'Bram Stoker', '#1f2937', '#f87171'],
  ['The Odyssey', 'Homer', '#0f766e', '#5eead4'],
  ['Don Quixote', 'Miguel de Cervantes', '#a16207', '#fde68a'],
];
const slug = (t) => t.toLowerCase().replace(/[^a-z0-9]/g, '-');
const outDir = new URL('../api/Bookstore.Api/Covers/', import.meta.url);
await mkdir(outDir, { recursive: true });

const browser = await chromium.launch({ args: ['--use-mock-keychain'] });
const page = await browser.newPage({ viewport: { width: 1200, height: 1800 } });
for (const [index, [title, author, dark, light]] of books.entries()) {
  await page.setContent(`<body style="margin:0">
<svg width="1200" height="1800" xmlns="http://www.w3.org/2000/svg">
  <filter id="grain"><feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="3" seed="${index + 1}"/>
    <feColorMatrix values="0 0 0 0 1  0 0 0 0 1  0 0 0 0 1  0 0 0 0.35 0"/></filter>
  <filter id="clouds"><feTurbulence type="turbulence" baseFrequency="0.004" numOctaves="5" seed="${index + 7}"/></filter>
  <defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="${light}"/><stop offset="1" stop-color="${dark}"/></linearGradient></defs>
  <rect width="1200" height="1800" fill="url(#g)"/>
  <rect width="1200" height="1800" filter="url(#clouds)" opacity="0.45"/>
  <rect width="1200" height="1800" filter="url(#grain)"/>
  <rect x="80" y="1180" width="1040" height="460" rx="24" fill="#0f172a" opacity="0.78"/>
  <text x="600" y="1390" font-family="Georgia, serif" font-size="${title.length > 14 ? 92 : 128}" fill="#fff" text-anchor="middle">${title}</text>
  <text x="600" y="1520" font-family="Georgia, serif" font-size="60" fill="${light}" text-anchor="middle">${author}</text>
</svg></body>`);
  await page.screenshot({ path: new URL(`${slug(title)}.jpg`, outDir).pathname, type: 'jpeg', quality: 92 });
  console.log(`${slug(title)}.jpg`);
}
await browser.close();

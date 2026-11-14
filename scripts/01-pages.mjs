import { open } from './browser.mjs';

for (const file of ['catalog.html', 'book.html']) {
    const { browser, page, requests } = await open(file);
    const fonts = requests.filter(r => r.endsWith('.woff2')).map(r => r.split('/').pop());
    const faces = await page.evaluate(() => [...document.fonts]
        .map(f => `${f.style}/${f.unicodeRange.startsWith('U+0-FF') ? 'latin' : 'latin-ext'}: ${f.status}`));
    console.log(`${file.padEnd(13)} requested: ${fonts.join(', ')}`);
    console.log(`${''.padEnd(13)} FontFace status: ${faces.join(' | ')}`);
    await browser.close();
}

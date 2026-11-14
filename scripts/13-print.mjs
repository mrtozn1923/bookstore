import { open, engines, ORIGIN } from './browser.mjs';
import { readFile } from 'node:fs/promises';

// 1) both engines: what does the print media query change?
for (const engine of engines) {
    const { browser, page } = await open('invoice.html', { engine });
    await page.emulateMedia({ media: 'print' });
    const r = await page.evaluate(() => {
        const cs = (sel, pseudo) => getComputedStyle(document.querySelector(sel), pseudo);
        return `header display ${cs('.site-header').display}, print button ${cs('.print-button').display}, `
            + `link ::after ${cs('.invoice-note a', '::after').content}, .status print-color-adjust ${cs('.status').printColorAdjust}`;
    });
    console.log(`--- ${engine} (print media)\n${r}`);
    await browser.close();
}

// 2) Chromium only: a real PDF (page.pdf), read back with pdf.js
const { browser, page } = await open('lab/13-table.html');
// pdf.js from node_modules, served under the same fake origin
await page.route(`${ORIGIN}/pdfjs/**`, async route => {
    const file = new URL(route.request().url()).pathname.replace('/pdfjs/', '');
    await route.fulfill({ body: await readFile(`node_modules/pdfjs-dist/build/${file}`), contentType: 'text/javascript' });
});

async function pdfOf(file, css = '') {
    const p = await browser.newPage();
    await p.route(`${ORIGIN}/**`, async route => {
        const url = new URL(route.request().url());
        try {
            await route.fulfill({ body: await readFile(`web${decodeURIComponent(url.pathname)}`) });
        } catch {
            await route.fulfill({ status: 404, body: '' });
        }
    });
    await p.goto(`${ORIGIN}/${file}`);
    if (css) await p.addStyleTag({ content: css });
    // printBackground stays false: the default of the print dialog ("Background graphics" off)
    const pdf = await p.pdf({ preferCSSPageSize: true });
    await p.close();
    return pdf.toString('base64');
}

// pages, page size in points, the text of every page, and light green pixels (the "Paid" badge) on page 1
async function readPdf(base64) {
    return page.evaluate(async data => {
        const pdfjs = await import('/pdfjs/pdf.mjs');
        pdfjs.GlobalWorkerOptions.workerSrc = '/pdfjs/pdf.worker.mjs';
        const doc = await pdfjs.getDocument({ data: Uint8Array.from(atob(data), c => c.charCodeAt(0)) }).promise;
        const pages = [];
        for (let n = 1; n <= doc.numPages; n++) {
            const p = await doc.getPage(n);
            const { width, height } = p.getViewport({ scale: 1 });
            const text = (await p.getTextContent()).items.map(i => i.str).join(' ').replace(/\s+/g, ' ');
            let green = 0;
            if (n === 1) {
                const viewport = p.getViewport({ scale: 2 });
                const canvas = new OffscreenCanvas(viewport.width, viewport.height);
                await p.render({ canvas, viewport }).promise;
                const px = canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height).data;
                for (let i = 0; i < px.length; i += 4) {
                    if (Math.abs(px[i] - 220) + Math.abs(px[i + 1] - 252) + Math.abs(px[i + 2] - 231) < 12) green++;
                }
            }
            pages.push({ width: Math.round(width * 100) / 100, height: Math.round(height * 100) / 100, text, green });
        }
        return pages;
    }, base64);
}

console.log('--- chromium, page.pdf()');
const invoice = await readPdf(await pdfOf('invoice.html'));
const i1 = invoice[0];
console.log(`invoice: ${invoice.length} page, ${i1.width} x ${i1.height} pt`);
console.log(`  "Bookstore My orders" header printed: ${i1.text.includes('My orders')}, "Print" button printed: ${/\bPrint\b/.test(i1.text)}`);
console.log(`  link printed as: "${i1.text.match(/see our returns page[^)]*\)/)[0]}"`);
console.log(`  "Paid" badge background pixels: ${i1.green}`);
const economy = await readPdf(await pdfOf('invoice.html', '.status { print-color-adjust: economy; }'));
console.log(`  with print-color-adjust: economy: ${economy[0].green}`);

const orders = await readPdf(await pdfOf('orders.html'));
console.log(`orders: ${orders.length} pages`);
orders.forEach((p, n) => {
    const ids = p.text.match(/BK-\d+/g) ?? [];
    console.log(`  page ${n + 1}: header row ${p.text.includes('Order Date Books Status Total') ? 'yes' : 'no '}, rows ${ids[0]} … ${ids.at(-1)} (${ids.length})`);
});
// tall rows with several lines, to see whether a row is cut between two pages
const tall = '.data-table td:nth-child(3) { font-size: 36pt; line-height: 1.1; }';
for (const [name, css] of [['auto', `${tall} .data-table tr { break-inside: auto; }`], ['avoid', tall]]) {
    const pages = await readPdf(await pdfOf('orders.html', css));
    // after the repeated header, a page should start with an order number; anything else is the rest of a cut row
    const cut = pages.slice(1).map((p, n) => {
        const rest = p.text.replace(/^.*?Order Date Books Status Total /, '');
        return rest.startsWith('BK-') ? null : pages[n].text.match(/BK-\d+/g).at(-1);
    }).filter(Boolean);
    console.log(`tall rows, break-inside: ${name.padEnd(5)} → ${pages.length} pages, rows cut in two: ${cut.join(', ') || 'none'}`);
}
await browser.close();

import { open } from './browser.mjs';

// @supports checks whether the browser can parse something, not whether it behaves as you expect
const checks = [
    'position-area: block-end',
    'animation-timeline: view()',
    'transition-behavior: allow-discrete',
    'overlay: auto',
    'position-visibility: anchors-visible',
    'display: grid-lanes',
    'width: if(media(width > 1px): 1px; else: 2px)',
    'selector(:open)',
    'at-rule(@starting-style)',
];

for (const engine of ['chromium', 'webkit']) {
    const { browser, page } = await open('lab/16-supports.html', { engine });
    console.log(`--- ${engine}\n${await page.evaluate(checks => {
        const applied = [...document.querySelectorAll('p')]
            .map(p => `${p.id} ${getComputedStyle(p).color === 'rgb(107, 114, 128)' ? 'no' : 'yes'}`).join(', ');
        const results = checks.map(c => `${c.padEnd(48)} ${CSS.supports(c.startsWith('selector') || c.startsWith('at-rule') ? c : `(${c})`)}`);
        return `blocks applied: ${applied}\n${results.join('\n')}`;
    }, checks)}`);
    await browser.close();
}

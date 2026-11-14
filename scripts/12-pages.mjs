import { open, engines } from './browser.mjs';
import { countColor } from './pixels.mjs';

for (const engine of engines) {
    console.log(`--- ${engine}`);
    const { browser, page } = await open('checkout.html', { engine });
    const style = (sel, prop) => page.locator(sel).first().evaluate((el, p) => getComputedStyle(el)[p], prop);
    console.log(`font-size: textarea ${await style('#note', 'fontSize')}, quantity ${await style('#quantity', 'fontSize')}; `
        + `accent-color ${await style('.shipping-option input', 'accentColor')}; caret-color ${await style('#note', 'caretColor')}`);
    const radio = await page.locator('.shipping-option input').first().boundingBox();
    console.log(`blue pixels in the checked radio: ${await countColor(page, radio, [29, 78, 216])}`);

    // the stepper: clicks, limits, and what a selection of the row copies
    const plus = page.locator('.stepper button[data-step="1"]');
    const minus = page.locator('.stepper button[data-step="-1"]');
    for (let i = 0; i < 3; i++) await plus.click();
    const afterPlus = await page.inputValue('#quantity');
    for (let i = 0; i < 5; i++) await minus.click();
    console.log(`stepper: 3 × + → ${afterPlus}, then 5 × − → ${await page.inputValue('#quantity')}; `
        + `touch-action ${await style('.stepper button', 'touchAction')}`);
    const row = await page.locator('.order-item').evaluate(el => {
        getSelection().selectAllChildren(el);
        return getSelection().toString().replace(/\s+/g, ' ').trim();
    });
    console.log(`selecting the order row copies "${row}"`);

    // the custom checkbox: keyboard focus, focus ring, Space
    await page.focus('.shipping-option input[value="standard"]');
    await page.keyboard.press('Tab');
    const focused = await page.evaluate(() => {
        const el = document.activeElement;
        return el.className ? `.${el.className}` : `#${el.id}`;
    });
    // WebKit on macOS tabs only to text fields by default, so focus the checkbox directly there
    if (focused !== '.check') await page.focus('.check');
    console.log(`Tab from the shipping radios → ${focused}; on .check: outline ${await style('.check', 'outlineStyle')} ${await style('.check', 'outlineWidth')}`);
    await page.keyboard.press('Space');
    console.log(`Space → checked ${await page.locator('.check').isChecked()}, ::before display ${await page.locator('.check').evaluate(el => getComputedStyle(el, '::before').display)}`);

    // the order note grows with its text, up to max-block-size
    const heights = [];
    for (const lines of [0, 6, 20]) {
        await page.fill('#note', Array.from({ length: lines }, (_, i) => `Line ${i + 1}`).join('\n'));
        heights.push(`${lines} lines ${await page.locator('#note').evaluate(el => `${el.offsetHeight}px${el.scrollHeight > el.clientHeight ? ' (scrolls)' : ''}`)}`);
    }
    console.log(`order note: ${heights.join(', ')}`);
    await browser.close();

    // the sale badge on the Recommended strip
    const book = await open('book.html', { engine });
    const hit = async () => {
        const b = await book.page.locator('.sale-badge').boundingBox();
        return book.page.evaluate(([x, y]) => {
            const el = document.elementFromPoint(x, y);
            return `${el.tagName}${el.closest('a') ? ' inside A' : ''}`;
        }, [b.x + b.width / 2, b.y + b.height / 2]);
    };
    await book.page.locator('.sale-badge').scrollIntoViewIfNeeded();
    const withNone = await hit();
    await book.page.addStyleTag({ content: '.sale-badge { pointer-events: auto; }' });
    console.log(`sale badge center hits: pointer-events none → ${withNone}, auto → ${await hit()}`);
    await book.browser.close();
}

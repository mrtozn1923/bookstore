import { open } from './browser.mjs';
import { explain, print } from './cascade-report.mjs';

const style = (page, selector, prop) =>
    page.$eval(selector, (el, p) => getComputedStyle(el).getPropertyValue(p), prop);

// 1) Specificity of :is(), :where(), :not() and :has()
{
    const { browser, page } = await open('lab/02-specificity.html');
    console.log('--- specificity');
    print(await explain(page, '.price', 'color'), 'color');
    await browser.close();
}

for (const engine of ['chromium', 'webkit']) {
    console.log(`--- ${engine}`);

    // 2) Catalog: :has(), :nth-child(of S), attribute selectors, :empty
    let { browser, page } = await open('catalog.html', { engine });
    const cards = await page.$$eval('.book-card', els => els.map(el => {
        const cs = getComputedStyle(el);
        const after = getComputedStyle(el.querySelector('.book-title'), '::after').content;
        return `${el.querySelector('.book-title').textContent.padEnd(11)} bg=${cs.backgroundColor.padEnd(19)} ` +
            `border=${cs.borderTopColor.padEnd(19)} title::after=${after}`;
    }));
    cards.forEach(c => console.log(c));
    console.log('cart-count display:', await style(page, '.cart-count', 'display'));
    await page.$eval('.cart-count', el => { el.textContent = ' '; });
    console.log('cart-count with a space:', await style(page, '.cart-count', 'display'));
    await browser.close();

    // 3) Checkout: :user-invalid vs :invalid, :focus-within, :focus-visible, :open
    ({ browser, page } = await open('checkout.html', { engine }));
    const email = page.locator('#email');
    const state = async label => console.log(label.padEnd(22),
        'invalid:', await email.evaluate(e => e.matches(':invalid')),
        '| user-invalid:', await email.evaluate(e => e.matches(':user-invalid')),
        '| label color:', await style(page, 'label[for=email]', 'color'));
    await state('on load');
    await email.focus();
    await state('focused');
    await email.pressSequentially('ada@');
    await state('typed "ada@"');
    await page.locator('#postcode').focus();
    await state('left the field');

    if (engine === 'chromium') {
        const focus = async (label, locator) => console.log(label.padEnd(22),
            'focus:', await locator.evaluate(e => e === document.activeElement),
            '| focus-visible:', await locator.evaluate(e => e.matches(':focus-visible')));
        const apply = page.getByRole('button', { name: 'Apply' });
        await apply.click();
        await focus('Apply, mouse click', apply);
        await page.locator('#coupon').click();
        await focus('coupon, mouse click', page.locator('#coupon'));
        await page.keyboard.press('Tab');
        await focus('Apply, Tab key', apply);
    }

    await page.locator('summary').click();
    console.log('details :open:', await page.$eval('details', d => d.matches(':open')),
        '| summary font-weight:', await style(page, 'summary', 'font-weight'));
    await browser.close();

    // 4) Forgiving (:is, :where) vs. unforgiving (plain list, :has) selector lists
    ({ browser, page } = await open('lab/02-forgiving.html', { engine }));
    console.log(await page.evaluate(() => {
        const kept = [...document.styleSheets[0].cssRules].map(r => r.selectorText);
        const price = getComputedStyle(document.querySelector('.price'));
        const card = getComputedStyle(document.querySelector('.book-card'));
        return `rules kept: ${kept.join(' | ')}\ncolor: ${price.color}, font-weight: ${price.fontWeight}, ` +
            `font-style: ${price.fontStyle}, card border: ${card.borderTopWidth}`;
    }));
    await browser.close();
}

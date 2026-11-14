// Asks Chromium (via CDP, the protocol DevTools uses) which rules declare a property on an element
export async function explain(page, selector, property) {
    const cdp = await page.context().newCDPSession(page);
    const sheets = new Map();
    cdp.on('CSS.styleSheetAdded', ({ header }) =>
        sheets.set(header.styleSheetId, header.sourceURL.split('/').pop() || '<style>'));
    await cdp.send('DOM.enable');
    await cdp.send('CSS.enable');
    const { root } = await cdp.send('DOM.getDocument');
    const { nodeId } = await cdp.send('DOM.querySelector', { nodeId: root.nodeId, selector });
    const { matchedCSSRules, inlineStyle } = await cdp.send('CSS.getMatchedStylesForNode', { nodeId });

    const rows = [];
    for (const { rule, matchingSelectors } of matchedCSSRules) {
        const declared = rule.style.cssProperties.filter(p => p.name === property && p.value);
        // Author rules list each declaration twice (source text + parsed); keep the source text one
        const own = declared.some(p => p.text) ? declared.filter(p => p.text) : declared.slice(0, 1);
        for (const p of own) {
            const sel = rule.selectorList.selectors[matchingSelectors[0]];
            const s = sel.specificity;
            rows.push({
                origin: rule.origin,
                layer: rule.layers?.map(l => l.text).join('.') || (rule.origin === 'user-agent' ? '' : '(unlayered)'),
                selector: sel.text,
                specificity: s ? `(${s.a},${s.b},${s.c})` : '',
                value: p.value,
                file: rule.origin === 'user-agent' ? 'user agent' : sheets.get(rule.styleSheetId) ?? '',
                important: p.important
            });
        }
    }
    const inline = (inlineStyle?.cssProperties ?? []).filter(p => p.name === property && p.text);
    for (const p of inline) {
        rows.push({ layer: '', selector: 'style=""', specificity: '', value: p.value, file: 'style attribute', important: p.important });
    }
    const computed = await page.$eval(selector, (el, prop) => getComputedStyle(el).getPropertyValue(prop), property);
    await cdp.detach();
    // rows are in ascending cascade order; for !important the order flips (earlier layer wins)
    const important = rows.filter(r => r.important);
    const normal = rows.filter(r => !r.important).reverse();
    return { computed, rows: [...important, ...normal] };
}

export function print({ computed, rows }, property) {
    console.log(`computed ${property}: ${computed}`);
    rows.forEach((r, i) => {
        console.log(`${i === 0 ? '✓' : '✗'} ${r.layer.padEnd(18)} ${r.selector.padEnd(36)} ${r.specificity.padEnd(8)} ${r.value.padEnd(22)} ${r.file}`);
    });
}

// Chromium only: reads the accessibility tree through the DevTools protocol (CDP).

// Chromium's accessibility tree: the name a screen reader gets (generated content included)
export async function axNames(page, selector) {
    const cdp = await page.context().newCDPSession(page);
    await cdp.send('Accessibility.enable');
    const { root } = await cdp.send('DOM.getDocument', { depth: -1 });
    const { nodeIds } = await cdp.send('DOM.querySelectorAll', { nodeId: root.nodeId, selector });
    const names = [];
    for (const nodeId of nodeIds) {
        const { nodes } = await cdp.send('Accessibility.getPartialAXTree', { nodeId, fetchRelatives: false });
        names.push(nodes[0].name?.value ?? '');
    }
    await cdp.detach();
    return names;
}
// the text of each list item (marker and generated content included), nested lists left out
export async function axItems(page, selector, separator = ' ') {
    const cdp = await page.context().newCDPSession(page);
    const { nodes } = await cdp.send('Accessibility.getFullAXTree');
    const { root } = await cdp.send('DOM.getDocument', { depth: -1 });
    const { nodeId } = await cdp.send('DOM.querySelector', { nodeId: root.nodeId, selector });
    const { node } = await cdp.send('DOM.describeNode', { nodeId });
    const byId = new Map(nodes.map(n => [n.nodeId, n]));
    const items = [];
    const text = n => ['StaticText', 'ListMarker'].includes(n.role?.value) ? n.name?.value ?? ''
        : n.role?.value === 'list' ? '' : (n.childIds ?? []).map(id => text(byId.get(id))).join(separator);
    const walk = n => {
        if (n.role?.value === 'listitem') items.push(text(n).replace(/\s+/g, ' ').trim());
        for (const id of n.childIds ?? []) walk(byId.get(id));
    };
    walk(nodes.find(n => n.backendDOMNodeId === node.backendNodeId));
    await cdp.detach();
    return items.join(' | ');
}
// all text a screen reader gets from the element's subtree
export async function axText(page, selector) {
    const cdp = await page.context().newCDPSession(page);
    const { nodes } = await cdp.send('Accessibility.getFullAXTree');
    const { root } = await cdp.send('DOM.getDocument', { depth: -1 });
    const { nodeId } = await cdp.send('DOM.querySelector', { nodeId: root.nodeId, selector });
    const { node } = await cdp.send('DOM.describeNode', { nodeId });
    const byId = new Map(nodes.map(n => [n.nodeId, n]));
    const text = n => n.role?.value === 'StaticText' ? n.name?.value ?? ''
        : (n.childIds ?? []).map(id => text(byId.get(id))).join(' ');
    const result = text(nodes.find(n => n.backendDOMNodeId === node.backendNodeId)).replace(/\s+/g, ' ').trim();
    await cdp.detach();
    return result;
}

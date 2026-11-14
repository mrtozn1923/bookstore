// Figma Auto Layout (Plugin API property names) -> CSS. Runs in Node and in the browser.
const justify = { MIN: 'flex-start', CENTER: 'center', MAX: 'flex-end', SPACE_BETWEEN: 'space-between' };
const align = { MIN: 'flex-start', CENTER: 'center', MAX: 'flex-end', BASELINE: 'baseline' };

// A value bound to a variable becomes the token's CSS variable; an unbound number stays a raw px value.
function value(node, prop, raw, variables, unbound) {
  const ref = node.boundVariables?.[prop];
  const id = Array.isArray(ref) ? ref[0]?.id : ref?.id;
  if (id) return `var(--${variables[id].name.replaceAll('/', '-')})`;
  if (raw !== undefined && raw !== 0) unbound.push(`${node.name}.${prop} = ${raw}`);
  return typeof raw === 'number' ? `${raw}px` : raw;
}

export function toCss(node, variables, parent = null, unbound = []) {
  const css = {};
  const auto = node.layoutMode === 'HORIZONTAL' || node.layoutMode === 'VERTICAL';
  if (auto) {
    css.display = 'flex';
    css['flex-direction'] = node.layoutMode === 'HORIZONTAL' ? 'row' : 'column';
    css['justify-content'] = justify[node.primaryAxisAlignItems ?? 'MIN'];
    css['align-items'] = align[node.counterAxisAlignItems ?? 'MIN'];
    // itemSpacing -> gap; a gap bound to a spacing variable becomes var(--space-n)
    if (node.itemSpacing !== undefined) css.gap = value(node, 'itemSpacing', node.itemSpacing, variables, unbound);
    if (node.layoutWrap === 'WRAP') css['flex-wrap'] = 'wrap';
    const pad = ['paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft'].map((p) => value(node, p, node[p] ?? 0, variables, unbound) ?? '0px');
    if (pad.some((p) => p !== '0px')) css.padding = pad.join(' ');
  }
  // sizing: FILL grows along the parent's main axis or stretches across it, HUG takes the content size
  const row = parent?.layoutMode === 'HORIZONTAL';
  for (const [axis, size] of [['Horizontal', 'width'], ['Vertical', 'height']]) {
    const mode = node[`layoutSizing${axis}`];
    const main = parent && (row ? axis === 'Horizontal' : axis === 'Vertical');
    if (mode === 'FILL') {
      if (main) css.flex = '1 1 0';
      else if (parent) css['align-self'] = 'stretch';
      else css[size] = '100%';
    } else if (mode === 'FIXED') {
      css[size] = value(node, size, node[size], variables, unbound);
      if (main) css['flex-shrink'] = '0';
    } else if (mode === 'HUG' && main) {
      css.flex = 'none';
    }
  }
  if (node.cornerRadius) css['border-radius'] = value(node, 'cornerRadius', node.cornerRadius, variables, unbound);
  const fill = node.boundVariables?.fills?.[0]?.id;
  if (fill) css[node.type === 'TEXT' ? 'color' : 'background'] = `var(--${variables[fill].name.replaceAll('/', '-')})`;
  if (node.type === 'TEXT') {
    css['font-size'] = value(node, 'fontSize', node.fontSize, variables, []);
    css['font-weight'] = String(node.fontWeight);
  }
  return { css, unbound };
}

export const declarations = (css) => Object.entries(css).map(([k, v]) => `${k}: ${v};`).join(' ');

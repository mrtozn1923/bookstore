// Reads pixels of a screenshot back through a canvas in the page (no image library needed).
// fullPage: true takes clip in page coordinates; false takes it in viewport coordinates (sticky elements after scrolling).
export async function pixels(page, clip, fullPage = true) {
    const png = (await page.screenshot({ clip, fullPage })).toString('base64');
    return page.evaluate(async src => {
        const img = new Image();
        img.src = `data:image/png;base64,${src}`;
        await img.decode();
        const canvas = new OffscreenCanvas(img.width, img.height);
        const ctx = canvas.getContext('2d');
        ctx.drawImage(img, 0, 0);
        return { width: img.width, height: img.height, data: [...ctx.getImageData(0, 0, img.width, img.height).data] };
    }, png);
}

// How many pixels in the area are close to the given rgb color?
export async function countColor(page, clip, [r, g, b], tolerance = 40) {
    const { data } = await pixels(page, clip);
    let count = 0;
    for (let i = 0; i < data.length; i += 4) {
        if (Math.abs(data[i] - r) + Math.abs(data[i + 1] - g) + Math.abs(data[i + 2] - b) < tolerance) count++;
    }
    return count;
}

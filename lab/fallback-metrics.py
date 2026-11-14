"""Computes @font-face overrides that make a local fallback font take the same space as a web font.
Usage: python fallback-metrics.py <webfont.woff2> <fallback.ttf>   (needs: pip install fonttools brotli)"""
import sys
from fontTools.ttLib import TTFont

# Text the widths are averaged over: the letters of real Bookstore copy, Turkish included
SAMPLE = "Bookstore haftanın kitabı: Dune. Kitaplığımızda Şubat ayında İstanbul'dan gelen yeni çıkanlar var."


def average_width(font: TTFont) -> float:
    cmap, hmtx, upem = font.getBestCmap(), font["hmtx"], font["head"].unitsPerEm
    widths = [hmtx[cmap[ord(c)]][0] / upem for c in SAMPLE if ord(c) in cmap]
    return sum(widths) / len(widths)


web, fallback = TTFont(sys.argv[1]), TTFont(sys.argv[2])
size_adjust = average_width(web) / average_width(fallback)
hhea, upem = web["hhea"], web["head"].unitsPerEm
print(f"size-adjust: {size_adjust * 100:.2f}%;")
print(f"ascent-override: {hhea.ascent / upem / size_adjust * 100:.2f}%;")
print(f"descent-override: {-hhea.descent / upem / size_adjust * 100:.2f}%;")
print(f"line-gap-override: {hhea.lineGap / upem / size_adjust * 100:.2f}%;")

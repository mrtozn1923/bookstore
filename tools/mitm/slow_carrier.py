"""mitmproxy addon: holds the carrier's quote responses to simulate a slow carrier.

    mitmdump -s tools/mitm/slow_carrier.py --set carrier_delay_ms=3000 --set carrier_slow_count=1
"""
import asyncio
import logging

from mitmproxy import ctx, http


class SlowCarrier:
    def __init__(self) -> None:
        self.slowed = 0

    def load(self, loader) -> None:
        loader.add_option("carrier_delay_ms", int, 0, "Milliseconds to hold each /v1/quotes response.")
        loader.add_option("carrier_slow_count", int, 0, "How many responses to hold (0 = all).")

    async def response(self, flow: http.HTTPFlow) -> None:
        if ctx.options.carrier_delay_ms <= 0 or not flow.request.path.startswith("/v1/quotes"):
            return
        if ctx.options.carrier_slow_count and self.slowed >= ctx.options.carrier_slow_count:
            return
        self.slowed += 1
        logging.info("slow_carrier: holding response #%d for %d ms", self.slowed, ctx.options.carrier_delay_ms)
        # async wait: other connections through the proxy keep moving
        await asyncio.sleep(ctx.options.carrier_delay_ms / 1000)


addons = [SlowCarrier()]

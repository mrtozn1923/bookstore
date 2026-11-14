// Which device APIs need a user activation? The same calls from a click and from a timer.
const calls = {
  writeText: () => navigator.clipboard.writeText("1051"),
  fullscreen: () => document.documentElement.requestFullscreen(),
  wakeLock: () => navigator.wakeLock.request("screen"),
  // two APIs that consume the activation, one after the other in the same click
  fullscreenThenShare: async () => {
    await document.documentElement.requestFullscreen();
    await navigator.share({ title: "Dune", url: location.href });
  },
  share: () => navigator.share({ title: "Dune", url: location.href }),
};

async function run(source, name) {
  const active = navigator.userActivation.isActive;
  // a share sheet can stay open for a long time: do not wait for it
  const result = await Promise.race([
    calls[name]().then(() => "ok", (err) => `${err.name}: ${err.message}`),
    new Promise((resolve) => setTimeout(() => resolve("still pending"), 2000)),
  ]);
  console.log(`${source.padEnd(5)} ${name.padEnd(19)} activation=${String(active).padEnd(5)} -> ${result}`);
  if (document.fullscreenElement) await document.exitFullscreen();
}

for (const name of Object.keys(calls)) {
  const button = document.createElement("button");
  button.id = name;
  button.textContent = name;
  button.addEventListener("click", () => run("click", name));
  document.getElementById("buttons").append(button, " ");
}

// long after any user activation has expired
setTimeout(async () => {
  for (const name of Object.keys(calls)) await run("timer", name);
  console.log("timer done");
}, 7000);

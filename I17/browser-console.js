// I17: examples to paste into the DevTools console on index.html, one block at a time, in order
// (blocks with top-level await work in the console; example 16 removes the sale banner, so run it last)

// --- example 1 ---
const banner = document.querySelector(".sale-banner");
const slide = banner.animate(
  [{ transform: "translateX(0)" }, { transform: "translateX(200px)" }],
  { duration: 500 }
);

console.log(slide.playState);
slide.finished.then(() => {
  console.log(slide.playState, getComputedStyle(banner).transform);
});

// --- example 2 ---
const duneCover = document.querySelector("#book-dune .cover");

const wiggle = duneCover.animate(
  [
    { transform: "rotate(0deg)" },
    { transform: "rotate(-6deg)", offset: 0.25 },
    { transform: "rotate(6deg)", offset: 0.75 },
    { transform: "rotate(0deg)" }
  ],
  { duration: 400, iterations: 3, easing: "ease-in-out", delay: 100 }
);

console.log(wiggle.playState); // running

// --- example 3 ---
const emmaCover = document.querySelector("#book-emma .cover");
const spin = emmaCover.animate(
  { transform: ["rotate(0deg)", "rotate(360deg)"] },
  { duration: 1000, iterations: Infinity }
);

spin.pause();
spin.currentTime = 250;
console.log(spin.playState, getComputedStyle(emmaCover).transform); // paused matrix(0, 1, -1, 0, 0, 0)

spin.playbackRate = 2;
spin.play();
console.log(spin.playState); // running

spin.reverse();
spin.cancel();
console.log(spin.playState, getComputedStyle(emmaCover).transform); // idle none

// --- example 7 ---
const hide = banner.animate({ opacity: [1, 0] }, { duration: 300, fill: "forwards" });
await hide.finished;
console.log(getComputedStyle(banner).opacity); // 0

banner.style.opacity = "1";
console.log(getComputedStyle(banner).opacity); // 0: the fill still wins

hide.commitStyles();
hide.cancel();
console.log(banner.style.opacity); // 0: the value is now in the style attribute
banner.style.opacity = "1";
console.log(getComputedStyle(banner).opacity); // 1

// --- example 8 ---
const hobbitCover = document.querySelector("#book-hobbit .cover");
const steps = [0, 1, 2].map((i) =>
  hobbitCover.animate(
    { transform: [`translateX(${i * 10}px)`, `translateX(${i * 10 + 10}px)`] },
    { duration: 50, fill: "forwards" }
  )
);

await steps[2].finished;
console.log(steps.map((a) => a.replaceState), hobbitCover.getAnimations().length);
// ["removed", "removed", "active"] 1
steps[2].cancel();

// --- example 9 ---
const running = document.getAnimations();
console.log(running.map((a) => `${a.constructor.name} ${a.animationName}`)); // ["CSSAnimation pulse"]

const header = document.querySelector(".site-header");
console.log(header.getAnimations().length, header.getAnimations({ subtree: true }).length); // 0 1

running.forEach((a) => a.pause());

// --- example 12 ---
const postalCode = document.querySelector("#postal-code");
postalCode.scrollIntoView({ block: "start", behavior: "instant" });
console.log(Math.round(postalCode.getBoundingClientRect().top)); // 72

postalCode.style.scrollMarginTop = "0px";
postalCode.scrollIntoView({ block: "start", behavior: "instant" });
console.log(Math.round(postalCode.getBoundingClientRect().top)); // 0: hidden under the header

// --- example 15 ---
const middlemarchCover = document.querySelector("#book-middlemarch .cover");
await middlemarchCover.animate({ width: ["180px", "120px"] }, 1000).finished;
await middlemarchCover.animate({ transform: ["scale(1)", "scale(0.66)"] }, 1000).finished;

// --- example 16 ---
const notice = document.querySelector(".sale-banner");
const fadeOut = notice.animate({ opacity: [1, 0] }, 300);
notice.remove(); // wrong: await fadeOut.finished first, then remove
await fadeOut.finished;
console.log(fadeOut.playState, notice.isConnected); // finished false

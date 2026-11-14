// I14: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 1 ---
const canvas = document.createElement("canvas");
console.log(canvas.width, canvas.height);

canvas.style.width = "600px";
console.log(canvas.width);

// --- example 3 ---
// Triangle
ctx.beginPath();
ctx.moveTo(60, 230);
ctx.lineTo(140, 230);
ctx.lineTo(100, 160);
ctx.closePath();
ctx.fillStyle = "seagreen";
ctx.fill();

// Circle
ctx.beginPath();
ctx.arc(250, 195, 35, 0, Math.PI * 2);
ctx.strokeStyle = "black";
ctx.lineWidth = 3;
ctx.stroke();

// --- example 4 ---
const gradient = ctx.createLinearGradient(0, 0, 400, 0);
gradient.addColorStop(0, "#f97316");
gradient.addColorStop(1, "#7c3aed");
ctx.fillStyle = gradient;
ctx.fillRect(0, 0, 400, 250);

ctx.fillStyle = "white";
ctx.font = "bold 32px system-ui";
ctx.textAlign = "center";
ctx.textBaseline = "middle";
ctx.fillText("20% off classics", 200, 125);

// --- example 7 ---
function makeSharp(canvas, cssWidth, cssHeight) {
  const dpr = window.devicePixelRatio || 1;
  canvas.style.width = `${cssWidth}px`;
  canvas.style.height = `${cssHeight}px`;
  canvas.width = Math.round(cssWidth * dpr);
  canvas.height = Math.round(cssHeight * dpr);

  const ctx = canvas.getContext("2d");
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0); // from now on, draw in CSS pixels
  return ctx;
}

const ctx = makeSharp(document.getElementById("canvas"), 400, 250);

// --- example 8 ---
ctx.fillStyle = "red";
ctx.save();
ctx.fillStyle = "blue";
ctx.fillRect(0, 0, 50, 50);
ctx.restore();
ctx.fillRect(60, 0, 50, 50);

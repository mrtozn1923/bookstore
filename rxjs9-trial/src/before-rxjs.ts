// imported before rxjs: shows what the runtime has on its own
console.log("Observable before rxjs:", typeof (globalThis as { Observable?: unknown }).Observable);

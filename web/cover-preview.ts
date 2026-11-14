import { filter, fromEvent, map } from "rxjs";

const input = document.querySelector<HTMLInputElement>("#cover")!;
const info = document.querySelector<HTMLParagraphElement>("#info")!;
const preview = document.querySelector<HTMLImageElement>("#preview")!;

type CoverChoice = { ok: true; file: File } | { ok: false; reason: string };

const coverChoice$ = fromEvent(input, "change").pipe(
  map(() => input.files?.[0]),
  filter((file): file is File => file !== undefined),
  map((file): CoverChoice =>
    file.type.startsWith("image/")
      ? { ok: true, file }
      : { ok: false, reason: `${file.name} is not an image` },
  ),
);

let previewUrl: string | undefined;
coverChoice$.subscribe((choice) => {
  if (!choice.ok) {
    info.textContent = choice.reason;
    return;
  }
  if (previewUrl) URL.revokeObjectURL(previewUrl);
  previewUrl = URL.createObjectURL(choice.file);
  preview.src = previewUrl;
  info.textContent = `${choice.file.name} (${choice.file.size} bytes)`;
});

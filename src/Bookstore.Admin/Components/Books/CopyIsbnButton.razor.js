// The Clipboard API needs a secure context (HTTPS or localhost) and the user's permission.
export async function copyText(text) {
  try {
    await navigator.clipboard.writeText(text);
    return true;
  } catch {
    return false;
  }
}

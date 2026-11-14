// T16: run with `node T16/examples.js` (or paste the blocks into the browser console in order)

// --- example 4 ---
function checkName(name) {
  if (name === "") return "Name can't be empty.";
  if (name.length < 2) return "Name must be at least 2 characters.";
  return "";
}

function checkEmail(email) {
  if (email === "") return "Email can't be empty.";
  if (!email.includes("@") || !email.includes(".")) {
    return "Enter a valid email address.";
  }
  return "";
}

function checkPassword(password) {
  if (password.length < 8) return "Password must be at least 8 characters.";
  return "";
}

console.log(checkName("  ".trim())); // "Name can't be empty."
console.log(checkEmail("ada@example.com")); // ""
console.log(checkPassword("1234")); // "Password must be at least 8 characters."

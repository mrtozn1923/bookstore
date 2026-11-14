async function isLoggedIn() {
  return false;
}

async function greet() {
  // Error: This condition will always return true since this 'Promise<boolean>' is always defined.
  if (isLoggedIn()) {
    console.log("Welcome back");
  }
}

async function saveAll(ids: number[]) {
  ids.forEach(async (id) => {
    await new Promise((resolve) => setTimeout(resolve, 10));
    console.log("saved", id);
  });
  console.log("all saved?");
}

saveAll([1, 2]);
// all saved?
// saved 1
// saved 2

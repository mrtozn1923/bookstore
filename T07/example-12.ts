type Email = string;
type Url = string;

function openLink(url: Url) {
  console.log(`Opening ${url}`);
}

const email: Email = "ada@example.com";
openLink(email); // no error: both are just string

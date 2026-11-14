const waitlist = ["Alan", "Ada"];

waitlist.push("Grace");       // adds to the end
console.log(waitlist);        // ["Alan", "Ada", "Grace"]

waitlist.unshift("Linus");    // adds to the start
console.log(waitlist);        // ["Linus", "Alan", "Ada", "Grace"]

const last = waitlist.pop();    // removes the last one
const first = waitlist.shift(); // removes the first one
console.log(last, first);       // "Grace" "Linus"
console.log(waitlist);          // ["Alan", "Ada"]

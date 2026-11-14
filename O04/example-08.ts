interface Greeter {
  greet(name: string): string;
}

interface Named {
  readonly label: string;
}

class WelcomeGreeter implements Greeter, Named {
  readonly label = "welcome";

  greet(name: string): string {
    return `Welcome back, ${name}!`;
  }
}

console.log(new WelcomeGreeter().greet("Ada")); // Welcome back, Ada!

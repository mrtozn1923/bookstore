// I08: run with `node I08/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
const target = { title: "Dune" };

const p = new Proxy(target, {
  get(t, key) {
    return key in t ? t[key] : "none";
  }
});

p.price = 450;
console.log(p.author);
console.log(target.price);
console.log(p === target);

// --- example 2 ---
const dune = { title: "Dune", stock: 12 };

const watched = new Proxy(dune, {
  get(target, key) {
    console.log(`read: ${String(key)}`);
    return target[key];
  },
  set(target, key, value) {
    console.log(`written: ${String(key)} = ${value}`);
    target[key] = value;
    return true;
  },
  has(target, key) {
    console.log(`checked: ${String(key)}`);
    return key in target;
  },
  deleteProperty(target, key) {
    console.log(`deleted: ${String(key)}`);
    return delete target[key];
  }
});

watched.title;
watched.price = 450;
"stock" in watched;
delete watched.stock;
console.log(dune); // { title: "Dune", price: 450 }

// --- example 3 ---
const author = {
  firstName: "Jane",
  lastName: "Austen",
  get fullName() {
    return `${this.firstName} ${this.lastName}`;
  }
};

const leaky = new Proxy(author, {
  get(target, key) {
    console.log("get:", key);
    return target[key];
  }
});

const solid = new Proxy(author, {
  get(target, key, receiver) {
    console.log("get:", key);
    return Reflect.get(target, key, receiver);
  }
});

leaky.fullName; // get: fullName
solid.fullName; // get: fullName, get: firstName, get: lastName

// --- example 4 ---
function validated(target, rules) {
  return new Proxy(target, {
    set(t, key, value, receiver) {
      const rule = rules[key];
      if (rule && !rule(value)) {
        throw new TypeError(`Invalid value: ${String(key)} = ${value}`);
      }
      return Reflect.set(t, key, value, receiver);
    }
  });
}

const newBook = validated({}, {
  title: (v) => typeof v === "string" && v.trim() !== "",
  price: (v) => typeof v === "number" && v >= 0
});

newBook.title = "Dune";
newBook.price = 450;

try {
  newBook.price = -5;
} catch (e) {
  console.log(e.message); // Invalid value: price = -5
}
console.log(newBook.title, newBook.price); // Dune 450

// --- example 5 ---
const defaults = { currency: "TRY", lang: "en", pageSize: 20 };

function createPreferences(customerPreferences) {
  return new Proxy(customerPreferences, {
    get(t, key, receiver) {
      if (Reflect.has(t, key)) return Reflect.get(t, key, receiver);
      return defaults[key];
    }
  });
}

const preferences = createPreferences({ currency: "EUR" });
console.log(preferences.currency, preferences.lang, preferences.pageSize); // EUR en 20

// --- example 6 ---
function hide(target) {
  const isHidden = (k) => typeof k === "string" && k.startsWith("_");
  return new Proxy(target, {
    get: (t, k, r) => (isHidden(k) ? undefined : Reflect.get(t, k, r)),
    has: (t, k) => (isHidden(k) ? false : Reflect.has(t, k)),
    ownKeys: (t) => Reflect.ownKeys(t).filter((k) => !isHidden(k)),
    deleteProperty: (t, k) => (isHidden(k) ? false : Reflect.deleteProperty(t, k))
  });
}

const customer = hide({ name: "Ada", _internalId: 4821 });
console.log(customer._internalId);      // undefined
console.log("_internalId" in customer); // false
console.log(Object.keys(customer));     // ["name"]

// --- example 8 ---
const stock = new Map([["Dune", 12]]);

const broken = new Proxy(stock, {});
try {
  broken.get("Dune");
} catch (e) {
  console.log(e.constructor.name); // TypeError
}

const working = new Proxy(stock, {
  get(t, k) {
    const value = Reflect.get(t, k, t);
    return typeof value === "function" ? value.bind(t) : value;
  }
});
console.log(working.get("Dune"), working.size); // 12 1

// --- example 9 ---
const storeConfig = Object.freeze({ currency: "TRY" });
const liar = new Proxy(storeConfig, { get: () => "EUR" });

try {
  liar.currency;
} catch (e) {
  console.log(e.message); // a TypeError saying the trap broke the rule
}

// --- example 10 ---
const array = Array.from({ length: 1_000_000 }, (_, i) => i);
const wrapped = new Proxy(array, {});

function sum(a) {
  let s = 0;
  for (let i = 0; i < a.length; i++) s += a[i];
  return s;
}

let t0 = performance.now();
sum(array);
console.log("plain array:", (performance.now() - t0).toFixed(1), "ms");

t0 = performance.now();
sum(wrapped);
console.log("proxy:", (performance.now() - t0).toFixed(1), "ms");

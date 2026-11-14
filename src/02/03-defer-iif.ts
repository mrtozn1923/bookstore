import { defer, iif, of } from "rxjs";

const session = { customer: "Grace", isInstitutional: false };

const retailPrice$ = of(450);
const institutionalPrice$ = of(405); // 10% institutional discount on Dune

// decided once, when this line runs
const ternaryPrice$ = session.isInstitutional ? institutionalPrice$ : retailPrice$;
// decided again on every subscribe
const iifPrice$ = iif(() => session.isInstitutional, institutionalPrice$, retailPrice$);

session.isInstitutional = true; // Grace switches to her school's account

ternaryPrice$.subscribe((p) => console.log("ternary:", p));
iifPrice$.subscribe((p) => console.log("iif:", p));

// defer runs its factory on every subscribe
let cartVersion = 0;
const cartSnapshot$ = defer(() => of(`cart v${++cartVersion}`));
cartSnapshot$.subscribe((v) => console.log("first subscriber:", v));
cartSnapshot$.subscribe((v) => console.log("second subscriber:", v));

import { threeWayMerge } from './merge.ts';

const base = { fullName: "Ada Lovelace", phone: "+90 212 555 0101", spendingLimit: 85000, institutionUnitId: 7 };
const mine = { ...base, phone: "+90 212 555 0199" };
const theirs = { ...base, spendingLimit: 92000 };
console.log(threeWayMerge(base, mine, theirs));
console.log(threeWayMerge(base, { ...mine, spendingLimit: 90000 }, theirs));

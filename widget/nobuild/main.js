import { debounce } from "lodash-es";

const input = document.querySelector("#query");
const search = debounce(() => console.log(`searching for "${input.value}"`), 200);
input.addEventListener("input", search);
console.log("book finder ready");

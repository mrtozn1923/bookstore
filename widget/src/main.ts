import "./styles.css";
import { sampleBooks } from "./books";
import { mountBookFinder } from "./finder";

const host = document.querySelector<HTMLElement>("#book-finder");
if (host) {
    mountBookFinder(host, sampleBooks);
}

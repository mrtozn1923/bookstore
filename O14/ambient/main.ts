import { isValidIsbn } from "legacy-isbn";
import styles from "./book-card.css";

console.log(isValidIsbn("9780441172719"), styles.cover, __APP_VERSION__);

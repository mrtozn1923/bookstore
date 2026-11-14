type DomEvent = "click" | "keydown" | "scroll";

type HandlerName = `on${Capitalize<DomEvent>}`; // "onClick" | "onKeydown" | "onScroll"

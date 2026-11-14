function tryRun(fn) {
  try {
    fn();
  } catch (error) {
    if (error instanceof TypeError) {
      console.log("Type error:", error.message);
    } else if (error instanceof RangeError) {
      console.log("Range error:", error.message);
    } else {
      throw error; // unknown error, pass it on
    }
  }
}

tryRun(() => null.length);              // Type error: ...
tryRun(() => new Array(-1));            // Range error: Invalid array length
tryRun(() => decodeURIComponent("%"));  // URIError, rethrown

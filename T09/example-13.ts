function scheduleTitle(title: string | undefined) {
  if (title !== undefined) {
    // Error: 'title' is possibly 'undefined'.
    setTimeout(() => console.log(title.toUpperCase()), 100);
  }
  title = undefined;
}

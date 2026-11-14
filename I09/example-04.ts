const connection = {
  close() {
    console.log("closed");
  },
};

function findBook() {
  using conn = connection; // Error
}

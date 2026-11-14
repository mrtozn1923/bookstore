interface CategoryNode {
  name: string;
  bookCount: number;
  children: CategoryNode[];
}

function countBooks(node: CategoryNode): number {
  return node.children.reduce((total, child) => total + countBooks(child), node.bookCount);
}

const fiction: CategoryNode = {
  name: "Fiction",
  bookCount: 1,
  children: [
    { name: "Classics", bookCount: 2, children: [] },
    { name: "Fantasy", bookCount: 3, children: [{ name: "Epic Fantasy", bookCount: 4, children: [] }] },
  ],
};

console.log(countBooks(fiction)); // 10

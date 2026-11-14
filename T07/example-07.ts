type Entity = {
  id: string;
  createdAt: Date;
};

type Customer = Entity & {
  name: string;
  email: string;
};

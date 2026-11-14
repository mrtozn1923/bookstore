interface Comment {
  text: string;
}

// Error: Type '{ text: string; }' is missing the following properties from type 'Comment': data, length, ownerDocument, appendData, and 59 more.
const comment: Comment = { text: "Great book!" };

const User = {
  collectionName: "users",

  fields: {
    name: "string",
    email: "string",
    password: "string",
    role: "string",
    createdAt: "date",
  },
};

module.exports = User;
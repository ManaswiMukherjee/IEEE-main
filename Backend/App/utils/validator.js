const isObject = (value) => {
  return value !== null && typeof value === "object" && !Array.isArray(value);
};

const isPositiveNumber = (value) => {
  return typeof value === "number" && value > 0;
};

const isNonEmptyString = (value) => {
  return typeof value === "string" && value.trim().length > 0;
};

module.exports = {
  isObject,
  isPositiveNumber,
  isNonEmptyString,
};
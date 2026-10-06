const fs = require("fs");

function loadPayload(filePath) {
  let text;
  try {
    text = fs.readFileSync(filePath, "utf8");
  } catch (err) {
    if (err && err.code === "ENOENT") {
      throw new Error(`Input file not found: ${filePath}`);
    }
    throw err;
  }

  let payload;
  try {
    payload = JSON.parse(text);
  } catch (err) {
    throw new Error(`${filePath} is not valid JSON: ${err.message}`);
  }

  if (
    !payload ||
    typeof payload !== "object" ||
    !Array.isArray(payload.results)
  ) {
    throw new Error(`${filePath} must be an object with a results array`);
  }
  return payload;
}

const payload = loadPayload("test.json");

const words = new Map();
const files = new Map();

payload.results.forEach((result) => {
  result.list.forEach((hit) =>
    words.set(hit.word, words.get(hit.word) + 1 || 1),
  );
});

payload.results.forEach((result) => {
  result.list.forEach((hit) => {
    let count = 1;
    payload.results.forEach((other) => {
      if (other.file === result.file) return;
      other.list.forEach((otherHit) => {
        if (otherHit.found === hit.found) count += 1;
      });
    });
    files.set(result.file + " " + hit.found, count);
  });
});

function byCount(map) {
  return new Map([...map].sort((a, b) => b[1] - a[1]));
}

console.log(byCount(words));
console.log(byCount(files));

const word = "golfer";
const matched = new Map();
payload.results.forEach((result) => {
  result.list.forEach((hit) => {
    if (hit.word !== word) return;
    const key = result.file + " " + hit.found;
    matched.set(key, files.get(key));
  });
});
console.log(byCount(matched));

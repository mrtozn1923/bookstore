self.onmessage = (e) => {
  const { start, end } = e.data;
  let total = 0;
  for (let i = start; i <= end; i++) total += i;
  self.postMessage(total);
};
